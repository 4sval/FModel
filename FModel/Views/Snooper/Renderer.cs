using System;
using System.Numerics;
using System.Threading;
using System.Windows;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace FModel.Views.Snooper;

public class Renderer : IDisposable
{
    private Shader _shader;
    private Shader _outline;

    public bool bDiffuseOnly;

    public PickingTexture Picking { get; }
    public Cache Cache { get; }
    public Options Settings { get; }

    public Renderer(int width, int height)
    {
        Picking = new PickingTexture(width, height);
        Cache = new Cache();
        Settings = new Options();
    }

    public Camera Load(CancellationToken cancellationToken, UObject export)
    {
        return export switch
        {
            UStaticMesh st => LoadStaticMesh(st),
            USkeletalMesh sk => LoadSkeletalMesh(sk),
            UMaterialInstance mi => LoadMaterialInstance(mi),
            UWorld wd => LoadWorld(cancellationToken, wd, Transform.Identity),
            _ => throw new ArgumentOutOfRangeException(nameof(export))
        };
    }

    public void Swap(UMaterialInstance unrealMaterial)
    {
        if (!Cache.Models.TryGetValue(Settings.SelectedModel, out var model) ||
            !Settings.TryGetSection(model, out var section)) return;

        model.Materials[section.MaterialIndex].SwapMaterial(unrealMaterial);
        Application.Current.Dispatcher.Invoke(() => model.Materials[section.MaterialIndex].Setup(Cache, model.NumTexCoords));
        Settings.SwapMaterial(false);
    }

    public void Setup()
    {
        _shader = new Shader();
        _outline = new Shader("outline");

        Picking.Setup();
        Cache.Setup();
    }

    public void Render(Camera cam)
    {
        var viewMatrix = cam.GetViewMatrix();
        var projMatrix = cam.GetProjectionMatrix();

        // render pass
        _shader.Render(viewMatrix, cam.Position, cam.Direction, projMatrix);
        _shader.SetUniform("bDiffuseOnly", bDiffuseOnly);
        foreach (var model in Cache.Models.Values)
        {
            if (!model.Show) continue;
            model.Render(_shader);
        }

        // outline pass
        if (Cache.Models.TryGetValue(Settings.SelectedModel, out var selected) && selected.Show)
        {
            _outline.Render(viewMatrix, cam.Position, projMatrix);
            selected.Outline(_outline);
        }

        // picking pass (dedicated FBO, binding to 0 afterward)
        Picking.Render(viewMatrix, projMatrix, Cache.Models);
    }

    private Camera SetupCamera(FBox box)
    {
        var far = box.Max.AbsMax();
        var center = box.GetCenter();
        return new Camera(
            new Vector3(0f, center.Z, box.Max.Y * 3),
            new Vector3(center.X, center.Z, center.Y),
            0.01f, far * 50f, far / 1.5f);
    }

    private Camera LoadStaticMesh(UStaticMesh original)
    {
        var guid = original.LightingGuid;
        if (Cache.Models.TryGetValue(guid, out var model))
        {
            model.AddInstance(Transform.Identity);
            Application.Current.Dispatcher.Invoke(() => model.SetupInstances());
            return null;
        }

        if (!original.TryConvert(out var mesh))
            return null;

        Cache.Models[guid] = new Model(original.Name, original.ExportType, original.Materials, mesh);
        Settings.SelectModel(guid);
        return SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
    }

    private Camera LoadSkeletalMesh(USkeletalMesh original)
    {
        var guid = Guid.NewGuid();
        if (Cache.Models.ContainsKey(guid) || !original.TryConvert(out var mesh)) return null;

        Cache.Models[guid] = new Model(original.Name, original.ExportType, original.Materials, original.MorphTargets, mesh);
        Settings.SelectModel(guid);
        return SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
    }

    private Camera LoadMaterialInstance(UMaterialInstance original)
    {
        var guid = Guid.NewGuid();
        if (Cache.Models.ContainsKey(guid)) return null;

        Cache.Models[guid] = new Cube(original);
        Settings.SelectModel(guid);
        return SetupCamera(new FBox(new FVector(-.65f), new FVector(.65f)));
    }

    private Camera LoadWorld(CancellationToken cancellationToken, UWorld original, Transform transform)
    {
        var cam = new Camera(new Vector3(0f, 5f, 5f), Vector3.Zero, 0.01f, 1000f, 5f);
        if (original.PersistentLevel.Load<ULevel>() is not { } persistentLevel)
            return cam;

        var length = persistentLevel.Actors.Length;
        for (var i = 0; i < length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (persistentLevel.Actors[i].Load() is not { } actor ||
                actor.ExportType is "LODActor" or "SplineMeshActor")
                continue;

            Services.ApplicationService.ApplicationView.Status.UpdateStatusLabel($"{original.Name} ... {i}/{length}");
            WorldCamera(actor, ref cam);
            WorldMesh(actor, transform);
            AdditionalWorlds(actor, transform.Matrix, cancellationToken);
        }
        Services.ApplicationService.ApplicationView.Status.UpdateStatusLabel($"{original.Name} ... {length}/{length}");
        return cam;
    }

