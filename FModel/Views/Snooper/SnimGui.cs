using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using CUE4Parse.UE4.Objects.Core.Misc;
using FModel.Creator;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace FModel.Views.Snooper;

public class SnimGui : IDisposable
{
    private readonly ImGuiController _controller;
    private readonly GraphicsAPI _api;
    private readonly string _renderer;

    private readonly Vector4 _xAxis = new (1.0f, 0.102f, 0.129f, 1.0f);
    private readonly Vector4 _yAxis = new (0.102f, 0.102f, 1.0f, 1.0f);
    private readonly Vector4 _zAxis = new (0.102f, 0.102f, 0.129f, 1.0f);

    private readonly Vector2 _outlinerSize;
    private readonly Vector2 _outlinerPosition;
    private readonly Vector2 _propertiesSize;
    private readonly Vector2 _propertiesPosition;
    private readonly Vector2 _viewportSize;
    private readonly Vector2 _viewportPosition;
    private readonly Vector2 _textureSize;
    private readonly Vector2 _texturePosition;
    private bool _viewportFocus;
    private FGuid _selectedModel;
    private int _selectedInstance;
    private int _selectedSection;

    private const ImGuiWindowFlags _noResize = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove; // delete once we have a proper docking branch
    private const ImGuiCond _firstUse = ImGuiCond.Appearing; // switch to FirstUseEver once the docking branch will not be useful anymore...
    private const uint _dockspaceId = 1337;

    public SnimGui(GL gl, IWindow window, IInputContext input)
    {
        var fontConfig = new ImGuiFontConfig("C:\\Windows\\Fonts\\segoeui.ttf", 16);
        _controller = new ImGuiController(gl, window, input, fontConfig);
        _api = window.API;
        _renderer = gl.GetStringS(StringName.Renderer);

        var style = ImGui.GetStyle();
        var viewport = ImGui.GetMainViewport();
        var titleBarHeight = ImGui.GetFontSize() + style.FramePadding.Y * 2;

        _outlinerSize = new Vector2(400, 300);
        _outlinerPosition = new Vector2(viewport.WorkSize.X - _outlinerSize.X, titleBarHeight);
        _propertiesSize = _outlinerSize with { Y = viewport.WorkSize.Y - _outlinerSize.Y - titleBarHeight };
        _propertiesPosition = new Vector2(viewport.WorkSize.X - _propertiesSize.X, _outlinerPosition.Y + _outlinerSize.Y);
        _viewportSize = _outlinerPosition with { Y = viewport.WorkSize.Y - titleBarHeight - 150 };
        _viewportPosition = new Vector2(0, titleBarHeight);
        _textureSize = _viewportSize with { Y = viewport.WorkSize.Y - _viewportSize.Y - titleBarHeight };
        _texturePosition = new Vector2(0, _viewportPosition.Y + _viewportSize.Y);
        _selectedModel = new FGuid();
        _selectedInstance = 0;
        _selectedSection = 0;

        Theme(style);
    }

    public void Increment(FGuid guid) => _selectedModel = guid;

    public void Construct(Vector2D<int> size, FramebufferObject framebuffer, Camera camera, IMouse mouse, IDictionary<FGuid, Model> models)
    {
        DrawDockSpace(size);
        DrawNavbar();

        DrawOuliner(camera, models);
        DrawProperties(camera, models);
        DrawTextures(models);
        Draw3DViewport(framebuffer, camera, mouse);
    }

    /// <summary>
    /// absolutely useless at the moment since ImGui.NET lacks DockerBuilder bindinds
    /// </summary>
    private void DrawDockSpace(Vector2D<int> size)
    {
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        ImGui.SetNextWindowPos(new Vector2(0, 0));
        ImGui.SetNextWindowSize(new Vector2(size.X, size.Y));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("Snooper", flags);
        ImGui.PopStyleVar();
        ImGui.DockSpace(_dockspaceId);
    }

