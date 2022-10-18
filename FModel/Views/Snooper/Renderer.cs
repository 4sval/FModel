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
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace FModel.Views.Snooper;

public class Renderer : IDisposable
{
    private Shader _shader;
    private Shader _outline;
    private Vector3 _diffuseLight;
    private Vector3 _specularLight;

    public Cache Cache { get; }
    public Options Settings { get; }

    public Renderer()
    {
        Cache = new Cache();
        Settings = new Options();
    }

    public void Load(CancellationToken cancellationToken, UObject export)
    {
        switch (export)
        {
            case UStaticMesh st:
            {
                LoadStaticMesh(st);
                break;
            }
            case USkeletalMesh sk:
            {
                LoadSkeletalMesh(sk);
                break;
            }
            case UMaterialInstance mi:
            {
                LoadMaterialInstance(mi);
                break;
            }
            case UWorld wd:
            {
                LoadWorld(cancellationToken, wd);
                // _camera = new Camera(new Vector3(0f, 5f, 5f), Vector3.Zero, 0.01f, 1000f, 5f);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(export));
        }
    }

    public void Swap(UMaterialInstance unrealMaterial)
    {
        if (!Cache.TryGetModel(Settings.SelectedModel, out var model) ||
            !Settings.TryGetSection(model, out var section)) return;

        section.SwapMaterial(unrealMaterial);
        Settings.SwapMaterial(false);
    }

    public void Setup()
    {
        _shader = new Shader();
        _outline = new Shader("outline");
        _diffuseLight = new Vector3(0.75f);
        _specularLight = new Vector3(0.5f);

        Cache.Setup();
    }

    public void Render(Camera cam)
    {
        var viewMatrix = cam.GetViewMatrix();
        var projMatrix = cam.GetProjectionMatrix();

        _outline.Use();
        _outline.SetUniform("uView", viewMatrix);
        _outline.SetUniform("uProjection", projMatrix);
        _outline.SetUniform("viewPos", cam.Position);

        _shader.Use();
        _shader.SetUniform("uView", viewMatrix);
        _shader.SetUniform("uProjection", projMatrix);
        _shader.SetUniform("viewPos", cam.Position);

        _shader.SetUniform("material.diffuseMap", 0);
        _shader.SetUniform("material.normalMap", 1);
        _shader.SetUniform("material.specularMap", 2);
        _shader.SetUniform("material.emissionMap", 3);

        _shader.SetUniform("light.position", cam.Position);
        _shader.SetUniform("light.diffuse", _diffuseLight);
        _shader.SetUniform("light.specular", _specularLight);

        Cache.Render(_shader);
        GL.Enable(EnableCap.StencilTest); // I don't get why this must be here but it works now so...
        Cache.Outline(_outline);
    }

    // private void SetupCamera(FBox box)
    // {
    //     var far = box.Max.Max();
    //     var center = box.GetCenter();
    //     var position = new Vector3(0f, center.Z, box.Max.Y * 3);
    //     var speed = far / 2f;
    //     if (speed > _previousSpeed)
    //     {
    //         _camera = new Camera(position, center, 0.01f, far * 50f, speed);
    //         _previousSpeed = _camera.Speed;
    //     }
    // }

    private void LoadStaticMesh(UStaticMesh original)
    {
        var guid = original.LightingGuid;
        if (Cache.TryGetModel(guid, out var model))
        {
            model.AddInstance(Transform.Identity);
        }
        else if (original.TryConvert(out var mesh))
        {
            Cache.AddModel(guid, new Model(original.Name, original.ExportType, mesh));
            // SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
            Settings.SelectModel(guid);
        }
    }

    private void LoadSkeletalMesh(USkeletalMesh original)
    {
        var guid = Guid.NewGuid();
        if (Cache.HasModel(guid) || !original.TryConvert(out var mesh)) return;

        Cache.AddModel(guid, new Model(original.Name, original.ExportType, original.MorphTargets, mesh));
        // SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
        Settings.SelectModel(guid);
    }

    private void LoadMaterialInstance(UMaterialInstance original)
    {
        var guid = Guid.NewGuid();
        if (Cache.HasModel(guid)) return;

        Cache.AddModel(guid, new Cube(original));
        // SetupCamera(new FBox(new FVector(-.65f), new FVector(.65f)));
        Settings.SelectModel(guid);
    }

    private void LoadWorld(CancellationToken cancellationToken, UWorld original)
    {
        if (original.PersistentLevel.Load<ULevel>() is not { } persistentLevel)
            return;

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
                model = new Model(m.Name, m.ExportType, mesh, transform);

                if (actor.TryGetAllValues(out FPackageIndex[] textureData, "TextureData"))
                {
                    for (int j = 0; j < textureData.Length; j++)
                    {
                        if (textureData[j].Load() is not { } textureDataIdx)
                            continue;

                        if (textureDataIdx.TryGetValue(out FPackageIndex diffuse, "Diffuse") &&
                            diffuse.Load() is UTexture2D diffuseTexture)
                            model.Sections[j].Parameters.Diffuse = diffuseTexture;
                        if (textureDataIdx.TryGetValue(out FPackageIndex normal, "Normal") &&
                            normal.Load() is UTexture2D normalTexture)
                            model.Sections[j].Parameters.Normal = normalTexture;
                        if (textureDataIdx.TryGetValue(out FPackageIndex specular, "Specular") &&
                            specular.Load() is UTexture2D specularTexture)
                            model.Sections[j].Parameters.Specular = specularTexture;
                    }
                }
                if (staticMeshComp.TryGetValue(out FPackageIndex[] overrideMaterials, "OverrideMaterials"))
                {
                    var max = model.Sections.Length - 1;
                    for (var j = 0; j < overrideMaterials.Length; j++)
                    {
                        if (j > max) break;
                        if (overrideMaterials[j].Load() is not UMaterialInterface unrealMaterial) continue;
                        model.Sections[j].SwapMaterial(unrealMaterial);
                    }
                }

                Cache.AddModel(guid, model);
            }
        }
    }

    public void Dispose()
    {
        _shader.Dispose();
        _outline.Dispose();
        Cache.Dispose();
    }
}
