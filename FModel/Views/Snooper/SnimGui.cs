using System;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using FModel.Framework;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace FModel.Views.Snooper;

public class SnimGui
{
    public readonly ImGuiController Controller;
    private bool _viewportFocus;
    private bool _swapAwareness;

    private readonly Vector4 _accentColor = new (32, 107, 212, 1f);
    private readonly Vector4 _alertColor = new (212, 146, 32, 1f);
    private readonly Vector4 _errorColor = new (194, 43, 43, 1f);

    private const ImGuiCond _firstUse = ImGuiCond.FirstUseEver; // switch to FirstUseEver once the docking branch will not be useful anymore...
    private const uint _dockspaceId = 1337;

    public SnimGui(int width, int height)
    {
        Controller = new ImGuiController(width, height);

        var style = ImGui.GetStyle();
        Theme(style);
    }

    public void Render(Snooper s)
    {
        DrawDockSpace(s.Size);
        DrawNavbar();

        ImGui.Begin("Camera");
        ImGui.DragFloat("Speed", ref s.Camera.Speed, 0.01f, 0.05f);
        ImGui.DragFloat("Far Plane", ref s.Camera.Far, 0.1f, 5f, s.Camera.Far * 2f, "%.2f m", ImGuiSliderFlags.AlwaysClamp);
        ImGui.End();
        ImGui.Begin("World");
        ImGui.Checkbox("Diffuse Only", ref s.Renderer.bDiffuseOnly);
        ImGui.End();
        ImGui.Begin("Timeline");
        ImGui.End();
        if (ImGui.Begin("Materials"))
        {
            PushStyleCompact();
            var guid = s.Renderer.Settings.SelectedModel;
            if (s.Renderer.Cache.Models.TryGetValue(guid, out var model) &&
                s.Renderer.Settings.TryGetSection(model, out var section))
            {
                var material = model.Materials[section.MaterialIndex];
                foreach ((string key, float value) in material.Parameters.Scalars)
                {
                    ImGui.Text($"{key}: {value}");
                }
                ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
                foreach ((string key, FLinearColor value) in material.Parameters.Colors)
                {
                    ImGui.Text(key); ImGui.SameLine();
                    ImGui.ColorButton(key, new Vector4(value.R, value.G, value.B, value.A));
                }
            }
            else NoMeshSelected();
            PopStyleCompact();
            ImGui.End();
        }
        if (ImGui.Begin("Textures"))
        {
            PushStyleCompact();
            var guid = s.Renderer.Settings.SelectedModel;
            if (s.Renderer.Cache.Models.TryGetValue(guid, out var model) &&
                s.Renderer.Settings.TryGetSection(model, out var section))
            {
                var material = model.Materials[section.MaterialIndex];
                foreach ((string key, UUnrealMaterial value) in material.Parameters.Textures)
                {
                    ImGui.Text($"{key}: {value.Name}");
                }
            }
            else NoMeshSelected();
            PopStyleCompact();
            ImGui.End();
        }
        if (ImGui.Begin("Parameters"))
        {
            PushStyleCompact();
            var guid = s.Renderer.Settings.SelectedModel;
            if (s.Renderer.Cache.Models.TryGetValue(guid, out var model) &&
                s.Renderer.Settings.TryGetSection(model, out var section))
            {
                const int width = 50;
                var material = model.Materials[section.MaterialIndex];
                ImGui.Checkbox("Show Section", ref section.Show);
                ImGui.SetNextItemWidth(width); ImGui.DragFloat("Emissive Multiplier", ref material.EmissiveMult, .01f, 0f);
                ImGui.SetNextItemWidth(width); ImGui.DragFloat("UV Scale", ref material.UVScale, .01f, 0f);
                if (material.HasM)
                {
                    ImGui.ColorEdit3("Skin Boost Color", ref material.M.SkinBoost.Color, ImGuiColorEditFlags.NoInputs);
                    ImGui.SetNextItemWidth(width); ImGui.DragFloat("Skin Boost Exponent", ref material.M.SkinBoost.Exponent, .01f, 0f);
                    ImGui.SetNextItemWidth(width); ImGui.DragFloat("AmbientOcclusion", ref material.M.AmbientOcclusion, .01f, 0f, 1f);
                    ImGui.SetNextItemWidth(width); ImGui.DragFloat("Cavity", ref material.M.Cavity, .01f, 0f, 1f);
                }
            }
            else NoMeshSelected();
            PopStyleCompact();
            ImGui.End();
        }

        DrawUvChannels(s);
        DrawTransform(s);
        DrawDetails(s);
        DrawOuliner(s);
        Draw3DViewport(s);
        // last render will always be on top
        // order by decreasing importance

        Controller.Render();

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

    private void DrawOuliner(Snooper s)
    {
        if (ImGui.Begin("Outliner"))
        {
            PushStyleCompact();

            if (ImGui.BeginTable("Items", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Instance", ImGuiTableColumnFlags.NoHeaderWidth | ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Channels", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Name");
                ImGui.TableHeadersRow();

                var i = 0;
                foreach ((FGuid guid, Model model) in s.Renderer.Cache.Models)
                {
                    ImGui.PushID(i);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    if (!model.Show)
                    {
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(1, 0, 0, .5f)));
                    }

                    ImGui.Text(model.TransformsCount.ToString("D"));
                    ImGui.TableNextColumn();
                    ImGui.Text(model.NumTexCoords.ToString("D"));
                    ImGui.TableNextColumn();
                    model.IsSelected = s.Renderer.Settings.SelectedModel == guid;
                    if (ImGui.Selectable(model.Name, model.IsSelected, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        s.Renderer.Settings.SelectModel(guid);
                    }
                    if (ImGui.BeginPopupContextItem())
                    {
                        s.Renderer.Settings.SelectModel(guid);
                        if (ImGui.Selectable("Delete")) s.Renderer.Cache.Models.Remove(guid);
                        if (ImGui.Selectable("Deselect")) s.Renderer.Settings.SelectModel(Guid.Empty);
                        if (ImGui.Selectable("Copy Name to Clipboard")) ImGui.SetClipboardText(model.Name);
                        ImGui.EndPopup();
                    }
                    ImGui.PopID();
                    i++;
                }

                ImGui.EndTable();
            }

            PopStyleCompact();

            ImGui.End();
        }
    }

    private void DrawDetails(Snooper s)
    {
        if (ImGui.Begin("Details"))
        {
            PushStyleCompact();
            var guid = s.Renderer.Settings.SelectedModel;
            if (s.Renderer.Cache.Models.TryGetValue(guid, out var model))
            {
                ImGui.Text($"Entity: ({model.Type}) {model.Name}");
                ImGui.Text($"Guid: {guid.ToString(EGuidFormats.UniqueObjectGuid)}");

                ImGui.Columns(3, "Actions", false);
                if (ImGui.Button("Go To"))
                {
                    var instancePos = model.Transforms[model.SelectedInstance].Position;
                    s.Camera.Position = new Vector3(instancePos.X, instancePos.Y, instancePos.Z);
                }
                ImGui.NextColumn(); ImGui.Checkbox("Show", ref model.Show);
                ImGui.NextColumn(); ImGui.BeginDisabled(!model.Show); ImGui.Checkbox("Wire", ref model.Wireframe); ImGui.EndDisabled();
                ImGui.Columns(4);
                ImGui.NextColumn(); ImGui.BeginDisabled(!model.HasVertexColors); ImGui.Checkbox("Colors", ref model.bVertexColors); ImGui.EndDisabled();
                ImGui.NextColumn(); ImGui.Checkbox("Normals", ref model.bVertexNormals);
                ImGui.NextColumn(); ImGui.Checkbox("Tangent", ref model.bVertexTangent);
                ImGui.NextColumn(); ImGui.Checkbox("Coords", ref model.bVertexTexCoords);
                ImGui.Columns(1);

                ImGui.Separator();

                if (ImGui.BeginTabBar("tabbar_details", ImGuiTabBarFlags.None))
                {
                    if (ImGui.BeginTabItem("Sections") && ImGui.BeginTable("table_sections", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
                    {
                        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed);
                        ImGui.TableSetupColumn("Material");
                        ImGui.TableHeadersRow();

                        var swap = false;
                        for (var i = 0; i < model.Sections.Length; i++)
                        {
                            var section = model.Sections[i];
                            var material = model.Materials[section.MaterialIndex];

                            ImGui.PushID(i);
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            if (!section.Show)
                            {
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(1, 0, 0, .5f)));
                            }

                            ImGui.Text(section.MaterialIndex.ToString("D"));
                            ImGui.TableNextColumn();
                            if (ImGui.Selectable(material.Name, s.Renderer.Settings.SelectedSection == i, ImGuiSelectableFlags.SpanAllColumns))
                            {
                                s.Renderer.Settings.SelectSection(i);
                            }
                            if (ImGui.BeginPopupContextItem())
                            {
                                s.Renderer.Settings.SelectSection(i);
                                if (ImGui.Selectable("Swap"))
                                {
                                    if (_swapAwareness)
                                    {
                                        s.Renderer.Settings.SwapMaterial(true);
                                        s.WindowShouldClose(true, false);
                                    }
                                    else swap = true;
                                }
                                if (ImGui.Selectable("Copy Name to Clipboard")) ImGui.SetClipboardText(material.Name);
                                ImGui.EndPopup();
                            }
                            ImGui.PopID();
                        }
                        ImGui.EndTable();

                        var p_open = true;
                        if (swap) ImGui.OpenPopup("Swap?");
                        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(.5f));
                        if (ImGui.BeginPopupModal("Swap?", ref p_open, ImGuiWindowFlags.AlwaysAutoResize))
                        {
                            ImGui.TextWrapped("You're about to swap a material.\nThe window will close for you to extract a material!\n\n");
                            ImGui.Separator();

                            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                            ImGui.Checkbox("Got it! Don't show me again", ref _swapAwareness);
                            ImGui.PopStyleVar();

                            var size = new Vector2(120, 0);
                            if (ImGui.Button("OK", size))
                            {
                                ImGui.CloseCurrentPopup();
                                s.Renderer.Settings.SwapMaterial(true);
                                s.WindowShouldClose(true, false);
                            }

                            ImGui.SetItemDefaultFocus();
                            ImGui.SameLine();

                            if (ImGui.Button("Cancel", size))
                            {
                                ImGui.CloseCurrentPopup();
                            }

                            ImGui.EndPopup();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Morph Targets"))
                    {
                        if (model.HasMorphTargets)
                        {
                            const float width = 10;
                            var region = ImGui.GetContentRegionAvail();
                            var box = new Vector2(region.X - width, region.Y / 1.5f);

                            if (ImGui.BeginListBox("", box))
                            {
                                for (int i = 0; i < model.Morphs.Length; i++)
                                {
                                    ImGui.PushID(i);
                                    if (ImGui.Selectable(model.Morphs[i].Name, s.Renderer.Settings.SelectedMorph == i))
                                    {
                                        s.Renderer.Settings.SelectMorph(i, model);
                                    }
                                    ImGui.PopID();
                                }
                                ImGui.EndListBox();

                                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2f, 0f));
                                ImGui.SameLine(); ImGui.PushID(99);
                                ImGui.VSliderFloat("", box with { X = width }, ref model.MorphTime, 0.0f, 1.0f, "", ImGuiSliderFlags.AlwaysClamp);
                                ImGui.PopID(); ImGui.PopStyleVar();
                                ImGui.Spacing();
                                ImGui.Text($"Time: {model.MorphTime:P}%");
                            }
                        }
                        else
                        {
                            ImGui.TextColored(_errorColor, "mesh has no morph targets");
                        }

                        ImGui.EndTabItem();
                    }
                }
            }
            else NoMeshSelected();
            PopStyleCompact();
            ImGui.End();
        }
    }

    private void DrawTransform(Snooper s)
    {
        if (ImGui.Begin("Transform"))
        {
            PushStyleCompact();
            if (s.Renderer.Cache.Models.TryGetValue(s.Renderer.Settings.SelectedModel, out var model))
            {
                const int width = 100;
                var speed = s.Camera.Speed / 100;

                ImGui.PushID(0); ImGui.BeginDisabled(model.TransformsCount < 2);
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.SliderInt("", ref model.SelectedInstance, 0, model.TransformsCount - 1, "Instance %i", ImGuiSliderFlags.AlwaysClamp);
                ImGui.EndDisabled(); ImGui.PopID();

                ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                if (ImGui.TreeNode("Location"))
                {
                    ImGui.PushID(1);
                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("X", ref model.Transforms[model.SelectedInstance].Position.X, speed, 0f, 0f, "%.2f m");

                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("Y", ref model.Transforms[model.SelectedInstance].Position.Z, speed, 0f, 0f, "%.2f m");

                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("Z", ref model.Transforms[model.SelectedInstance].Position.Y, speed, 0f, 0f, "%.2f m");

                    ImGui.PopID();
                    ImGui.TreePop();
                }

                ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                if (ImGui.TreeNode("Rotation"))
                {
                    ImGui.PushID(2);
                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("X", ref model.Transforms[model.SelectedInstance].Rotation.Roll, .5f, 0f, 0f, "%.1f°");

                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("Y", ref model.Transforms[model.SelectedInstance].Rotation.Pitch, .5f, 0f, 0f, "%.1f°");

                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("Z", ref model.Transforms[model.SelectedInstance].Rotation.Yaw, .5f, 0f, 0f, "%.1f°");

                    ImGui.PopID();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Scale"))
                {
                    ImGui.PushID(3);
                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("X", ref model.Transforms[model.SelectedInstance].Scale.X, speed, 0f, 0f, "%.3f");

                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("Y", ref model.Transforms[model.SelectedInstance].Scale.Z, speed, 0f, 0f, "%.3f");

                    ImGui.SetNextItemWidth(width);
                    ImGui.DragFloat("Z", ref model.Transforms[model.SelectedInstance].Scale.Y, speed, 0f, 0f, "%.3f");

                    ImGui.PopID();
                    ImGui.TreePop();
                }

                model.UpdateMatrix(model.SelectedInstance);
            }
            else NoMeshSelected();
            PopStyleCompact();
            ImGui.End();
        }
    }

    private void DrawUvChannels(Snooper s)
    {
        if (ImGui.Begin("UV Channels"))
        {
            PushStyleCompact();
            if (s.Renderer.Cache.Models.TryGetValue(s.Renderer.Settings.SelectedModel, out var model) &&
                s.Renderer.Settings.TryGetSection(model, out var section))
            {
                var width = ImGui.GetContentRegionAvail().X;
                var material = model.Materials[section.MaterialIndex];

                ImGui.PushID(0); ImGui.BeginDisabled(model.NumTexCoords < 2);
                ImGui.SetNextItemWidth(width);
                ImGui.SliderInt("", ref material.SelectedChannel, 0, model.NumTexCoords - 1, "Channel %i", ImGuiSliderFlags.AlwaysClamp);
                ImGui.EndDisabled(); ImGui.PopID();

                ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                if (ImGui.TreeNode("Textures"))
                {
                    if (material.Diffuse.Length > 0)
                    {
                        var size = new Vector2(ImGui.GetContentRegionAvail().X / 5.75f);
                        DrawSquareTexture(material.Diffuse[material.SelectedChannel], size); ImGui.SameLine();
                        DrawSquareTexture(material.Normals[material.SelectedChannel], size); ImGui.SameLine();
                        DrawSquareTexture(material.SpecularMasks[material.SelectedChannel], size); ImGui.SameLine();
                        DrawSquareTexture(material.M.Texture, size); ImGui.SameLine();
                        DrawSquareTexture(material.Emissive[material.SelectedChannel], size); ImGui.SameLine();
                    }
                    else TextColored(_errorColor, "no texture in material section");
                }
            }
            else NoMeshSelected();
            PopStyleCompact();
            ImGui.End();
        }
    }

    private void Draw3DViewport(Snooper s)
    {
        const float lookSensitivity = 0.1f;
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysUseWindowPadding;

        // ImGui.SetNextWindowSize(_viewportSize, _firstUse);
        // ImGui.SetNextWindowPos(_viewportPosition, _firstUse);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("3D Viewport", flags);
        ImGui.PopStyleVar();

        var largest = ImGui.GetContentRegionAvail();
        largest.X -= ImGui.GetScrollX();
        largest.Y -= ImGui.GetScrollY();

        var size = new Vector2(largest.X, largest.Y);
        s.Camera.AspectRatio = size.X / size.Y;
        ImGui.ImageButton(s.Framebuffer.GetPointer(), size, new Vector2(0, 1), new Vector2(1, 0), 0);

        // it took me 5 hours to make it work, don't change any of the following code
        // basically the Raw cursor doesn't actually freeze the mouse position
        // so for ImGui, the IsItemHovered will be false if mouse leave, even in Raw mode
        if (ImGui.IsItemHovered())
        {
            // if left button down while mouse is hover viewport
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left) && !_viewportFocus)
            {
                _viewportFocus = true;
                s.CursorState = CursorState.Grabbed;
            }
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                var guid = s.Renderer.Picking.ReadPixel(ImGui.GetMousePos(), ImGui.GetCursorScreenPos(), size);
                s.Renderer.Settings.SelectModel(guid);
                ImGui.SetWindowFocus("Outliner");
            }
        }

        // this can't be inside IsItemHovered! read it as
        // if left mouse button was pressed while hovering the viewport
        // move camera until left mouse button is released
        // no matter where mouse position end up
        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left, lookSensitivity) && _viewportFocus)
        {
            var io = ImGui.GetIO();
            var delta = io.MouseDelta * lookSensitivity;
            s.Camera.ModifyDirection(delta.X, delta.Y);
        }

        // if left button up and mouse was in viewport
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && _viewportFocus)
        {
            _viewportFocus = false;
            s.CursorState = CursorState.Normal;
        }

        const float padding = 7.5f;
        float framerate = ImGui.GetIO().Framerate;
        var text = $"FPS: {framerate:0} ({1000.0f / framerate:0.##} ms)";
        ImGui.SetCursorPos(size with { X = padding });
        ImGui.Text(text);

        ImGui.End();
    }

    private void PopStyleCompact() => ImGui.PopStyleVar(2);
    private void PushStyleCompact()
    {
        var style = ImGui.GetStyle();
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, style.FramePadding with { Y = style.FramePadding.Y * 0.6f });
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, style.ItemSpacing with { Y = style.ItemSpacing.Y * 0.6f });
    }

    private void NoMeshSelected() => TextColored(_errorColor, "no mesh selected");
    private void TextColored(Vector4 color, string text)
    {
        ImGui.TextColored(color, text);
    }

    private void DrawSquareTexture(Texture texture, Vector2 size)
    {
        ImGui.Image(texture?.GetPointer() ?? IntPtr.Zero, size, Vector2.Zero, Vector2.One, Vector4.One, new Vector4(1, 1, 1, .5f));
        if (texture == null) return;

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
            ImGui.SetClipboardText(Creator.Utils.FixPath(texture.Path));
            texture.Label = "(?) Path Copied to Clipboard";
        }
    }

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
        style.Colors[(int) ImGuiCol.Header]                 = new Vector4(0.05f, 0.26f, 0.56f, 1.00f);
        style.Colors[(int) ImGuiCol.HeaderHovered]          = new Vector4(0.05f, 0.26f, 0.56f, 0.39f);
        style.Colors[(int) ImGuiCol.HeaderActive]           = new Vector4(0.04f, 0.23f, 0.52f, 1.00f);
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
        style.Colors[(int) ImGuiCol.TabUnfocusedActive]     = new Vector4(0.15f, 0.15f, 0.15f, 1.00f);
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
}