    private void DrawNavbar()
    {
        if (!ImGui.BeginMainMenuBar()) return;

        if (ImGui.BeginMenu("Window"))
        {
            ImGui.MenuItem("Append", "H");
            ImGui.MenuItem("Close", "ESC");
            ImGui.EndMenu();
        }
        if (ImGui.BeginMenu("Edit"))
        {
            if (ImGui.MenuItem("Undo", "CTRL+Z")) {}
            if (ImGui.MenuItem("Redo", "CTRL+Y", false, false)) {}  // Disabled item
            ImGui.Separator();
            if (ImGui.MenuItem("Cut", "CTRL+X")) {}
            if (ImGui.MenuItem("Copy", "CTRL+C")) {}
            if (ImGui.MenuItem("Paste", "CTRL+V")) {}
            ImGui.EndMenu();
        }

        const string text = "Press ESC to Exit...";
        ImGui.SetCursorPosX(ImGui.GetWindowViewport().WorkSize.X - ImGui.CalcTextSize(text).X - 5);
        ImGui.TextColored(new Vector4(0.36f, 0.42f, 0.47f, 1.00f), text); // ImGuiCol.TextDisabled

        ImGui.EndMainMenuBar();
    }

    private void DrawOuliner(Camera camera, IDictionary<FGuid, Model> models)
    {
        ImGui.SetNextWindowSize(_outlinerSize, _firstUse);
        ImGui.SetNextWindowPos(_outlinerPosition, _firstUse);
        ImGui.Begin("Scene Hierarchy", _noResize | ImGuiWindowFlags.NoCollapse);

        ImGui.Text("hello world!");
        ImGui.Spacing();

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.CollapsingHeader("Collection"))
        {
            if (ImGui.BeginListBox("", new Vector2(ImGui.GetContentRegionAvail().X, _outlinerSize.Y / 2)))
            {
                var i = 0;
                foreach (var (guid, model) in models)
                {
                    ImGui.PushID(i);
                    model.IsSelected = _selectedModel == guid;
                    if (ImGui.Selectable(model.Name, model.IsSelected))
                    {
                        _selectedModel = guid;
                        _selectedInstance = 0;
                        _selectedSection = 0;
                    }
                    if (ImGui.BeginPopupContextItem())
                    {
                        if (ImGui.Selectable("Deselect"))
                            _selectedModel = Guid.Empty;
                        if (ImGui.Selectable("Delete"))
                            models.Remove(guid);
                        if (ImGui.Selectable("Copy to Clipboard"))
                            Application.Current.Dispatcher.Invoke(delegate
                            {
                                Clipboard.SetText(model.Name);
                            });
                        ImGui.EndPopup();
                    }
                    ImGui.PopID();
                    i++;
                }
                ImGui.EndListBox();
            }
        }

        if (ImGui.CollapsingHeader("Camera"))
        {
            PushStyleCompact();
            string[] modes = { "free cam", "orbital cam" };
            int selectedMode = 0;
            ImGui.Combo("Projection", ref selectedMode, modes, modes.Length);
            ImGui.DragFloat3("Position", ref camera.Position);
            ImGui.DragFloat3("Direction", ref camera.Direction);
            ImGui.DragFloat("Speed", ref camera.Speed, 0.01f);
            ImGui.DragFloat("Zoom", ref camera.Zoom);
            PopStyleCompact();
        }

