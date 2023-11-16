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
using OpenTK.Windowing.GraphicsLibraryFramework;
using UModel = FModel.Views.Snooper.Models.UModel;

namespace FModel.Views.Snooper;

public enum VertexColor
{
    Default,
    Sections,
    Colors,
    Normals,
    TextureCoordinates
}

public class Renderer : IDisposable
{
    private readonly Skybox _skybox;
    private readonly Grid _grid;
    private Shader _shader;
    private Shader _outline;
    private Shader _light;
    private Shader _bone;
    private bool _saveCameraMode;

    public bool ShowSkybox;
    public bool ShowGrid;
    public bool ShowLights;
    public bool AnimateWithRotationOnly;
    public bool IsSkeletonTreeOpen;
    public VertexColor Color;

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
        Color = VertexColor.Default;
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
            case USkeleton skel:
                LoadSkeleton(skel);
                break;
            case UMaterialInstance mi:
                LoadMaterialInstance(mi);
                break;
            case UWorld wd:
                LoadWorld(cancellationToken, wd, Transform.Identity);
                break;
        }
        SetupCamera();
    }

    public void Swap(UMaterialInstance unrealMaterial)
    {
        if (!Options.TryGetModel(out var model) || !Options.TryGetSection(model, out var section)) return;

        model.Materials[section.MaterialIndex].SwapMaterial(unrealMaterial);
        Application.Current.Dispatcher.Invoke(() => model.Materials[section.MaterialIndex].Setup(Options, model.UvCount));
    }

    public void Animate(UObject anim) => Animate(anim, Options.SelectedModel);
    private void Animate(UObject anim, FGuid guid)
    {
        if (!Options.TryGetModel(guid, out var m) || m is not SkeletalModel model)
            return;

        float maxElapsedTime;
        switch (anim)
        {
            case UAnimSequence animSequence when animSequence.Skeleton.TryLoad(out USkeleton skeleton):
            {
                var animSet = skeleton.ConvertAnims(animSequence);
                var animation = new Animation(animSequence, animSet, guid);
                maxElapsedTime = animation.TotalElapsedTime;
                model.Skeleton.Animate(animSet);
                Options.AddAnimation(animation);
                break;
            }
            case UAnimMontage animMontage when animMontage.Skeleton.TryLoad(out USkeleton skeleton):
            {
                var animSet = skeleton.ConvertAnims(animMontage);
                var animation = new Animation(animMontage, animSet, guid);
                maxElapsedTime = animation.TotalElapsedTime;
                model.Skeleton.Animate(animSet);
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

                    UModel addedModel = null;
                    switch (export)
                    {
                        case UStaticMesh st:
                        {
                            guid = st.LightingGuid;
                            if (Options.TryGetModel(guid, out addedModel))
                            {
                                addedModel.AddInstance(t);
                            }
                            else if (st.TryConvert(out var mesh))
                            {
                                addedModel = new StaticModel(st, mesh, t);
                                Options.Models[guid] = addedModel;
                            }
                            break;
                        }
                        case USkeletalMesh sk:
                        {
                            guid = Guid.NewGuid();
                            if (!Options.Models.ContainsKey(guid) && sk.TryConvert(out var mesh))
                            {
                                addedModel = new SkeletalModel(sk, mesh, t);
                                Options.Models[guid] = addedModel;
                            }
                            break;
                        }
                    }

                    if (addedModel == null)
                        throw new ArgumentException("Unknown model type");

                    addedModel.IsProp = true;
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

                        var s = new Socket($"ANIM_{addedModel.Name}", socketName, t, true);
                        model.Sockets.Add(s);
                        addedModel.Attachments.Attach(model, addedModel.GetTransform(), s,
                            new SocketAttachementInfo { Guid = guid, Instance = addedModel.SelectedInstance });
                    }
                }
                break;
            }
            case UAnimComposite animComposite when animComposite.Skeleton.TryLoad(out USkeleton skeleton):
            {
                var animSet = skeleton.ConvertAnims(animComposite);
                var animation = new Animation(animComposite, animSet, guid);
                maxElapsedTime = animation.TotalElapsedTime;
                model.Skeleton.Animate(animSet);
                Options.AddAnimation(animation);
                break;
            }
            default:
                throw new ArgumentException();
        }

        Options.Tracker.IsPaused = false;
        Options.Tracker.SafeSetMaxElapsedTime(maxElapsedTime);
    }

    public void Setup()
    {
        _skybox.Setup();
        _grid.Setup();

        _shader = new Shader();
        _outline = new Shader("outline");
        _light = new Shader("light");
        _bone = new Shader("bone");

        Picking.Setup();
        Options.SetupModelsAndLights();
    }

    public void Render()
    {
        var viewMatrix = CameraOp.GetViewMatrix();
        var projMatrix = CameraOp.GetProjectionMatrix();

        if (ShowSkybox) _skybox.Render(viewMatrix, projMatrix);
        if (ShowGrid) _grid.Render(viewMatrix, projMatrix, CameraOp.Near, CameraOp.Far);

        _shader.Render(viewMatrix, CameraOp.Position, projMatrix);
        for (int i = 0; i < 5; i++)
            _shader.SetUniform($"bVertexColors[{i}]", i == (int) Color);

        // render model pass
        foreach (var model in Options.Models.Values)
        {
            if (!model.IsVisible) continue;
            model.Render(_shader, Color == VertexColor.TextureCoordinates ? Options.Icons["checker"] : null);
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

        // debug + outline pass
        if (Options.TryGetModel(out var selected) && selected.IsVisible)
        {
            if (IsSkeletonTreeOpen && selected is SkeletalModel skeletalModel)
            {
                _bone.Render(viewMatrix, projMatrix);
                skeletalModel.RenderBones(_bone);
            }

            _outline.Render(viewMatrix, CameraOp.Position, projMatrix);
            selected.Render(_outline, Color == VertexColor.TextureCoordinates ? Options.Icons["checker"] : null, true);
        }

        // picking pass (dedicated FBO, binding to 0 afterward)
        Picking.Render(viewMatrix, projMatrix, Options.Models);
    }

    public void Update(Snooper wnd, float deltaSeconds)
    {
        if (Options.Animations.Count > 0) Options.Tracker.Update(deltaSeconds);
        foreach (var animation in Options.Animations)
        {
            animation.TimeCalculation(Options.Tracker.ElapsedTime);
            foreach (var guid in animation.AttachedModels)
            {
                if (Options.Models[guid] is not SkeletalModel skeletalModel) continue;
                skeletalModel.Skeleton.UpdateAnimationMatrices(animation, AnimateWithRotationOnly);
            }
        }

        {
            foreach (var model in Options.Models.Values)
            {
                model.Update(Options);
            }
            if (IsSkeletonTreeOpen && Options.TryGetModel(out var selected) && selected is SkeletalModel { IsVisible: true } skeletalModel)
            {
                skeletalModel.Skeleton.UpdateVertices();
            }
        }

        CameraOp.Modify(wnd.KeyboardState, deltaSeconds);

        if (wnd.KeyboardState.IsKeyPressed(Keys.Z) &&
            Options.TryGetModel(out var selectedModel) &&
            selectedModel is SkeletalModel)
        {
            Options.RemoveAnimations();
            Options.AnimateMesh(true);
            wnd.WindowShouldClose(true, false);
        }
        if (wnd.KeyboardState.IsKeyPressed(Keys.Space))
            Options.Tracker.IsPaused = !Options.Tracker.IsPaused;
        if (wnd.KeyboardState.IsKeyPressed(Keys.Delete))
            Options.RemoveModel(Options.SelectedModel);
        if (wnd.KeyboardState.IsKeyPressed(Keys.H))
            wnd.WindowShouldClose(true, false);
        if (wnd.KeyboardState.IsKeyPressed(Keys.Escape))
            wnd.WindowShouldClose(true, true);
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

        Options.Models[guid] = new StaticModel(original, mesh);
        Options.SelectModel(guid);
    }

    private void LoadSkeletalMesh(USkeletalMesh original)
    {
        var guid = new FGuid((uint) original.GetFullName().GetHashCode());
        if (Options.Models.ContainsKey(guid) || !original.TryConvert(out var mesh)) return;

        var skeletalModel = new SkeletalModel(original, mesh);
        Options.Models[guid] = skeletalModel;
        Options.SelectModel(guid);
    }

    private void LoadSkeleton(USkeleton original)
    {
        var guid = original.Guid;
        if (Options.Models.ContainsKey(guid) || !original.TryConvert(out _, out var box)) return;

        var fakeSkeletalModel = new SkeletalModel(original, box);
        Options.Models[guid] = fakeSkeletalModel;
        Options.SelectModel(guid);
        IsSkeletonTreeOpen = true;
    }

    private void LoadMaterialInstance(UMaterialInstance original)
    {
        if (!Utils.TryLoadObject("Engine/Content/BasicShapes/Cube.Cube", out UStaticMesh editorCube))
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

        Options.Models[guid] = new StaticModel(original, mesh);
        Options.SelectModel(guid);
    }

    private void SetupCamera()
    {
        if (Options.TryGetModel(out var model))
            CameraOp.Setup(model.Box);
    }

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
        if (actor.TryGetValue(out FPackageIndex[] instanceComponents, "InstanceComponents"))
        {
            foreach (var component in instanceComponents)
            {
                if (!component.TryLoad(out UInstancedStaticMeshComponent staticMeshComp) ||
                    !staticMeshComp.GetStaticMesh().TryLoad(out UStaticMesh m) || m.Materials.Length < 1)
                    continue;

                if (staticMeshComp.PerInstanceSMData is { Length: > 0 })
                {

                    var relation = CalculateTransform(staticMeshComp, transform);
                    foreach (var perInstanceData in staticMeshComp.PerInstanceSMData)
                    {
                        ProcessMesh(actor, staticMeshComp, m, new Transform
                        {
                            Relation = relation.Matrix,
                            Position = perInstanceData.TransformData.Translation * Constants.SCALE_DOWN_RATIO,
                            Rotation = perInstanceData.TransformData.Rotation,
                            Scale = perInstanceData.TransformData.Scale3D
                        });
                    }
                }
                else ProcessMesh(actor, staticMeshComp, m, CalculateTransform(staticMeshComp, transform));
            }
        }
        else if (actor.TryGetValue(out FPackageIndex staticMeshComponent, "StaticMeshComponent", "StaticMesh", "Mesh", "LightMesh") &&
                 staticMeshComponent.TryLoad(out UStaticMeshComponent staticMeshComp) &&
                 staticMeshComp.GetStaticMesh().TryLoad(out UStaticMesh m) && m.Materials.Length > 0)
        {
            ProcessMesh(actor, staticMeshComp, m, CalculateTransform(staticMeshComp, transform));
        }
    }

    private void ProcessMesh(UObject actor, UStaticMeshComponent staticMeshComp, UStaticMesh m, Transform transform)
    {
        var guid = m.LightingGuid;

        OverrideVertexColors(staticMeshComp, m);
        if (Options.TryGetModel(guid, out var model))
        {
            model.AddInstance(transform);
        }
        else if (m.TryConvert(out var mesh))
        {
            model = new StaticModel(m, mesh, transform);
            model.IsTwoSided = actor.GetOrDefault("bMirrored", staticMeshComp.GetOrDefault("bDisallowMeshPaintPerInstance", model.IsTwoSided));

            if (actor.TryGetAllValues(out FPackageIndex[] textureData, "TextureData"))
            {
                var material = model.Materials.FirstOrDefault();
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
                for (var j = 0; j < overrideMaterials.Length && j < model.Sections.Length; j++)
                {
                    var matIndex = model.Sections[j].MaterialIndex;
                    if (matIndex < 0 || matIndex >= model.Materials.Length || matIndex >= overrideMaterials.Length ||
                        overrideMaterials[matIndex].Load() is not UMaterialInterface unrealMaterial) continue;

                    model.Materials[matIndex].SwapMaterial(unrealMaterial);
                }
            }

            Options.Models[guid] = model;
        }

        if (actor.TryGetValue(out FPackageIndex treasureLight, "PointLight", "TreasureLight") &&
            treasureLight.TryLoad(out var pl1) && pl1.Template.TryLoad(out var pl2))
        {
            Options.Lights.Add(new PointLight(guid, Options.Icons["pointlight"], pl1, pl2, transform));
        }
        if (actor.TryGetValue(out FPackageIndex spotLight, "SpotLight") &&
            spotLight.TryLoad(out var sl1) && sl1.Template.TryLoad(out var sl2))
        {
            Options.Lights.Add(new SpotLight(guid, Options.Icons["spotlight"], sl1, sl2, transform));
        }
    }

    private Transform CalculateTransform(UStaticMeshComponent staticMeshComp, Transform relation)
    {
        return new Transform
        {
            Relation = relation.Matrix,
            Position = staticMeshComp.GetOrDefault("RelativeLocation", FVector.ZeroVector) * Constants.SCALE_DOWN_RATIO,
            Rotation = staticMeshComp.GetOrDefault("RelativeRotation", FRotator.ZeroRotator).Quaternion(),
            Scale = staticMeshComp.GetOrDefault("RelativeScale3D", FVector.OneVector)
        };
    }

    private void OverrideVertexColors(UStaticMeshComponent staticMeshComp, UStaticMesh staticMesh)
    {
        if (staticMeshComp.LODData is not { Length: > 0 } || staticMesh.RenderData is not { LODs.Length: > 0 })
            return;

        for (var lod = 0; lod < staticMeshComp.LODData.Length; lod++)
        {
            var vertexColors = staticMeshComp.LODData[lod].OverrideVertexColors;
            if (vertexColors == null) continue;

            staticMesh.RenderData.LODs[lod].ColorVertexBuffer = vertexColors;
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
        _bone?.Dispose();
        Picking?.Dispose();
        Options?.Dispose();
    }
}