    private void WorldCamera(UObject actor, ref Camera cam)
    {
        if (actor.ExportType != "LevelBounds" || !actor.TryGetValue(out FPackageIndex boxComponent, "BoxComponent") ||
            boxComponent.Load() is not { } boxObject) return;

        var direction = boxObject.GetOrDefault("RelativeLocation", FVector.ZeroVector).ToMapVector() * Constants.SCALE_DOWN_RATIO;
        var position = boxObject.GetOrDefault("RelativeScale3D", FVector.OneVector).ToMapVector() / 2f * Constants.SCALE_DOWN_RATIO;
        var far = position.AbsMax();
        cam = new Camera(
            new Vector3(position.X, position.Y, position.Z),
            new Vector3(direction.X, direction.Y, direction.Z),
            0.01f, far * 25f, Math.Max(5f, far / 10f));
    }

    private void WorldMesh(UObject actor, Transform transform)
    {
        if (!actor.TryGetValue(out FPackageIndex staticMeshComponent, "StaticMeshComponent", "Mesh") ||
            staticMeshComponent.Load() is not { } staticMeshComp) return;

        if (!staticMeshComp.TryGetValue(out FPackageIndex staticMesh, "StaticMesh") && actor.Class is UBlueprintGeneratedClass)
            foreach (var actorExp in actor.Class.Owner.GetExports())
                if (actorExp.TryGetValue(out staticMesh, "StaticMesh"))
                    break;

        if (staticMesh?.Load() is not UStaticMesh m)
            return;

        var guid = m.LightingGuid;
        var t = new Transform
        {
            Relation = transform.Matrix,
            Position = staticMeshComp.GetOrDefault("RelativeLocation", FVector.ZeroVector).ToMapVector() * Constants.SCALE_DOWN_RATIO,
            Rotation = staticMeshComp.GetOrDefault("RelativeRotation", FRotator.ZeroRotator),
            Scale = staticMeshComp.GetOrDefault("RelativeScale3D", FVector.OneVector).ToMapVector()
        };

        if (Cache.Models.TryGetValue(guid, out var model))
        {
            model.AddInstance(t);
        }
        else if (m.TryConvert(out var mesh))
        {
            model = new Model(m.Name, m.ExportType, m.Materials, mesh, t);
            if (actor.TryGetAllValues(out FPackageIndex[] textureData, "TextureData"))
            {
                for (int j = 0; j < textureData.Length; j++)
                {
                    if (!model.Materials[model.Sections[j].MaterialIndex].IsUsed ||
                        textureData[j].Load() is not { } textureDataIdx)
                        continue;

                    if (textureDataIdx.TryGetValue(out FPackageIndex overrideMaterial, "OverrideMaterial") &&
                        overrideMaterial.TryLoad(out var material) && material is UMaterialInterface unrealMaterial)
                        model.Materials[model.Sections[j].MaterialIndex].SwapMaterial(unrealMaterial);

                    if (textureDataIdx.TryGetValue(out FPackageIndex diffuse, "Diffuse") &&
                        diffuse.Load() is UTexture2D diffuseTexture)
                        model.Materials[model.Sections[j].MaterialIndex].Parameters.Textures[CMaterialParams2.Diffuse[0][0]] = diffuseTexture;
                    if (textureDataIdx.TryGetValue(out FPackageIndex normal, "Normal") &&
                        normal.Load() is UTexture2D normalTexture)
                        model.Materials[model.Sections[j].MaterialIndex].Parameters.Textures[CMaterialParams2.Normals[0][0]] = normalTexture;
                    if (textureDataIdx.TryGetValue(out FPackageIndex specular, "Specular") &&
                        specular.Load() is UTexture2D specularTexture)
                        model.Materials[model.Sections[j].MaterialIndex].Parameters.Textures[CMaterialParams2.SpecularMasks[0][0]] = specularTexture;
                }
            }
            if (staticMeshComp.TryGetValue(out FPackageIndex[] overrideMaterials, "OverrideMaterials"))
            {
                var max = model.Sections.Length - 1;
                for (var j = 0; j < overrideMaterials.Length; j++)
                {
                    if (j > max) break;
                    if (!model.Materials[model.Sections[j].MaterialIndex].IsUsed ||
                        overrideMaterials[j].Load() is not UMaterialInterface unrealMaterial) continue;
                    model.Materials[model.Sections[j].MaterialIndex].SwapMaterial(unrealMaterial);
                }
            }
            Cache.Models[guid] = model;
        }
    }

    private void AdditionalWorlds(UObject actor, Matrix4x4 relation, CancellationToken cancellationToken)
    {
        if (!actor.TryGetValue(out FSoftObjectPath[] additionalWorlds, "AdditionalWorlds") ||
            !actor.TryGetValue(out FPackageIndex staticMeshComponent, "StaticMeshComponent", "Mesh") ||
            staticMeshComponent.Load() is not { } staticMeshComp)
            return;

        var transform = new Transform
        {
            Relation = relation,
            Position = staticMeshComp.GetOrDefault("RelativeLocation", FVector.ZeroVector).ToMapVector() * Constants.SCALE_DOWN_RATIO,
            Rotation = staticMeshComp.GetOrDefault("RelativeRotation", FRotator.ZeroRotator),
            Scale = FVector.OneVector.ToMapVector()
        };

        for (int j = 0; j < additionalWorlds.Length; j++)
            if (Creator.Utils.TryLoadObject(additionalWorlds[j].AssetPathName.Text, out UWorld w))
                LoadWorld(cancellationToken, w, transform);
    }

    public void Dispose()
    {
        _shader.Dispose();
        _outline.Dispose();
        Picking.Dispose();
        Cache.Dispose();
    }
}