        ImGui.End();
    }

    private void DrawProperties(Camera camera, IDictionary<FGuid, Model> models)
    {
        ImGui.SetNextWindowSize(_propertiesSize, _firstUse);
        ImGui.SetNextWindowPos(_propertiesPosition, _firstUse);
        ImGui.Begin("Properties", _noResize | ImGuiWindowFlags.NoCollapse);
        if (!models.TryGetValue(_selectedModel, out var model))
            return;

        ImGui.Text($"Entity: ({model.Type}) {model.Name}");
        ImGui.Text($"Guid: {_selectedModel.ToString(EGuidFormats.UniqueObjectGuid)}");
        PushStyleCompact();
        ImGui.Columns(4, "Actions", false);
        if (ImGui.Button("Go To")) camera.Position = model.Transforms[_selectedInstance].Position;
        ImGui.NextColumn(); ImGui.Checkbox("Show", ref model.Show);
        ImGui.NextColumn(); ImGui.BeginDisabled(!model.HasVertexColors); ImGui.Checkbox("Colors", ref model.DisplayVertexColors); ImGui.EndDisabled();
        ImGui.NextColumn(); ImGui.BeginDisabled(!model.HasBones); ImGui.Checkbox("Bones", ref model.DisplayBones); ImGui.EndDisabled();
        ImGui.Columns(1);
        PopStyleCompact();

        ImGui.Separator();

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.BeginTabBar("properties_tab_bar", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("Transform"))
            {
                const int width = 100;
                var speed = camera.Speed / 100;

                PushStyleCompact();
                ImGui.PushID(0); ImGui.BeginDisabled(model.TransformsCount < 2);
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.SliderInt("", ref _selectedInstance, 0, model.TransformsCount - 1, "Instance %i", ImGuiSliderFlags.AlwaysClamp);
                ImGui.EndDisabled(); ImGui.PopID();

                ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                if (ImGui.TreeNode("Location"))
                {
                    ImGui.PushID(1);
                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("X", ref model.Transforms[_selectedInstance].Position.X, speed, 0f, 0f, "%.2f m");

                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("Y", ref model.Transforms[_selectedInstance].Position.Y, speed, 0f, 0f, "%.2f m");

                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("Z", ref model.Transforms[_selectedInstance].Position.Z, speed, 0f, 0f, "%.2f m");

                    ImGui.PopID();
                    ImGui.TreePop();
                }

                ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                if (ImGui.TreeNode("Rotation"))
                {
                    ImGui.PushID(2);
                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("X", ref model.Transforms[_selectedInstance].Rotation.Pitch, .5f, 0f, 0f, "%.1f°");

                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("Y", ref model.Transforms[_selectedInstance].Rotation.Roll, .5f, 0f, 0f, "%.1f°");

                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("Z", ref model.Transforms[_selectedInstance].Rotation.Yaw, .5f, 0f, 0f, "%.1f°");

                    ImGui.PopID();
                    ImGui.TreePop();
                }

                ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                if (ImGui.TreeNode("Scale"))
                {
                    ImGui.PushID(3);
                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("X", ref model.Transforms[_selectedInstance].Scale.X, speed, 0f, 0f, "%.3f");

                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("Y", ref model.Transforms[_selectedInstance].Scale.Y, speed, 0f, 0f, "%.3f");

                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("Z", ref model.Transforms[_selectedInstance].Scale.Z, speed, 0f, 0f, "%.3f");

                    ImGui.PopID();
                    ImGui.TreePop();
                }

                model.UpdateMatrix(_selectedInstance);
                PopStyleCompact();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Materials"))
            {
                PushStyleCompact();
                ImGui.BeginTable("Sections", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable);
                ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.NoHeaderWidth | ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Name");
                ImGui.TableHeadersRow();
                for (var i = 0; i < model.Sections.Length; i++)
                {
                    var section = model.Sections[i];

                    ImGui.PushID(i);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    if (!section.Show)
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(1, 0, 0, .5f)));
                    ImGui.Text(section.Index.ToString("D"));
                    ImGui.TableNextColumn();
                    if (ImGui.Selectable(section.Name, _selectedSection == i, ImGuiSelectableFlags.SpanAllColumns))
                        _selectedSection = i;
                    if (ImGui.BeginPopupContextItem())
                    {
                        if (ImGui.Selectable("Swap"))
                        {

                        }
                        ImGui.EndPopup();
                    }
                    ImGui.PopID();
                }
                ImGui.EndTable();
                PopStyleCompact();
                ImGui.EndTabItem();
            }

            ImGui.BeginDisabled(!model.HasMorphTargets);
            if (ImGui.BeginTabItem("Shape Keys"))
            {
                for (int i = 0; i < model.Morphs.Length; i++)
                {
                    ImGui.PushID(i);
                    ImGui.DragFloat(model.Morphs[i].Name, ref model.Morphs[i].Value, 0.001f, 0.0f, 1.0f, "%.2f", ImGuiSliderFlags.AlwaysClamp);
                    ImGui.PopID();
                }
                ImGui.EndTabItem();
            }
            ImGui.EndDisabled();
        }

        ImGui.End();
    }

    private void DrawTextures(IDictionary<FGuid, Model> models)
    {
        ImGui.SetNextWindowSize(_textureSize, _firstUse);
        ImGui.SetNextWindowPos(_texturePosition, _firstUse);
        ImGui.Begin("Textures", _noResize | ImGuiWindowFlags.NoCollapse);
        if (!models.TryGetValue(_selectedModel, out var model))
            return;

        var section = model.Sections[_selectedSection];
        PushStyleCompact(); ImGui.BeginGroup();
        ImGui.Checkbox("Show", ref section.Show);
        ImGui.Checkbox("Wireframe", ref section.Wireframe);
        ImGui.SetNextItemWidth(50); ImGui.DragFloat("Metallic", ref section.Parameters.MetallicValue, 0.01f, 0.0f, 1.0f, "%.2f");
        ImGui.SetNextItemWidth(50); ImGui.DragFloat("Roughness", ref section.Parameters.RoughnessValue, 0.01f, 0.0f, 1.0f, "%.2f");
        ImGui.EndGroup(); PopStyleCompact(); ImGui.SameLine(); ImGui.BeginGroup();
        if (section.HasDiffuseColor)
        {
            ImGui.SetNextItemWidth(300);
            ImGui.ColorEdit4(section.TexturesLabels[0], ref section.DiffuseColor, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
            if (section.Textures[1] is { } normalMap) DrawTexture(normalMap);
        }
        else
        {
            for (var i = 0;i < section.Textures.Length; i++)
            {
                if (section.Textures[i] is not {} texture)
                    continue;

                DrawTexture(texture);

                if (i == 3) // emissive, show color
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(300);
                    ImGui.ColorEdit4($"{section.TexturesLabels[i]} Color", ref section.EmissionColor, ImGuiColorEditFlags.NoAlpha);
                }
                var text = section.TexturesLabels[i];
                var width = ImGui.GetCursorPos().X;
                ImGui.SetCursorPosX(width + ImGui.CalcTextSize(text).X * 0.5f);
                ImGui.Text(text);
                ImGui.EndGroup();
            }
        }
        ImGui.EndGroup();

        ImGui.End();
    }

    private void DrawTexture(Texture texture)
    {
        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Image(texture.GetPointer(), new Vector2(88), Vector2.Zero, Vector2.One, Vector4.One, new Vector4(1, 1, 1, .5f));
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text($"Type: ({texture.Format}) {texture.Type}:{texture.Name}");
            ImGui.Text($"Texture: {texture.Path}");
            ImGui.Text($"Imported: {texture.ImportedWidth}x{texture.ImportedHeight}");
            ImGui.Text($"Mip Used: {texture.Width}x{texture.Height}");
            ImGui.Spacing();
            ImGui.TextDisabled(texture.Label);
            ImGui.EndTooltip();
        }

        if (ImGui.IsItemClicked())
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                Clipboard.SetText(Utils.FixPath(texture.Path));
                texture.Label = "(?) Copied to Clipboard";
            });
        }
    }

    private void Draw3DViewport(FramebufferObject framebuffer, Camera camera, IMouse mouse)
    {
        const float lookSensitivity = 0.1f;
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysUseWindowPadding;

        ImGui.SetNextWindowSize(_viewportSize, _firstUse);
        ImGui.SetNextWindowPos(_viewportPosition, _firstUse);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin($"Viewport ({_api.API} {_api.Version.MajorVersion}.{_api.Version.MinorVersion}) ({_renderer})", _noResize | flags);
        ImGui.PopStyleVar();

        var largest = ImGui.GetContentRegionAvail();
        largest.X -= ImGui.GetScrollX();
        largest.Y -= ImGui.GetScrollY();

        var width = largest.X;
        var height = width / camera.AspectRatio;
        if (height > largest.Y)
        {
            height = largest.Y;
            width = height * camera.AspectRatio;
        }

        var pos = new Vector2(largest.X / 2f - width / 2f + ImGui.GetCursorPosX(), largest.Y / 2f - height / 2f + ImGui.GetCursorPosY());
        var size = new Vector2(width, height);
        ImGui.SetCursorPos(pos);
        ImGui.ImageButton(framebuffer.GetPointer(), size, new Vector2(0, 1), new Vector2(1, 0), 0);

        // it took me 5 hours to make it work, don't change any of the following code
        // basically the Raw cursor doesn't actually freeze the mouse position
        // so for ImGui, the IsItemHovered will be false if mouse leave, even in Raw mode
        var io = ImGui.GetIO();
        if (ImGui.IsItemHovered())
        {
            camera.ModifyZoom(io.MouseWheel);

            // if left button down while mouse is hover viewport
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left) && !_viewportFocus)
                _viewportFocus = true;
        }

        // this can't be inside IsItemHovered! read it as
        // if left mouse button was pressed while hovering the viewport
        // move camera until left mouse button is released
        // no matter where mouse position end up
        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left, lookSensitivity) && _viewportFocus)
        {
            var delta = io.MouseDelta * lookSensitivity;
            camera.ModifyDirection(delta.X, delta.Y);
            mouse.Cursor.CursorMode = CursorMode.Raw;
        }

        // if left button up and mouse was in viewport
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && _viewportFocus)
        {
            _viewportFocus = false;
            mouse.Cursor.CursorMode = CursorMode.Normal;
        }

        const float padding = 5f;
        float framerate = ImGui.GetIO().Framerate;
        var text = $"FPS: {framerate:0} ({1000.0f / framerate:0.##} ms)";
        ImGui.SetCursorPos(new Vector2(pos.X + padding, pos.Y + size.Y - ImGui.CalcTextSize(text).Y - padding));
        ImGui.Text(text);

        ImGui.End();
    }

    private void PushStyleCompact()
    {
        var style = ImGui.GetStyle();
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, style.FramePadding with { Y = style.FramePadding.Y * 0.6f });
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, style.ItemSpacing with { Y = style.ItemSpacing.Y * 0.6f });
    }

    private void PopStyleCompact() => ImGui.PopStyleVar(2);

    private void Theme(ImGuiStylePtr style)
    {
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        io.ConfigWindowsMoveFromTitleBarOnly = true;
        io.ConfigDockingWithShift = true;

        style.WindowMenuButtonPosition = ImGuiDir.Right;
        style.ScrollbarSize = 10f;
        style.FrameRounding = 3.0f;

        style.Colors[(int) ImGuiCol.Text]                   = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
        style.Colors[(int) ImGuiCol.TextDisabled]           = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        style.Colors[(int) ImGuiCol.WindowBg]               = new Vector4(0.11f, 0.11f, 0.12f, 1.00f);
        style.Colors[(int) ImGuiCol.ChildBg]                = new Vector4(0.15f, 0.15f, 0.19f, 1.00f);
        style.Colors[(int) ImGuiCol.PopupBg]                = new Vector4(0.08f, 0.08f, 0.08f, 0.94f);
        style.Colors[(int) ImGuiCol.Border]                 = new Vector4(0.25f, 0.26f, 0.33f, 1.00f);
        style.Colors[(int) ImGuiCol.BorderShadow]           = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        style.Colors[(int) ImGuiCol.FrameBg]                = new Vector4(0.05f, 0.05f, 0.05f, 0.54f);
        style.Colors[(int) ImGuiCol.FrameBgHovered]         = new Vector4(0.69f, 0.69f, 1.00f, 0.20f);
        style.Colors[(int) ImGuiCol.FrameBgActive]          = new Vector4(0.69f, 0.69f, 1.00f, 0.39f);
        style.Colors[(int) ImGuiCol.TitleBg]                = new Vector4(0.09f, 0.09f, 0.09f, 1.00f);
        style.Colors[(int) ImGuiCol.TitleBgActive]          = new Vector4(0.09f, 0.09f, 0.09f, 1.00f);
        style.Colors[(int) ImGuiCol.TitleBgCollapsed]       = new Vector4(0.05f, 0.05f, 0.05f, 0.51f);
        style.Colors[(int) ImGuiCol.MenuBarBg]              = new Vector4(0.14f, 0.14f, 0.14f, 1.00f);
        style.Colors[(int) ImGuiCol.ScrollbarBg]            = new Vector4(0.02f, 0.02f, 0.02f, 0.53f);
        style.Colors[(int) ImGuiCol.ScrollbarGrab]          = new Vector4(0.31f, 0.31f, 0.31f, 1.00f);
        style.Colors[(int) ImGuiCol.ScrollbarGrabHovered]   = new Vector4(0.41f, 0.41f, 0.41f, 1.00f);
        style.Colors[(int) ImGuiCol.ScrollbarGrabActive]    = new Vector4(0.51f, 0.51f, 0.51f, 1.00f);
        style.Colors[(int) ImGuiCol.CheckMark]              = new Vector4(0.13f, 0.42f, 0.83f, 1.00f);
        style.Colors[(int) ImGuiCol.SliderGrab]             = new Vector4(0.13f, 0.42f, 0.83f, 0.78f);
        style.Colors[(int) ImGuiCol.SliderGrabActive]       = new Vector4(0.13f, 0.42f, 0.83f, 1.00f);
        style.Colors[(int) ImGuiCol.Button]                 = new Vector4(0.05f, 0.05f, 0.05f, 0.54f);
        style.Colors[(int) ImGuiCol.ButtonHovered]          = new Vector4(0.69f, 0.69f, 1.00f, 0.20f);
        style.Colors[(int) ImGuiCol.ButtonActive]           = new Vector4(0.69f, 0.69f, 1.00f, 0.39f);
        style.Colors[(int) ImGuiCol.Header]                 = new Vector4(0.16f, 0.16f, 0.21f, 1.00f);
        style.Colors[(int) ImGuiCol.HeaderHovered]          = new Vector4(0.69f, 0.69f, 1.00f, 0.20f);
        style.Colors[(int) ImGuiCol.HeaderActive]           = new Vector4(0.69f, 0.69f, 1.00f, 0.39f);
        style.Colors[(int) ImGuiCol.Separator]              = new Vector4(0.43f, 0.43f, 0.50f, 0.50f);
        style.Colors[(int) ImGuiCol.SeparatorHovered]       = new Vector4(0.10f, 0.40f, 0.75f, 0.78f);
        style.Colors[(int) ImGuiCol.SeparatorActive]        = new Vector4(0.10f, 0.40f, 0.75f, 1.00f);
        style.Colors[(int) ImGuiCol.ResizeGrip]             = new Vector4(0.13f, 0.42f, 0.83f, 0.39f);
        style.Colors[(int) ImGuiCol.ResizeGripHovered]      = new Vector4(0.12f, 0.41f, 0.81f, 0.78f);
        style.Colors[(int) ImGuiCol.ResizeGripActive]       = new Vector4(0.12f, 0.41f, 0.81f, 1.00f);
        style.Colors[(int) ImGuiCol.Tab]                    = new Vector4(0.15f, 0.15f, 0.19f, 1.00f);
        style.Colors[(int) ImGuiCol.TabHovered]             = new Vector4(0.35f, 0.35f, 0.41f, 0.80f);
        style.Colors[(int) ImGuiCol.TabActive]              = new Vector4(0.23f, 0.24f, 0.29f, 1.00f);
        style.Colors[(int) ImGuiCol.TabUnfocused]           = new Vector4(0.15f, 0.15f, 0.15f, 1.00f);
        style.Colors[(int) ImGuiCol.TabUnfocusedActive]     = new Vector4(0.14f, 0.26f, 0.42f, 1.00f);
        style.Colors[(int) ImGuiCol.DockingPreview]         = new Vector4(0.26f, 0.59f, 0.98f, 0.70f);
        style.Colors[(int) ImGuiCol.DockingEmptyBg]         = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
        style.Colors[(int) ImGuiCol.PlotLines]              = new Vector4(0.61f, 0.61f, 0.61f, 1.00f);
        style.Colors[(int) ImGuiCol.PlotLinesHovered]       = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
        style.Colors[(int) ImGuiCol.PlotHistogram]          = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
        style.Colors[(int) ImGuiCol.PlotHistogramHovered]   = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
        style.Colors[(int) ImGuiCol.TableHeaderBg]          = new Vector4(0.09f, 0.09f, 0.09f, 1.00f);
        style.Colors[(int) ImGuiCol.TableBorderStrong]      = new Vector4(0.69f, 0.69f, 1.00f, 0.20f);
        style.Colors[(int) ImGuiCol.TableBorderLight]       = new Vector4(0.69f, 0.69f, 1.00f, 0.20f);
        style.Colors[(int) ImGuiCol.TableRowBg]             = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        style.Colors[(int) ImGuiCol.TableRowBgAlt]          = new Vector4(1.00f, 1.00f, 1.00f, 0.06f);
        style.Colors[(int) ImGuiCol.TextSelectedBg]         = new Vector4(0.26f, 0.59f, 0.98f, 0.35f);
        style.Colors[(int) ImGuiCol.DragDropTarget]         = new Vector4(1.00f, 1.00f, 0.00f, 0.90f);
        style.Colors[(int) ImGuiCol.NavHighlight]           = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        style.Colors[(int) ImGuiCol.NavWindowingHighlight]  = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
        style.Colors[(int) ImGuiCol.NavWindowingDimBg]      = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
        style.Colors[(int) ImGuiCol.ModalWindowDimBg]       = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);
    }

    public void Update(float deltaTime) => _controller.Update(deltaTime);
    public void Render() => _controller.Render();
    public void Dispose() => _controller.Dispose();
}
