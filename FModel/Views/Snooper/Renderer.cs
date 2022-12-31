using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Settings;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Lights;
using FModel.Views.Snooper.Models;
using FModel.Views.Snooper.Models.Animations;
using FModel.Views.Snooper.Shading;

namespace FModel.Views.Snooper;

public class Renderer : IDisposable
{
    private readonly Skybox _skybox;
    private readonly Grid _grid;
    private Shader _shader;
    private Shader _outline;
    private Shader _light;

    public bool ShowSkybox;
    public bool ShowGrid;
    public bool ShowLights;
    public int VertexColor;

    public PickingTexture Picking { get; }
    public Options Options { get; }

    public Renderer(int width, int height)
    {
        _skybox = new Skybox();
        _grid = new Grid();

        Picking = new PickingTexture(width, height);
        Options = new Options();

        ShowSkybox = UserSettings.Default.ShowSkybox;
        ShowGrid = UserSettings.Default.ShowGrid;
        VertexColor = 0; // default
    }

    public Camera Load(CancellationToken cancellationToken, UObject export)
    {
        ShowLights = false;
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
        if (!Options.TryGetModel(out var model) || !Options.TryGetSection(model, out var section)) return;

        model.Materials[section.MaterialIndex].SwapMaterial(unrealMaterial);
        Application.Current.Dispatcher.Invoke(() => model.Materials[section.MaterialIndex].Setup(Options, model.UvCount));
        Options.SwapMaterial(false);
    }

    public void Animate(UAnimSequence animSequence)
    {
        if (!Options.TryGetModel(out var model) || !model.Skeleton.IsLoaded ||
            model.Skeleton?.RefSkel.ConvertAnims(animSequence) is not { } anim || anim.Sequences.Count == 0)
            return;

        model.Skeleton.Anim = new Animation(anim);
        Options.AnimateMesh(false);
    }

    public void Setup()
    {
        _skybox.Setup();
        _grid.Setup();

        _shader = new Shader();
        _outline = new Shader("outline");
        _light = new Shader("light");

        Picking.Setup();
        Options.SetupModelsAndLights();
    }

    public void Render(Camera cam)
    {
        var viewMatrix = cam.GetViewMatrix();
        var projMatrix = cam.GetProjectionMatrix();

        if (ShowSkybox) _skybox.Render(viewMatrix, projMatrix);
        if (ShowGrid) _grid.Render(viewMatrix, projMatrix, cam.Near, cam.Far);

        _shader.Render(viewMatrix, cam.Position, projMatrix);
        for (int i = 0; i < 5; i++)
            _shader.SetUniform($"bVertexColors[{i}]", i == VertexColor);

        // render model pass
        foreach (var model in Options.Models.Values)
        {
            if (!model.Show) continue;
            model.Render(_shader);
        }

        {   // light pass
            var uNumLights = Math.Min(Options.Lights.Count, 100);
            _shader.SetUniform("uNumLights", ShowLights ? uNumLights : 0);

            if (ShowLights)
                for (int i = 0; i < uNumLights; i++)
                    Options.Lights[i].Render(i, _shader);

            _light.Render(viewMatrix, projMatrix);
            for (int i = 0; i < uNumLights; i++)
                Options.Lights[i].Render(_light);
        }

        // outline pass
        if (Options.TryGetModel(out var selected) && selected.Show)
        {
            _outline.Render(viewMatrix, cam.Position, projMatrix);
            selected.Outline(_outline);
        }

        // picking pass (dedicated FBO, binding to 0 afterward)
        Picking.Render(viewMatrix, projMatrix, Options.Models);
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
        if (Options.TryGetModel(guid, out var model))
        {
            model.AddInstance(Transform.Identity);
            Application.Current.Dispatcher.Invoke(() => model.SetupInstances());
            return null;
        }

        if (!original.TryConvert(out var mesh))
            return null;

        Options.Models[guid] = new Model(original, mesh);
        Options.SelectModel(guid);
        return SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
    }

    private Camera LoadSkeletalMesh(USkeletalMesh original)
    {
        var guid = Guid.NewGuid();
        if (Options.Models.ContainsKey(guid) || !original.TryConvert(out var mesh)) return null;

        Options.Models[guid] = new Model(original, mesh);
        Options.SelectModel(guid);
        return SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
    }

