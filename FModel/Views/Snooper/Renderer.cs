using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator;
using FModel.Extensions;
using FModel.Settings;
using FModel.Views.Snooper.Animations;
using FModel.Views.Snooper.Buffers;
using FModel.Views.Snooper.Lights;
using FModel.Views.Snooper.Models;
using FModel.Views.Snooper.Shading;

namespace FModel.Views.Snooper;

public class Renderer : IDisposable
{
    private readonly Skybox _skybox;
    private readonly Grid _grid;
    private Shader _shader;
    private Shader _outline;
    private Shader _light;
    private bool _saveCameraMode;

    public bool ShowSkybox;
    public bool ShowGrid;
    public bool ShowLights;
    public bool AnimateWithRotationOnly;
    public int VertexColor;

    public Camera CameraOp { get; }
    public PickingTexture Picking { get; }
    public Options Options { get; }

    public Renderer(int width, int height)
    {
        _skybox = new Skybox();
        _grid = new Grid();

        CameraOp = new Camera();
        Picking = new PickingTexture(width, height);
        Options = new Options();

        ShowSkybox = UserSettings.Default.ShowSkybox;
        ShowGrid = UserSettings.Default.ShowGrid;
        AnimateWithRotationOnly = UserSettings.Default.AnimateWithRotationOnly;
        VertexColor = 0; // default
    }

    public void Load(CancellationToken cancellationToken, UObject export)
    {
        ShowLights = false;
        _saveCameraMode = export is not UWorld;
        CameraOp.Mode = _saveCameraMode ? UserSettings.Default.CameraMode : Camera.WorldMode.FlyCam;
        switch (export)
        {
            case UStaticMesh st:
                LoadStaticMesh(st);
                break;
            case USkeletalMesh sk:
                LoadSkeletalMesh(sk);
                break;
            case UMaterialInstance mi:
                LoadMaterialInstance(mi);
                break;
            case UWorld wd:
                LoadWorld(cancellationToken, wd, Transform.Identity);
                break;
        }
    }

    public void Swap(UMaterialInstance unrealMaterial)
    {
        if (!Options.TryGetModel(out var model) || !Options.TryGetSection(model, out var section)) return;

        model.Materials[section.MaterialIndex].SwapMaterial(unrealMaterial);
        Application.Current.Dispatcher.Invoke(() => model.Materials[section.MaterialIndex].Setup(Options, model.UvCount));
        Options.SwapMaterial(false);
    }

