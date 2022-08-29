using System.Numerics;
using ImGuiNET;

namespace FModel.Extensions;

public static class ImGuiExtensions
{
    public const float PADDING = 5.0f;
    public static ImGuiStylePtr STYLE = ImGui.GetStyle();

    public static void DrawNavbar()
    {
        if (ImGui.BeginMainMenuBar())
        {
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
    }

    public static void DrawFPS()
    {
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoNav;
        var viewport = ImGui.GetMainViewport();
        var work_pos = viewport.WorkPos;
        var work_size = viewport.WorkSize;

        ImGui.SetNextWindowPos(new Vector2(work_pos.X + PADDING, work_pos.Y + work_size.Y - PADDING), ImGuiCond.Always, new Vector2(0, 1));
        if (ImGui.Begin("FPS Overlay", flags))
        {
            float framerate = ImGui.GetIO().Framerate;
            ImGui.Text($"FPS: {framerate:0} ({1000.0f / framerate:0.##} ms)");
        }
        ImGui.End();
    }

    public static void Theme()
    {
        STYLE.FrameRounding = 4.0f;
        STYLE.GrabRounding = 4.0f;

        STYLE.Colors[(int) ImGuiCol.Text] = new Vector4(0.95f, 0.96f, 0.98f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.TextDisabled] = new Vector4(0.36f, 0.42f, 0.47f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.WindowBg] = new Vector4(0.11f, 0.15f, 0.17f, 0.35f);
        STYLE.Colors[(int) ImGuiCol.ChildBg] = new Vector4(0.15f, 0.18f, 0.22f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.PopupBg] = new Vector4(0.08f, 0.08f, 0.08f, 0.94f);
        STYLE.Colors[(int) ImGuiCol.Border] = new Vector4(0.08f, 0.10f, 0.12f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        STYLE.Colors[(int) ImGuiCol.FrameBg] = new Vector4(0.20f, 0.25f, 0.29f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.FrameBgHovered] = new Vector4(0.12f, 0.20f, 0.28f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.FrameBgActive] = new Vector4(0.09f, 0.12f, 0.14f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.TitleBg] = new Vector4(0.09f, 0.12f, 0.14f, 0.65f);
        STYLE.Colors[(int) ImGuiCol.TitleBgActive] = new Vector4(0.08f, 0.10f, 0.12f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.TitleBgCollapsed] = new Vector4(0.00f, 0.00f, 0.00f, 0.51f);
        STYLE.Colors[(int) ImGuiCol.MenuBarBg] = new Vector4(0.15f, 0.18f, 0.22f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.ScrollbarBg] = new Vector4(0.02f, 0.02f, 0.02f, 0.39f);
        STYLE.Colors[(int) ImGuiCol.ScrollbarGrab] = new Vector4(0.20f, 0.25f, 0.29f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.18f, 0.22f, 0.25f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.ScrollbarGrabActive] = new Vector4(0.09f, 0.21f, 0.31f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.CheckMark] = new Vector4(0.28f, 0.56f, 1.00f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.SliderGrab] = new Vector4(0.28f, 0.56f, 1.00f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.SliderGrabActive] = new Vector4(0.37f, 0.61f, 1.00f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.Button] = new Vector4(0.20f, 0.25f, 0.29f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.ButtonHovered] = new Vector4(0.28f, 0.56f, 1.00f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.ButtonActive] = new Vector4(0.06f, 0.53f, 0.98f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.Header] = new Vector4(0.20f, 0.25f, 0.29f, 0.55f);
        STYLE.Colors[(int) ImGuiCol.HeaderHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
        STYLE.Colors[(int) ImGuiCol.HeaderActive] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.Separator] = new Vector4(0.20f, 0.25f, 0.29f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.SeparatorHovered] = new Vector4(0.10f, 0.40f, 0.75f, 0.78f);
        STYLE.Colors[(int) ImGuiCol.SeparatorActive] = new Vector4(0.10f, 0.40f, 0.75f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.ResizeGrip] = new Vector4(0.26f, 0.59f, 0.98f, 0.25f);
        STYLE.Colors[(int) ImGuiCol.ResizeGripHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.67f);
        STYLE.Colors[(int) ImGuiCol.ResizeGripActive] = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
        STYLE.Colors[(int) ImGuiCol.Tab] = new Vector4(0.11f, 0.15f, 0.17f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.TabHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
        STYLE.Colors[(int) ImGuiCol.TabActive] = new Vector4(0.20f, 0.25f, 0.29f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.TabUnfocused] = new Vector4(0.11f, 0.15f, 0.17f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.TabUnfocusedActive] = new Vector4(0.11f, 0.15f, 0.17f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.PlotLines] = new Vector4(0.61f, 0.61f, 0.61f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.PlotLinesHovered] = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.PlotHistogram] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.PlotHistogramHovered] = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.TextSelectedBg] = new Vector4(0.26f, 0.59f, 0.98f, 0.35f);
        STYLE.Colors[(int) ImGuiCol.DragDropTarget] = new Vector4(1.00f, 1.00f, 0.00f, 0.90f);
        STYLE.Colors[(int) ImGuiCol.NavHighlight] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        STYLE.Colors[(int) ImGuiCol.NavWindowingHighlight] = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
        STYLE.Colors[(int) ImGuiCol.NavWindowingDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
        STYLE.Colors[(int) ImGuiCol.ModalWindowDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);
    }
}