    private Camera LoadMaterialInstance(UMaterialInstance original)
    {
        var guid = Guid.NewGuid();
        if (Options.Models.ContainsKey(guid)) return null;

        Options.Models[guid] = new Cube(original);
        Options.SelectModel(guid);
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
            // WorldLight(actor);
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

    private void WorldLight(UObject actor)
    {
        // if (!actor.TryGetValue(out FPackageIndex lightComponent, "LightComponent") ||
        //     lightComponent.Load() is not { } lightObject) return;
        //
        // Cache.Lights.Add(new PointLight(Cache.Icons["pointlight"], lightObject, FVector.ZeroVector));
    }

    private void WorldMesh(UObject actor, Transform transform)
    {
        if (!actor.TryGetValue(out FPackageIndex staticMeshComponent, "StaticMeshComponent", "StaticMesh", "Mesh", "LightMesh") ||
            staticMeshComponent.Load() is not { } staticMeshComp) return;

        if (!staticMeshComp.TryGetValue(out FPackageIndex staticMesh, "StaticMesh") && actor.Class is UBlueprintGeneratedClass)
            foreach (var actorExp in actor.Class.Owner.GetExports())
                if (actorExp.TryGetValue(out staticMesh, "StaticMesh"))
                    break;

        if (staticMesh?.Load() is not UStaticMesh m || m.Materials.Length < 1)
            return;

        var guid = m.LightingGuid;
        var t = new Transform
        {
            Relation = transform.Matrix,
            Position = staticMeshComp.GetOrDefault("RelativeLocation", FVector.ZeroVector).ToMapVector() * Constants.SCALE_DOWN_RATIO,
            Rotation = staticMeshComp.GetOrDefault("RelativeRotation", FRotator.ZeroRotator),
            Scale = staticMeshComp.GetOrDefault("RelativeScale3D", FVector.OneVector).ToMapVector()
        };

        if (Options.TryGetModel(guid, out var model))
        {
            model.AddInstance(t);
        }
        else if (m.TryConvert(out var mesh))
        {
            model = new Model(m, mesh, t);
            if (actor.TryGetValue(out FPackageIndex baseMaterial, "BaseMaterial") &&
                actor.TryGetAllValues(out FPackageIndex[] textureData, "TextureData"))
            {
                var material = model.Materials.FirstOrDefault(x => x.Name == baseMaterial.Name);
                if (material is { IsUsed: true })
                {
                    for (int j = 0; j < textureData.Length; j++)
                    {
                        if (textureData[j]?.Load() is not { } textureDataIdx)
                            continue;

                        if (textureDataIdx.TryGetValue(out FPackageIndex overrideMaterial, "OverrideMaterial") &&
                            overrideMaterial.TryLoad(out var oMaterial) && oMaterial is UMaterialInterface oUnrealMaterial)
                            material.SwapMaterial(oUnrealMaterial);

                        WorldTextureData(material, textureDataIdx, "Diffuse", j switch
                        {
                            0 => "Diffuse",
                            > 0 => $"Diffuse_Texture_{j + 1}",
                            _ => CMaterialParams2.FallbackDiffuse
                        });
                        WorldTextureData(material, textureDataIdx, "Normal", j switch
                        {
                            0 => "Normals",
                            > 0 => $"Normals_Texture_{j + 1}",
                            _ => CMaterialParams2.FallbackNormals
                        });
                        WorldTextureData(material, textureDataIdx, "Specular", j switch
                        {
                            0 => "SpecularMasks",
                            > 0 => $"SpecularMasks_{j + 1}",
                            _ => CMaterialParams2.FallbackNormals
                        });
                    }
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
            Options.Models[guid] = model;
        }

        if (actor.TryGetValue(out FPackageIndex treasureLight, "PointLight", "TreasureLight") &&
            treasureLight.TryLoad(out var pl1) && pl1.Template.TryLoad(out var pl2))
        {
            Options.Lights.Add(new PointLight(guid, Options.Icons["pointlight"], pl1, pl2, t.Position));
        }
        if (actor.TryGetValue(out FPackageIndex spotLight, "SpotLight") &&
            spotLight.TryLoad(out var sl1) && sl1.Template.TryLoad(out var sl2))
        {
            Options.Lights.Add(new SpotLight(guid, Options.Icons["spotlight"], sl1, sl2, t.Position));
        }
    }

    private void WorldTextureData(Material material, UObject textureData, string name, string key)
    {
        if (textureData.TryGetValue(out FPackageIndex package, name) && package.Load() is UTexture2D texture)
            material.Parameters.Textures[key] = texture;
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

    public void Save()
    {
        UserSettings.Default.ShowSkybox = ShowSkybox;
        UserSettings.Default.ShowGrid = ShowGrid;
    }

    public void Dispose()
    {
        _skybox?.Dispose();
        _grid?.Dispose();
        _shader?.Dispose();
        _outline?.Dispose();
        _light?.Dispose();
        Picking?.Dispose();
        Options?.Dispose();
    }
}