    public void Animate(UObject anim) => Animate(anim, Options.SelectedModel);
    private void Animate(UObject anim, FGuid guid)
    {
        if (!Options.TryGetModel(guid, out var model) || !model.HasSkeleton)
            return;

        float maxElapsedTime;
        switch (anim)
        {
            case UAnimSequence animSequence when animSequence.Skeleton.TryLoad(out USkeleton skeleton):
            {
                var animSet = skeleton.ConvertAnims(animSequence);
                var animation = new Animation(animSequence.Name, animSet, guid);
                maxElapsedTime = animation.TotalElapsedTime;
                model.Skeleton.Animate(animSet, AnimateWithRotationOnly);
                Options.AddAnimation(animation);
                break;
            }
            case UAnimMontage animMontage when animMontage.Skeleton.TryLoad(out USkeleton skeleton):
            {
                var animSet = skeleton.ConvertAnims(animMontage);
                var animation = new Animation(animMontage.Name, animSet, guid);
                maxElapsedTime = animation.TotalElapsedTime;
                model.Skeleton.Animate(animSet, AnimateWithRotationOnly);
                Options.AddAnimation(animation);

                foreach (var notifyEvent in animMontage.Notifies)
                {
                    if (!notifyEvent.NotifyStateClass.TryLoad(out UObject notifyClass) ||
                        !notifyClass.TryGetValue(out FPackageIndex meshProp, "SkeletalMeshProp", "StaticMeshProp", "Mesh") ||
                        !meshProp.TryLoad(out UObject export)) continue;

                    var t = Transform.Identity;
                    if (notifyClass.TryGetValue(out FTransform offset, "Offset"))
                    {
                        t.Rotation = offset.Rotation;
                        t.Position = offset.Translation * Constants.SCALE_DOWN_RATIO;
                        t.Scale = offset.Scale3D;
                    }

                    switch (export)
                    {
                        case UStaticMesh st:
                        {
                            guid = st.LightingGuid;
                            if (Options.TryGetModel(guid, out var instancedModel))
                                instancedModel.AddInstance(t);
                            else if (st.TryConvert(out var mesh))
                                Options.Models[guid] = new Model(st, mesh, t);
                            break;
                        }
                        case USkeletalMesh sk:
                        {
                            guid = Guid.NewGuid();
                            if (!Options.Models.ContainsKey(guid) && sk.TryConvert(out var mesh))
                                Options.Models[guid] = new Model(sk, mesh, t);
                            break;
                        }
                        default:
                            throw new ArgumentException();
                    }

                    if (!Options.TryGetModel(guid, out var addedModel))
                        continue;

                    addedModel.IsAnimatedProp = true;
                    if (notifyClass.TryGetValue(out UObject skeletalMeshPropAnimation, "SkeletalMeshPropAnimation", "Animation"))
                        Animate(skeletalMeshPropAnimation, guid);
                    if (notifyClass.TryGetValue(out FName socketName, "SocketName"))
                    {
                        t = Transform.Identity;
                        if (notifyClass.TryGetValue(out FVector location, "LocationOffset", "Location"))
                            t.Position = location * Constants.SCALE_DOWN_RATIO;
                        if (notifyClass.TryGetValue(out FRotator rotation, "RotationOffset", "Rotation"))
                            t.Rotation = rotation.Quaternion();
                        if (notifyClass.TryGetValue(out FVector scale, "Scale"))
                            t.Scale = scale;

                        var s = new Socket($"TL_{addedModel.Name}", socketName, t, true);
                        model.Sockets.Add(s);
                        addedModel.AttachModel(model, s, new SocketAttachementInfo { Guid = guid, Instance = addedModel.SelectedInstance });
                    }
                }
                break;
            }
            case UAnimComposite animComposite when animComposite.Skeleton.TryLoad(out USkeleton skeleton):
            {
                var animSet = skeleton.ConvertAnims(animComposite);
                var animation = new Animation(animComposite.Name, animSet, guid);
                maxElapsedTime = animation.TotalElapsedTime;
                model.Skeleton.Animate(animSet, AnimateWithRotationOnly);
                Options.AddAnimation(animation);
                break;
            }
            default:
                throw new ArgumentException();
        }

        Options.Tracker.SafeSetMaxElapsedTime(maxElapsedTime);
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

    public void Render(float deltaSeconds)
    {
        var viewMatrix = CameraOp.GetViewMatrix();
        var projMatrix = CameraOp.GetProjectionMatrix();

        if (ShowSkybox) _skybox.Render(viewMatrix, projMatrix);
        if (ShowGrid) _grid.Render(viewMatrix, projMatrix, CameraOp.Near, CameraOp.Far);

        _shader.Render(viewMatrix, CameraOp.Position, projMatrix);
        for (int i = 0; i < 5; i++)
            _shader.SetUniform($"bVertexColors[{i}]", i == VertexColor);

        // update animations
        if (Options.Animations.Count > 0) Options.Tracker.Update(deltaSeconds);
        foreach (var animation in Options.Animations)
        {
            animation.TimeCalculation(Options.Tracker.ElapsedTime);
            foreach (var guid in animation.AttachedModels.Where(guid => Options.Models[guid].HasSkeleton))
            {
                Options.Models[guid].Skeleton.UpdateAnimationMatrices(animation.CurrentSequence, animation.FrameInSequence);
            }
        }

        // render model pass
        foreach (var model in Options.Models.Values)
        {
            model.UpdateMatrices(Options);
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
            _outline.Render(viewMatrix, CameraOp.Position, projMatrix);
            selected.Render(_outline, true);
        }

        // picking pass (dedicated FBO, binding to 0 afterward)
        Picking.Render(viewMatrix, projMatrix, Options.Models);
    }

    private void LoadStaticMesh(UStaticMesh original)
    {
        var guid = original.LightingGuid;
        if (Options.TryGetModel(guid, out var model))
        {
            model.AddInstance(Transform.Identity);
            Application.Current.Dispatcher.Invoke(() => model.SetupInstances());
            return;
        }

        if (!original.TryConvert(out var mesh))
            return;

        Options.Models[guid] = new Model(original, mesh);
        Options.SelectModel(guid);
        SetupCamera(Options.Models[guid].Box);
    }

    private void LoadSkeletalMesh(USkeletalMesh original)
    {
        var guid = new FGuid((uint) original.GetFullName().GetHashCode());
        if (Options.Models.ContainsKey(guid) || !original.TryConvert(out var mesh)) return;

        Options.Models[guid] = new Model(original, mesh);
        Options.SelectModel(guid);
        SetupCamera(Options.Models[guid].Box);
    }

    private void LoadMaterialInstance(UMaterialInstance original)
    {
        if (!Utils.TryLoadObject("Engine/Content/EditorMeshes/EditorCube.EditorCube", out UStaticMesh editorCube))
            return;

        var guid = editorCube.LightingGuid;
        if (Options.TryGetModel(guid, out var model))
        {
            model.Materials[0].SwapMaterial(original);
            Application.Current.Dispatcher.Invoke(() => model.Materials[0].Setup(Options, model.UvCount));
            return;
        }

        if (!editorCube.TryConvert(out var mesh))
            return;

        Options.Models[guid] = new Cube(mesh, original);
        Options.SelectModel(guid);
        SetupCamera(Options.Models[guid].Box);
    }

    private void SetupCamera(FBox box) => CameraOp.Setup(box);

    private void LoadWorld(CancellationToken cancellationToken, UWorld original, Transform transform)
    {
        CameraOp.Setup(new FBox(FVector.ZeroVector, new FVector(0, 10, 10)));
        if (original.PersistentLevel.Load<ULevel>() is not { } persistentLevel)
            return;

        if (persistentLevel.TryGetValue(out FSoftObjectPath runtimeCell, "WorldPartitionRuntimeCell") &&
            Utils.TryLoadObject(runtimeCell.AssetPathName.Text.SubstringBeforeWithLast(".") + runtimeCell.SubPathString.SubstringAfterLast("."), out UObject worldPartition))
        {
            var position = worldPartition.GetOrDefault("Position", FVector.ZeroVector) * Constants.SCALE_DOWN_RATIO;
            var box = worldPartition.GetOrDefault("ContentBounds", new FBox(FVector.ZeroVector, FVector.OneVector));
            box *= MathF.Pow(Constants.SCALE_DOWN_RATIO, 2);
            CameraOp.Teleport(new Vector3(position.X, position.Z, position.Y), box, true);
        }

        var length = persistentLevel.Actors.Length;
        for (var i = 0; i < length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (persistentLevel.Actors[i].Load() is not { } actor ||
                actor.ExportType is "LODActor" or "SplineMeshActor")
                continue;

            Services.ApplicationService.ApplicationView.Status.UpdateStatusLabel($"{original.Name} ... {i}/{length}");
            WorldCamera(actor);
            WorldLight(actor);
            WorldMesh(actor, transform);
            AdditionalWorlds(actor, transform.Matrix, cancellationToken);
        }
        Services.ApplicationService.ApplicationView.Status.UpdateStatusLabel($"{original.Name} ... {length}/{length}");
    }

    private void WorldCamera(UObject actor)
    {
        if (actor.ExportType != "LevelBounds" || !actor.TryGetValue(out FPackageIndex boxComponent, "BoxComponent") ||
            boxComponent.Load() is not { } boxObject) return;

        var direction = boxObject.GetOrDefault("RelativeLocation", FVector.ZeroVector) * Constants.SCALE_DOWN_RATIO;
        var position = boxObject.GetOrDefault("RelativeScale3D", FVector.OneVector) / 2f * Constants.SCALE_DOWN_RATIO;
        CameraOp.Setup(new FBox(direction, position));
    }

    private void WorldLight(UObject actor)
    {
        if (!actor.TryGetValue(out FPackageIndex lightComponent, "LightComponent") ||
            lightComponent.Load() is not { } lightObject) return;

        switch (actor.ExportType)
        {
            case "PointLight":
                Options.Lights.Add(new PointLight(Options.Icons["pointlight"], lightObject));
                break;
            case "SpotLight":
                Options.Lights.Add(new SpotLight(Options.Icons["spotlight"], lightObject));
                break;
            case "RectLight":
            case "SkyLight":
            case "DirectionalLight":
                break;
        }
    }

    private void WorldMesh(UObject actor, Transform transform)
    {
        if (!actor.TryGetValue(out FPackageIndex staticMeshComponent, "StaticMeshComponent", "StaticMesh", "Mesh", "LightMesh") ||
            !staticMeshComponent.TryLoad(out UStaticMeshComponent staticMeshComp) ||
            !staticMeshComp.GetStaticMesh().TryLoad(out UStaticMesh m) || m.Materials.Length < 1)
            return;

        var guid = m.LightingGuid;
        var t = new Transform
        {
            Relation = transform.Matrix,
            Position = staticMeshComp.GetOrDefault("RelativeLocation", FVector.ZeroVector) * Constants.SCALE_DOWN_RATIO,
            Rotation = staticMeshComp.GetOrDefault("RelativeRotation", FRotator.ZeroRotator).Quaternion(),
            Scale = staticMeshComp.GetOrDefault("RelativeScale3D", FVector.OneVector)
        };

        if (Options.TryGetModel(guid, out var model))
        {
            model.AddInstance(t);
        }
        else if (m.TryConvert(out var mesh))
        {
            model = new Model(m, mesh, t);
            model.TwoSided = actor.GetOrDefault("bMirrored", staticMeshComp.GetOrDefault("bDisallowMeshPaintPerInstance", model.TwoSided));

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
            Options.Lights.Add(new PointLight(guid, Options.Icons["pointlight"], pl1, pl2, t));
        }
        if (actor.TryGetValue(out FPackageIndex spotLight, "SpotLight") &&
            spotLight.TryLoad(out var sl1) && sl1.Template.TryLoad(out var sl2))
        {
            Options.Lights.Add(new SpotLight(guid, Options.Icons["spotlight"], sl1, sl2, t));
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
            Position = staticMeshComp.GetOrDefault("RelativeLocation", FVector.ZeroVector) * Constants.SCALE_DOWN_RATIO,
            Rotation = staticMeshComp.GetOrDefault("RelativeRotation", FRotator.ZeroRotator).Quaternion()
        };

        for (int j = 0; j < additionalWorlds.Length; j++)
            if (Utils.TryLoadObject(additionalWorlds[j].AssetPathName.Text, out UWorld w))
                LoadWorld(cancellationToken, w, transform);
    }

    public void WindowResized(int width, int height)
    {
        CameraOp.AspectRatio = width / (float) height;
        Picking.WindowResized(width, height);
    }

    public void Save()
    {
        Options.ResetModelsLightsAnimations();
        Options.SelectModel(Guid.Empty);
        Options.SwapMaterial(false);
        Options.AnimateMesh(false);

        if (_saveCameraMode) UserSettings.Default.CameraMode = CameraOp.Mode;
        UserSettings.Default.ShowSkybox = ShowSkybox;
        UserSettings.Default.ShowGrid = ShowGrid;
        UserSettings.Default.AnimateWithRotationOnly = AnimateWithRotationOnly;
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
