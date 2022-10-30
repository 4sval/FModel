using System;
using System.Collections.Generic;
using System.Windows;
using CUE4Parse.UE4.Objects.Core.Misc;
using FModel.Framework;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace FModel.Views.Snooper;

public class SnimGui : IDisposable
{
    private readonly ImGuiController _controller;

    private readonly Vector2 _outlinerSize;
    private readonly Vector2 _outlinerPosition;
    private readonly Vector2 _propertiesSize;
    private readonly Vector2 _propertiesPosition;
    private readonly Vector2 _viewportSize;
    private readonly Vector2 _viewportPosition;
    private readonly Vector2 _textureSize;
    private readonly Vector2 _texturePosition;
    private bool _viewportFocus;

    private const ImGuiWindowFlags _noResize = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove; // delete once we have a proper docking branch
    private const ImGuiCond _firstUse = ImGuiCond.Appearing; // switch to FirstUseEver once the docking branch will not be useful anymore...
    private const uint _dockspaceId = 1337;

    public SnimGui(int width, int height)
    {
        _controller = new ImGuiController(width, height);

        var style = ImGui.GetStyle();
        var viewport = ImGui.GetMainViewport();
        var titleBarHeight = ImGui.GetFontSize() + style.FramePadding.Y * 2;

        // _outlinerSize = new Vector2(400, 300);
        // _outlinerPosition = new Vector2(viewport.WorkSize.X - _outlinerSize.X, titleBarHeight);
        // _propertiesSize = _outlinerSize with { Y = viewport.WorkSize.Y - _outlinerSize.Y - titleBarHeight };
        // _propertiesPosition = new Vector2(viewport.WorkSize.X - _propertiesSize.X, _outlinerPosition.Y + _outlinerSize.Y);
        _viewportSize = new Vector2(width, height);
        _viewportPosition = new Vector2(0, titleBarHeight);
        // _textureSize = _viewportSize with { Y = viewport.WorkSize.Y - _viewportSize.Y - titleBarHeight };
        // _texturePosition = new Vector2(0, _viewportPosition.Y + _viewportSize.Y);

        Theme(style);
    }

    public void Render(Vector2i size, FramebufferObject framebuffer, Camera camera)
    {
        DrawDockSpace(size);

        DrawNavbar();
        ImGui.Begin("Outliner");
        ImGui.End();
        ImGui.Begin("Properties");
        ImGui.End();
        ImGui.Begin("UV Editor");
        ImGui.End();
        ImGui.Begin("Timeline");
        ImGui.End();
        Draw3DViewport(framebuffer, camera);

        _controller.Render();

        ImGuiController.CheckGLError("End of frame");
    }

    private void DrawDockSpace(Vector2i size)
    {
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        ImGui.SetNextWindowPos(new Vector2(0, 0));
        ImGui.SetNextWindowSize(new Vector2(size.X, size.Y));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("Oui oui", flags);
        ImGui.PopStyleVar();
        ImGui.DockSpace(_dockspaceId);
    }

    private void DrawNavbar()
    {
        if (!ImGui.BeginMainMenuBar()) return;

        if (ImGui.BeginMenu("Window"))
        {
            ImGui.MenuItem("Append", "R");
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

    private void Draw3DViewport(FramebufferObject framebuffer, Camera camera)
    {
        const float lookSensitivity = 0.1f;
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysUseWindowPadding;

        // ImGui.SetNextWindowSize(_viewportSize, _firstUse);
        // ImGui.SetNextWindowPos(_viewportPosition, _firstUse);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("3D Viewport");
        ImGui.PopStyleVar();

        var largest = ImGui.GetContentRegionAvail();
        largest.X -= ImGui.GetScrollX();
        largest.Y -= ImGui.GetScrollY();

        var size = new Vector2(largest.X, largest.Y);
        camera.AspectRatio = size.X / size.Y;
        ImGui.ImageButton(framebuffer.GetPointer(), size, new Vector2(0, 1), new Vector2(1, 0), 0);

        // it took me 5 hours to make it work, don't change any of the following code
        // basically the Raw cursor doesn't actually freeze the mouse position
        // so for ImGui, the IsItemHovered will be false if mouse leave, even in Raw mode
        // var io = ImGui.GetIO();
        // if (ImGui.IsItemHovered())
        // {
        //     // if right button down while mouse is hover viewport
        //     if (ImGui.IsMouseDown(ImGuiMouseButton.Right) && !_viewportFocus)
        //         _viewportFocus = true;
        // }

        // this can't be inside IsItemHovered! read it as
        // if right mouse button was pressed while hovering the viewport
        // move camera until right mouse button is released
        // no matter where mouse position end up
        // if (ImGui.IsMouseDragging(ImGuiMouseButton.Right, lookSensitivity) && _viewportFocus)
        // {
        //     var delta = io.MouseDelta * lookSensitivity;
        //     camera.ModifyDirection(delta.X, delta.Y);
        //     mouse.Cursor.CursorMode = CursorMode.Raw;
        // }

        // if left button up and mouse was in viewport
        // if (ImGui.IsMouseReleased(ImGuiMouseButton.Right) && _viewportFocus)
        // {
        //     _viewportFocus = false;
        //     mouse.Cursor.CursorMode = CursorMode.Normal;
        // }

        // const float padding = 5f;
        // float framerate = ImGui.GetIO().Framerate;
        // var text = $"FPS: {framerate:0} ({1000.0f / framerate:0.##} ms)";
        // ImGui.SetCursorPos(new Vector2(pos.X + padding, pos.Y + size.Y - ImGui.CalcTextSize(text).Y - padding));
        // ImGui.Text(text);

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

    public void Update(GameWindow wnd, float deltaSeconds) => _controller.Update(wnd, deltaSeconds);
    public void WindowResized(int width, int height) => _controller.WindowResized(width, height);
    public void Dispose() => _controller.Dispose();
}
