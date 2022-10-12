using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Meshes;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FModel.Views.Snooper;

public class FWindow : GameWindow
{
    private SnimGui _imGui;
    private Camera _camera;
    private IMouse _mouse;
    private Image _icon;
    private Options _options;

    private readonly FramebufferObject _framebuffer;
    private readonly Skybox _skybox;
    private readonly Grid _grid;

    private Shader _shader;
    private Shader _outline;
    private Vector3 _diffuseLight;
    private Vector3 _specularLight;
    private readonly Dictionary<FGuid, Model> _models;

    private float _previousSpeed;

    public FWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
    {
        _options = new Options();
        _framebuffer = new FramebufferObject(Size);
        _skybox = new Skybox();
        _grid = new Grid();
        _models = new Dictionary<FGuid, Model>();
    }

    public void Run(CancellationToken cancellationToken, UObject export)
    {
        switch (export)
        {
            case UStaticMesh st:
            {
                var guid = st.LightingGuid;
                if (!_models.TryGetValue(guid, out _) && st.TryConvert(out var mesh))
                {
                    _models[guid] = new Model(export, st.Name, st.ExportType, mesh.LODs[0], mesh.LODs[0].Verts);
                    SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
                    _options.SelectModel(guid);
                }
                break;
            }
            case USkeletalMesh sk:
            {
                var guid = Guid.NewGuid();
                if (!_models.TryGetValue(guid, out _) && sk.TryConvert(out var mesh))
                {
                    _models[guid] = new Model(export, sk.Name, sk.ExportType, mesh.LODs[0], mesh.LODs[0].Verts, sk.MorphTargets, mesh.RefSkeleton);
                    SetupCamera(mesh.BoundingBox *= Constants.SCALE_DOWN_RATIO);
                    _options.SelectModel(guid);
                }
                break;
            }
            case UMaterialInstance mi:
            {
                var guid = Guid.NewGuid();
                if (!_models.TryGetValue(guid, out _))
                {
                    _models[guid] = new Cube(export, mi.Name, mi.ExportType, mi);
                    SetupCamera(new FBox(new FVector(-.65f), new FVector(.65f)));
                }
                break;
            }
            case UWorld wd:
            {
                var persistentLevel = wd.PersistentLevel.Load<ULevel>();
                var length = persistentLevel.Actors.Length;
                for (var i = 0; i < length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (persistentLevel.Actors[i].Load() is not { } actor || actor.ExportType == "LODActor" ||
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

                    if (_models.TryGetValue(guid, out var model))
                    {
                        model.AddInstance(transform);
                    }
                    else if (m.TryConvert(out var mesh))
                    {
                        model = new Model(export, m.Name, m.ExportType, mesh.LODs[0], mesh.LODs[0].Verts, null, null, transform);

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

                        _models[guid] = model;
                    }
                }
                _camera = new Camera(new Vector3(0f, 5f, 5f), Vector3.Zero, 0.01f, 1000f, 5f);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(export));
        }

        DoLoop();
    }

    public void SwapMaterial(UMaterialInstance mi)
    {
        if (!_models.TryGetValue(_options.SelectedModel, out var model) ||
            !_options.TryGetSection(model, out var section)) return;

        section.SwapMaterial(mi);
        _options.SwapMaterial(false);
        DoLoop();
    }

    private void DoLoop()
    {
        if (_options.Append) _options.Append = false;
        _window.Run();
        // if (_window.IsInitialized)
        // {
        //     if (!_window.GLContext.IsCurrent)
        //     {
        //         _window.GLContext.MakeCurrent();
        //     }
        //
        //     _append = false;
        //     _window.IsVisible = true;
        //     var model = _models.Last();
        //     model.Value.Setup(_gl);
        //     _imGui.Increment(model.Key);
        // }
        // else _window.Initialize();
        //
        // while (!_window.IsClosing && _window.IsVisible)
        // {
        //     _window.DoEvents();
        //     if (!_window.IsClosing && _window.IsVisible)
        //         _window.DoUpdate();
        //     if (_window.IsClosing || !_window.IsVisible)
        //         return;
        //     _window.DoRender();
        // }
        //
        // _window.DoEvents();
        // if (_window.IsClosing) _window.Reset();
    }

    private void SetupCamera(FBox box)
    {
        var far = box.Max.Max();
        var center = box.GetCenter();
        var position = new Vector3(0f, center.Z, box.Max.Y * 3);
        var speed = far / 2f;
        if (speed > _previousSpeed)
        {
            _camera = new Camera(position, center, 0.01f, far * 50f, speed);
            _previousSpeed = _camera.Speed;
        }
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        _window.SetWindowIcon(ref _icon);
        _window.Center();

        var input = _window.CreateInput();
        _keyboard = input.Keyboards[0];
        _mouse = input.Mice[0];

        _gl = GL.GetApi(_window);
        _gl.Enable(EnableCap.Blend);
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.Multisample);
        _gl.StencilOp(StencilOp.Keep, StencilOp.Replace, StencilOp.Replace);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _imGui = new SnimGui(_gl, _window, input);

        _framebuffer.Setup();
        _skybox.Setup();
        _grid.Setup();

        _shader = new Shader();
        _outline = new Shader("outline");
        _diffuseLight = new Vector3(0.75f);
        _specularLight = new Vector3(0.5f);
        foreach (var model in _models.Values)
        {
            model.Setup();
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        _imGui.Update((float) args.Time);

        ClearWhatHasBeenDrawn(); // in main window

        _framebuffer.Bind(); // switch to dedicated window
        ClearWhatHasBeenDrawn(); // in dedicated window

        _skybox.Bind(_camera);
        _grid.Bind(_camera);

        var viewMatrix = _camera.GetViewMatrix();
        var projMatrix = _camera.GetProjectionMatrix();

        _outline.Use();
        _outline.SetUniform("uView", viewMatrix);
        _outline.SetUniform("uProjection", projMatrix);
        _outline.SetUniform("viewPos", _camera.Position);

        _shader.Use();
        _shader.SetUniform("uView", viewMatrix);
        _shader.SetUniform("uProjection", projMatrix);
        _shader.SetUniform("viewPos", _camera.Position);

        _shader.SetUniform("material.diffuseMap", 0);
        _shader.SetUniform("material.normalMap", 1);
        _shader.SetUniform("material.specularMap", 2);
        _shader.SetUniform("material.emissionMap", 3);

        _shader.SetUniform("light.position", _camera.Position);
        _shader.SetUniform("light.diffuse", _diffuseLight);
        _shader.SetUniform("light.specular", _specularLight);

        foreach (var model in _models.Values.Where(model => model.Show))
        {
            model.Bind(_shader);
        }
        GL.Enable(EnableCap.StencilTest); // I don't get why this must be here but it works now so...
        foreach (var model in _models.Values.Where(model => model.IsSelected && model.Show))
        {
            model.Outline(_outline);
        }

        _imGui.Construct(ref _options, _size, _framebuffer, _camera, _mouse, _models);

        _framebuffer.BindMsaa();
        _framebuffer.Bind(0); // switch back to main window
        _framebuffer.BindStuff();

        _imGui.Render(); // render ImGui in main window

        SwapBuffers();
    }

    private void ClearWhatHasBeenDrawn()
    {
        GL.ClearColor(1.0f, 0.102f, 0.129f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        if (!IsFocused || ImGui.GetIO().WantTextInput)
            return;

        var multiplier = KeyboardState.IsKeyPressed(Keys.LeftShift) ? 2f : 1f;
        var moveSpeed = _camera.Speed * multiplier * (float) e.Time;
        if (KeyboardState.IsKeyPressed(Keys.W))
            _camera.Position += moveSpeed * _camera.Direction;
        if (KeyboardState.IsKeyPressed(Keys.S))
            _camera.Position -= moveSpeed * _camera.Direction;
        if (KeyboardState.IsKeyPressed(Keys.A))
            _camera.Position -= Vector3.Normalize(Vector3.Cross(_camera.Direction, _camera.Up)) * moveSpeed;
        if (KeyboardState.IsKeyPressed(Keys.D))
            _camera.Position += Vector3.Normalize(Vector3.Cross(_camera.Direction, _camera.Up)) * moveSpeed;
        if (KeyboardState.IsKeyPressed(Keys.E))
            _camera.Position += moveSpeed * _camera.Up;
        if (KeyboardState.IsKeyPressed(Keys.Q))
            _camera.Position -= moveSpeed * _camera.Up;
        if (KeyboardState.IsKeyPressed(Keys.X))
            _camera.ModifyZoom(-.5f);
        if (KeyboardState.IsKeyPressed(Keys.C))
            _camera.ModifyZoom(+.5f);

        if (KeyboardState.IsKeyPressed(Keys.H))
            IsVisible = false;
        if (KeyboardState.IsKeyPressed(Keys.Escape))
            Close();
    }

    private void OnClose()
    {
        _framebuffer.Dispose();
        _grid.Dispose();
        _skybox.Dispose();
        _shader.Dispose();
        _outline.Dispose();
        foreach (var model in _models.Values)
        {
            model.Dispose();
        }
        if (!_options.Append)
        {
            _models.Clear();
            _options.Reset();
            _previousSpeed = 0f;
        }
        _imGui.Dispose();
        _window.Dispose();
        _gl.Dispose();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, Size.X, Size.Y);
        _camera.AspectRatio = Size.X / (float)Size.Y;
    }
}
