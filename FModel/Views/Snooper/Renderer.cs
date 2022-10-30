using System;
using System.Threading;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using OpenTK.Mathematics;

namespace FModel.Views.Snooper;

public class Renderer : IDisposable
{
    private Shader _shader;
    // private Shader _outline; // fix stutter
    private Vector3 _diffuseLight;
    private Vector3 _specularLight;

    public Cache Cache { get; }
    public Options Settings { get; }

    public Renderer()
    {
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
            UWorld wd => LoadWorld(cancellationToken, wd),
            _ => throw new ArgumentOutOfRangeException(nameof(export))
        };
    }

    public void Swap(UMaterialInstance unrealMaterial)
    {
        if (!Cache.TryGetModel(Settings.SelectedModel, out var model) ||
            !Settings.TryGetSection(model, out var section)) return;

        model.Materials[section.MaterialIndex].SwapMaterial(unrealMaterial);
        Settings.SwapMaterial(false);
    }

    public void Setup()
    {
        _shader = new Shader();
        // _outline = new Shader("outline");
        _diffuseLight = new Vector3(0.75f);
        _specularLight = new Vector3(0.5f);

        Cache.Setup();
    }

    public void Render(Camera cam)
    {
        var viewMatrix = cam.GetViewMatrix();
        var projMatrix = cam.GetProjectionMatrix();

        // _outline.Use();
        // _outline.SetUniform("uView", viewMatrix);
        // _outline.SetUniform("uProjection", projMatrix);
        // _outline.SetUniform("viewPos", cam.Position);

        _shader.Use();
        _shader.SetUniform("uView", viewMatrix);
        _shader.SetUniform("uProjection", projMatrix);
        _shader.SetUniform("viewPos", cam.Position);

        _shader.SetUniform("light.position", cam.Position);
        _shader.SetUniform("light.diffuse", _diffuseLight);
        _shader.SetUniform("light.specular", _specularLight);

        Cache.Render(_shader);
        // GL.Enable(EnableCap.StencilTest); // I don't get why this must be here but it works now so...
        // Cache.Outline(_outline);
    }

    private Camera SetupCamera(FBox box)
    {
        var far = box.Max.Max();
        var center = box.GetCenter();
        return new Camera(
            new Vector3(0f, center.Z, box.Max.Y * 3),
            new Vector3(center.X, center.Z, center.Y),
            0.01f, far * 50f, far / 2f);
    }

    private Camera LoadStaticMesh(UStaticMesh original)
    {
        var guid = original.LightingGuid;
        if (Cache.TryGetModel(guid, out var model))
        {
            model.AddInstance(Transform.Identity);
            return null;
        }

        if (!original.TryConvert(out var mesh))
            return null;

        Cache.AddModel(guid, new Model(original.Name, original.ExportType, original.Materials, mesh));
        Settings.SelectModel(guid);
        return SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
    }

    private Camera LoadSkeletalMesh(USkeletalMesh original)
    {
        var guid = Guid.NewGuid();
        if (Cache.HasModel(guid) || !original.TryConvert(out var mesh)) return null;

        Cache.AddModel(guid, new Model(original.Name, original.ExportType, original.Materials, original.MorphTargets, mesh));
        Settings.SelectModel(guid);
        return SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
    }

    private Camera LoadMaterialInstance(UMaterialInstance original)
    {
        var guid = Guid.NewGuid();
        if (Cache.HasModel(guid)) return null;

        Cache.AddModel(guid, new Cube(original));
        Settings.SelectModel(guid);
        return SetupCamera(new FBox(new FVector(-.65f), new FVector(.65f)));
    }

    private Camera LoadWorld(CancellationToken cancellationToken, UWorld original)
    {
        var ret = new Camera(new Vector3(0f, 5f, 5f), Vector3.Zero, 0.01f, 1000f, 5f);
        if (original.PersistentLevel.Load<ULevel>() is not { } persistentLevel)
            return ret;

        var length = persistentLevel.Actors.Length;
        for (var i = 0; i < length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (persistentLevel.Actors[i].Load() is not { } actor ||actor.ExportType == "LODActor" ||
                !actor.TryGetValue(out FPackageIndex staticMeshComponent, "StaticMeshComponent") ||
                staticMeshComponent.Load() is not { } staticMeshComp) continue;

            if (!staticMeshComp.TryGetValue(out FPackageIndex staticMesh, "StaticMesh") && actor.Class is UBlueprintGeneratedClass)
                foreach (var actorExp in actor.Class.Owner.GetExports())
                    if (actorExp.TryGetValue(out staticMesh, "StaticMesh"))
                        break;
            if (staticMesh?.Load() is not UStaticMesh m)
                continue;

            Services.ApplicationService.ApplicationView.Status.UpdateStatusLabel($"Actor {i}/{length}");

            var guid = m.LightingGuid;
            var transform = new Transform
            {
                Position = staticMeshComp.GetOrDefault("RelativeLocation", FVector.ZeroVector) * Constants.SCALE_DOWN_RATIO,
                Rotation = staticMeshComp.GetOrDefault("RelativeRotation", FRotator.ZeroRotator),
                Scale = staticMeshComp.GetOrDefault("RelativeScale3D", FVector.OneVector)
            };
            transform.Rotation.Yaw = -transform.Rotation.Yaw;

            if (Cache.TryGetModel(guid, out var model))
            {
                model.AddInstance(transform);
            }
            else if (m.TryConvert(out var mesh))
            {
                model = new Model(m.Name, m.ExportType, m.Materials, mesh, transform);

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

                Cache.AddModel(guid, model);
            }
        }
        Services.ApplicationService.ApplicationView.Status.UpdateStatusLabel($"Actor {length}/{length}");
        return ret;
    }

    public void Dispose()
    {
        _shader.Dispose();
        // _outline.Dispose();
        Cache.Dispose();
    }
}
