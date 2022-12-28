using System;
using System.Collections.Generic;
using System.Diagnostics;
using CUE4Parse.UE4.Objects.Core.Misc;
using FModel.Framework;
using ImGuiNET;
using OpenTK.Windowing.Common;
using System.Numerics;
using System.Text;
using FModel.Settings;
using FModel.Views.Snooper.Models;
using FModel.Views.Snooper.Shading;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper;

public class Swap
{
    public bool Value;
    public bool IsAware;

    public void Reset()
    {
        Value = false;
    }
}

public class Save
{
    public bool Value;
    public string Label;
    public string Path;

    public void Reset()
    {
        Value = false;
        Label = string.Empty;
        Path = string.Empty;
    }
}

public class SnimGui
{
    public readonly ImGuiController Controller;
    private readonly Swap _swapper = new ();
    private readonly Save _saver = new ();
    private readonly string _renderer;
    private readonly string _version;
    private bool _ti_open;
    private bool _ti_overlayUv;
    private bool _viewportFocus;

    private readonly Vector4 _accentColor = new (0.125f, 0.42f, 0.831f, 1.0f);
    private readonly Vector4 _alertColor = new (0.831f, 0.573f, 0.125f, 1.0f);
    private readonly Vector4 _errorColor = new (0.761f, 0.169f, 0.169f, 1.0f);

    private const uint _dockspaceId = 1337;

    public SnimGui(int width, int height)
    {
        _renderer = GL.GetString(StringName.Renderer);
        _version = "OpenGL " + GL.GetString(StringName.Version);
        Controller = new ImGuiController(width, height);
        Theme();
    }

    public void Render(Snooper s)
    {
        Controller.SemiBold();
        DrawDockSpace(s.Size);

        SectionWindow("Material Inspector", s.Renderer, DrawMaterialInspector, false);

        // Window("Timeline", () => {});
        Window("World", () => DrawWorld(s), false);
        Window("Sockets", () => DrawSockets(s));

        DrawOuliner(s);
        DrawDetails(s);
        Draw3DViewport(s);
        DrawNavbar();

        if (_ti_open) DrawTextureInspector(s);
        Controller.Render();
    }

    private void DrawWorld(Snooper s)
    {
        if (ImGui.BeginTable("world_details", 2, ImGuiTableFlags.SizingStretchProp))
        {
            var length = s.Renderer.Options.Models.Count;
            Layout("Renderer");ImGui.Text($" :  {_renderer}");
            Layout("Version");ImGui.Text($" :  {_version}");
            Layout("Loaded Models");ImGui.Text($" :  x{length}");ImGui.SameLine();

            var b = false;
            if (ImGui.SmallButton("Save All"))
            {
                foreach (var model in s.Renderer.Options.Models.Values)
                {
                    b |= model.TrySave(out _, out _);
                }
            }

            Modal("Saved", b, () =>
            {
                ImGui.TextWrapped($"Successfully saved {length} models");
                ImGui.Separator();

                var size = new Vector2(120, 0);
                if (ImGui.Button("OK", size))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SetItemDefaultFocus();
                ImGui.SameLine();

                if (ImGui.Button("Show In Explorer", size))
                {
                    Process.Start("explorer.exe", $"/select, \"{UserSettings.Default.ModelDirectory.Replace('/', '\\')}\"");
                    ImGui.CloseCurrentPopup();
                }
            });

            ImGui.EndTable();
        }

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.CollapsingHeader("Editor"))
        {
            if (ImGui.BeginTable("world_editor", 2))
            {
                Layout("Skybox");ImGui.PushID(1);
                ImGui.Checkbox("", ref s.Renderer.ShowSkybox);
                ImGui.PopID();Layout("Grid");ImGui.PushID(2);
                ImGui.Checkbox("", ref s.Renderer.ShowGrid);
                ImGui.PopID();Layout("Lights");ImGui.PushID(3);
                ImGui.Checkbox("", ref s.Renderer.ShowLights);
                ImGui.PopID();Layout("Vertex Colors");ImGui.PushID(4);
                ImGui.Combo("vertex_colors", ref s.Renderer.VertexColor,
                    "Default\0Diffuse Only\0Colors\0Normals\0Tangent\0Texture Coordinates\0");
                ImGui.PopID();

                ImGui.EndTable();
            }
        }

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.CollapsingHeader("Camera"))
        {
            s.Camera.ImGuiCamera();
        }

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.CollapsingHeader("Lights"))
        {
            for (int i = 0; i < s.Renderer.Options.Lights.Count; i++)
            {
                var id = $"[{i}] {s.Renderer.Options.Models[s.Renderer.Options.Lights[i].Model].Name}";
                if (ImGui.TreeNode(id) && ImGui.BeginTable(id, 2))
                {
                    s.Renderer.Options.SelectModel(s.Renderer.Options.Lights[i].Model);
                    s.Renderer.Options.Lights[i].ImGuiLight();
                    ImGui.EndTable();
                    ImGui.TreePop();
                }
            }
        }
    }

    private void DrawDockSpace(OpenTK.Mathematics.Vector2i size)
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

        Modal("Commands", ImGui.MenuItem("Commands"), () =>
        {
            ImGui.TextWrapped(
                @"Most commands should be pretty straightforward but just in case here is a non-exhaustive list of things you can do in this 3D viewer:

1. UI / UX
    - Press Shift while moving a window to dock it
    - Ctrl Click in a box to input a new value
    - Mouse Click + Drag in a box to modify the value without having to type
    - Press H to hide the window and append the next mesh you extract

2. Viewport
    - WASD to move around
    - Shift to move faster
    - XC to zoom
    - Left Mouse Button pressed to look around
    - Right Click to select a model in the world

3. Outliner
    3.1. Right Click Model
        - Show / Hide the model
        - Show a skeletal representation of the model
        - Save to save the model as .psk / .pskx
        - Animate to load an animation on the model
        - Teleport to quickly move the camera to the position of the model
        - Delete
        - Deselect
        - Copy Name to Clipboard

4. World
    - Save All to save all loaded models at once
      (no it's not dying it's just freezing while saving them all)

5. Details
    5.1. Right Click Section
        - Show / Hide the section
        - Swap to change the material used by this section
        - Copy Name to Clipboard
    5.2. Transform
        - Move / Rotate / Scale the model in the world
    5.3. Morph Targets
        - Modify the vertices position by a given amount to change the shape of the model
");
            ImGui.Separator();

            var size = new Vector2(120, 0);
            ImGui.InvisibleButton("", size * 3);
            ImGui.SameLine();
            ImGui.SetItemDefaultFocus();
            if (ImGui.Button("OK", size))
            {
                ImGui.CloseCurrentPopup();
            }
        });

        Modal("GPU OpenGL Info", ImGui.MenuItem("GPU Info"), () =>
        {
            var s = new StringBuilder();
            s.AppendLine($"MaxTextureImageUnits: {GL.GetInteger(GetPName.MaxTextureImageUnits)}");
            s.AppendLine($"MaxTextureUnits: {GL.GetInteger(GetPName.MaxTextureUnits)}");
            s.AppendLine($"MaxVertexTextureImageUnits: {GL.GetInteger(GetPName.MaxVertexTextureImageUnits)}");
            s.AppendLine($"MaxCombinedTextureImageUnits: {GL.GetInteger(GetPName.MaxCombinedTextureImageUnits)}");
            s.AppendLine($"MaxGeometryTextureImageUnits: {GL.GetInteger(GetPName.MaxGeometryTextureImageUnits)}");
            s.AppendLine($"MaxTextureCoords: {GL.GetInteger(GetPName.MaxTextureCoords)}");
            s.AppendLine($"Renderer: {_renderer}");
            s.AppendLine($"Version: {_version}");
            ImGui.TextWrapped(s.ToString());
            ImGui.Separator();

            var size = new Vector2(120, 0);
            ImGui.InvisibleButton("", size * 4);
            ImGui.SameLine();
            ImGui.SetItemDefaultFocus();
            if (ImGui.Button("OK", size))
            {
                ImGui.CloseCurrentPopup();
            }
        });

        Modal("About Snooper", ImGui.MenuItem("About"), () =>
        {
            ImGui.TextWrapped(
                @"Snooper, an ""OpenGL x ImGui"" based 3D viewer, is the result of months of work in order to improve our last one and open up the capabilities data-mining offers. For too long, softwares including FModel were only focused on a bare minimum level of detail showed to the end-user. This is the first step of a long and painful transition to make FModel a viable open-source tool to deep dive into Unreal Engine, its structure, and show how things work internally.

Snooper aims to give an accurate preview of models, materials, skeletal animations, particles, levels, and level animations (oof) while keeping it compatible with most UE games. This is not an easy task AT ALL, in fact, I don't really know if everything will make out, but what I can say is that we have ideas and a vision for the future of FModel.
");
            ImGui.Separator();

            var size = new Vector2(120, 0);
            ImGui.InvisibleButton("", size * 4);
            ImGui.SameLine();
            ImGui.SetItemDefaultFocus();
            if (ImGui.Button("OK", size))
            {
                ImGui.CloseCurrentPopup();
            }
        });

        const string text = "Press H to Hide or ESC to Exit...";
        ImGui.SetCursorPosX(ImGui.GetWindowViewport().WorkSize.X - ImGui.CalcTextSize(text).X - 5);
        ImGui.TextColored(new Vector4(0.36f, 0.42f, 0.47f, 1.00f), text);

        ImGui.EndMainMenuBar();
    }

    private void DrawOuliner(Snooper s)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        Window("Outliner", () =>
        {
            if (ImGui.BeginTable("Items", 3, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersOuterV, ImGui.GetContentRegionAvail()))
            {
                ImGui.TableSetupColumn("Instance", ImGuiTableColumnFlags.NoHeaderWidth | ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Channels", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Name");
                ImGui.TableHeadersRow();

                var i = 0;
                foreach ((FGuid guid, Model model) in s.Renderer.Options.Models)
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
                    ImGui.Text(model.UvCount.ToString("D"));
                    ImGui.TableNextColumn();
                    if (ImGui.Selectable(model.Name, s.Renderer.Options.SelectedModel == guid, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        s.Renderer.Options.SelectModel(guid);
                    }

                    Popup(() =>
                    {
                        s.Renderer.Options.SelectModel(guid);
                        if (ImGui.MenuItem("Show", null, model.Show)) model.Show = !model.Show;
                        if (ImGui.MenuItem("Wireframe", null, model.Wireframe)) model.Wireframe = !model.Wireframe;
                        ImGui.Separator();
                        if (ImGui.Selectable("Save"))
                        {
                            s.WindowShouldFreeze(true);
                            _saver.Value = model.TrySave(out _saver.Label, out _saver.Path);
                            s.WindowShouldFreeze(false);
                        }

                        ImGui.BeginDisabled(true);
                        // ImGui.BeginDisabled(!model.HasSkeleton);
                        if (ImGui.Selectable("Animate"))
                        {
                            s.Renderer.Options.AnimateMesh(true);
                            s.WindowShouldClose(true, false);
                        }

                        ImGui.EndDisabled();
                        if (ImGui.Selectable("Teleport To"))
                        {
                            var instancePos = model.Transforms[model.SelectedInstance].Position;
                            s.Camera.Position = new Vector3(instancePos.X, instancePos.Y, instancePos.Z);
                        }

                        if (ImGui.Selectable("Delete")) s.Renderer.Options.Models.Remove(guid);
                        if (ImGui.Selectable("Deselect")) s.Renderer.Options.SelectModel(Guid.Empty);
                        ImGui.Separator();
                        if (ImGui.Selectable("Copy Name to Clipboard")) ImGui.SetClipboardText(model.Name);
                    });
                    ImGui.PopID();
                    i++;
                }

                Modal("Saved", _saver.Value, () =>
                {
                    ImGui.TextWrapped($"Successfully saved {_saver.Label}");
                    ImGui.Separator();

                    var size = new Vector2(120, 0);
                    if (ImGui.Button("OK", size))
                    {
                        _saver.Reset();
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.SetItemDefaultFocus();
                    ImGui.SameLine();

                    if (ImGui.Button("Show In Explorer", size))
                    {
                        Process.Start("explorer.exe", $"/select, \"{_saver.Path.Replace('/', '\\')}\"");

                        _saver.Reset();
                        ImGui.CloseCurrentPopup();
                    }
                });

                ImGui.EndTable();
            }
        });
        ImGui.PopStyleVar();
    }

    private void DrawSockets(Snooper s)
    {
        foreach (var model in s.Renderer.Options.Models.Values)
        {
            if (!model.HasSkeleton || model.IsSelected) return;
            if (ImGui.TreeNode($"{model.Name} [{model.Skeleton.Sockets.Length}]"))
            {
                var i = 0;
                foreach (var socket in model.Skeleton.Sockets)
                {
                    ImGui.PushID(i);
                    ImGui.Text($"{socket.Name} attached to {socket.Bone}");
                    ImGui.Text($"P: {socket.Transform.Matrix.M41} | {socket.Transform.Matrix.M42} | {socket.Transform.Matrix.M43}");
                    // ImGui.Text($"R: {socket.Transform.Rotation}");
                    // ImGui.Text($"S: {socket.Transform.Scale}");
                    if (ImGui.Button("Attach") && s.Renderer.Options.TryGetModel(out var selected))
                    {
                        selected.Transforms[selected.SelectedInstance] = socket.Transform;
                        selected.UpdateMatrix(selected.SelectedInstance);
                    }
                    ImGui.PopID();
                    i++;
                }
                ImGui.TreePop();
            }
        }
    }

    private void DrawDetails(Snooper s)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        MeshWindow("Details", s.Renderer, (icons, model) =>
        {
            if (ImGui.BeginTable("model_details", 2, ImGuiTableFlags.SizingStretchProp))
            {
                Layout("Entity");ImGui.Text($" :  ({model.Type}) {model.Name}");
                Layout("Guid");ImGui.Text($" :  {s.Renderer.Options.SelectedModel.ToString(EGuidFormats.UniqueObjectGuid)}");
                if (model.HasSkeleton)
                {
                    Layout("Skeleton");ImGui.Text($" :  {model.Skeleton.RefSkel.Name}");
                    Layout("Bones");ImGui.Text($" :  x{model.Skeleton.RefSkel.BoneTree.Length}");
                    Layout("Sockets");ImGui.Text($" :  x{model.Skeleton.Sockets.Length}");
                }

                ImGui.EndTable();
            }
            if (ImGui.BeginTabBar("tabbar_details", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem("Sections") && ImGui.BeginTable("table_sections", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersOuterV, ImGui.GetContentRegionAvail()))
                {
                    ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Material");
                    ImGui.TableHeadersRow();

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
                        if (ImGui.Selectable(material.Name, s.Renderer.Options.SelectedSection == i, ImGuiSelectableFlags.SpanAllColumns))
                        {
                            s.Renderer.Options.SelectSection(i);
                        }
                        Popup(() =>
                        {
                            s.Renderer.Options.SelectSection(i);
                            if (ImGui.MenuItem("Show", null, section.Show)) section.Show = !section.Show;
                            if (ImGui.Selectable("Swap"))
                            {
                                if (_swapper.IsAware)
                                {
                                    s.Renderer.Options.SwapMaterial(true);
                                    s.WindowShouldClose(true, false);
                                }
                                else _swapper.Value = true;
                            }
                            ImGui.Separator();
                            if (ImGui.Selectable("Copy Name to Clipboard")) ImGui.SetClipboardText(material.Name);
                        });
                        ImGui.PopID();
                    }
                    ImGui.EndTable();

                    Modal("Swap?", _swapper.Value, () =>
                    {
                        ImGui.TextWrapped("You're about to swap a material.\nThe window will close for you to extract a material!\n\n");
                        ImGui.Separator();

                        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                        ImGui.Checkbox("Got it! Don't show me again", ref _swapper.IsAware);
                        ImGui.PopStyleVar();

                        var size = new Vector2(120, 0);
                        if (ImGui.Button("OK", size))
                        {
                            _swapper.Reset();
                            s.Renderer.Options.SwapMaterial(true);
                            ImGui.CloseCurrentPopup();
                            s.WindowShouldClose(true, false);
                        }

                        ImGui.SetItemDefaultFocus();
                        ImGui.SameLine();

                        if (ImGui.Button("Cancel", size))
                        {
                            _swapper.Reset();
                            ImGui.CloseCurrentPopup();
                        }
                    });

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Transform"))
                {
                    ImGui.PushID(0); ImGui.BeginDisabled(model.TransformsCount < 2);
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    ImGui.SliderInt("", ref model.SelectedInstance, 0, model.TransformsCount - 1, "Instance %i", ImGuiSliderFlags.AlwaysClamp);
                    ImGui.EndDisabled(); ImGui.PopID();

                    model.Transforms[model.SelectedInstance].ImGuiTransform(s.Camera.Speed / 100f);
                    model.UpdateMatrix(model.SelectedInstance);
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
                                if (ImGui.Selectable(model.Morphs[i].Name, s.Renderer.Options.SelectedMorph == i))
                                {
                                    s.Renderer.Options.SelectMorph(i, model);
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
                    else CenteredTextColored(_errorColor, "Selected Mesh Has No Morph Targets");
                    ImGui.EndTabItem();
                }
            }
        });
        ImGui.PopStyleVar();
    }

    private void DrawMaterialInspector(Dictionary<string, Texture> icons, Model model, Section section)
    {
        var material = model.Materials[section.MaterialIndex];

        ImGui.Spacing();
        ImGui.Image(icons["material"].GetPointer(), new Vector2(24));
        ImGui.SameLine(); ImGui.AlignTextToFramePadding(); ImGui.Text(material.Name);
        ImGui.Spacing();

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.CollapsingHeader("Parameters"))
        {
            material.ImGuiParameters();
        }

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.CollapsingHeader("Textures") && material.ImGuiTextures(icons, model))
        {
            _ti_open = true;
            ImGui.SetWindowFocus("Texture Inspector");
        }

        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        if (ImGui.CollapsingHeader("Properties"))
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
            if (ImGui.TreeNode("Scalars"))
            {
                material.ImGuiDictionaries("scalars", material.Parameters.Scalars, true);
                ImGui.TreePop();
            }
            ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
            if (ImGui.TreeNode("Switchs"))
            {
                material.ImGuiDictionaries("switchs", material.Parameters.Switchs, true);
                ImGui.TreePop();
            }
            ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
            if (ImGui.TreeNode("Colors"))
            {
                material.ImGuiColors(material.Parameters.Colors);
                ImGui.TreePop();
            }
            if (ImGui.TreeNode("Referenced Textures"))
            {
                material.ImGuiDictionaries("textures", material.Parameters.Textures);
                ImGui.TreePop();
            }
        }
    }

    private void DrawTextureInspector(Snooper s)
    {
        if (ImGui.Begin("Texture Inspector", ref _ti_open, ImGuiWindowFlags.NoScrollbar) &&
            s.Renderer.Options.TryGetModel(out var model) &&
            s.Renderer.Options.TryGetSection(model, out var section))
        {
            var vectors = model.Materials[section.MaterialIndex].ImGuiTextureInspector(s.Renderer.Options.Icons["noimage"]);
            if (_ti_overlayUv)
            {
                var size = vectors[0];
                var drawList = ImGui.GetWindowDrawList();
                drawList.PushClipRect(size, size, true);
                ImGui.SetCursorPos(vectors[1]);
                ImGui.InvisibleButton("canvas", size, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight);
                drawList.AddLine(new Vector2(0, 0), size, 255, 2f);
                drawList.PopClipRect();
            }
            Popup(() =>
            {
                if (ImGui.MenuItem("Overlay UVs", null, _ti_overlayUv, false))
                    _ti_overlayUv = !_ti_overlayUv;
            });
        }
        ImGui.End(); // if window is collapsed
    }

    private void Draw3DViewport(Snooper s)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        Window("3D Viewport", () =>
        {
            var largest = ImGui.GetContentRegionAvail();
            largest.X -= ImGui.GetScrollX();
            largest.Y -= ImGui.GetScrollY();

            var size = new Vector2(largest.X, largest.Y);
            s.Camera.AspectRatio = size.X / size.Y;
            ImGui.ImageButton(s.Framebuffer.GetPointer(), size, new Vector2(0, 1), new Vector2(1, 0), 0);

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
                    s.Renderer.Options.SelectModel(guid);
                    ImGui.SetWindowFocus("Outliner");
                }
            }

            const float lookSensitivity = 0.1f;
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

            float framerate = ImGui.GetIO().Framerate;
            ImGui.SetCursorPos(size with { X = 7.5f });
            ImGui.Text($"FPS: {framerate:0} ({1000.0f / framerate:0.##} ms)");
        }, false);
        ImGui.PopStyleVar();
    }

    private void Popup(Action content)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4f));
        if (ImGui.BeginPopupContextItem())
        {
            content();
            ImGui.EndPopup();
        }
        ImGui.PopStyleVar();
    }

    private void Modal(string title, bool condition, Action content)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4f));
        var pOpen = true;
        if (condition) ImGui.OpenPopup(title);
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(.5f));
        if (ImGui.BeginPopupModal(title, ref pOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            content();
            ImGui.EndPopup();
        }
        ImGui.PopStyleVar();
    }

    private void Window(string name, Action content, bool styled = true)
    {
        if (ImGui.Begin(name, ImGuiWindowFlags.NoScrollbar))
        {
            Controller.PopFont();
            if (styled) PushStyleCompact();
            content();
            if (styled) PopStyleCompact();
            ImGui.End();
        }
    }

    private void MeshWindow(string name, Renderer renderer, Action<Dictionary<string, Texture>, Model> content, bool styled = true)
    {
        Window(name, () =>
        {
            if (renderer.Options.TryGetModel(out var model)) content(renderer.Options.Icons, model);
            else NoMeshSelected();
        }, styled);
    }

    private void SectionWindow(string name, Renderer renderer, Action<Dictionary<string, Texture>, Model, Section> content, bool styled = true)
    {
        MeshWindow(name, renderer, (icons, model) =>
        {
            if (renderer.Options.TryGetSection(model, out var section)) content(icons, model, section);
            else NoSectionSelected();
        }, styled);
    }

    private void PopStyleCompact() => ImGui.PopStyleVar(2);
    private void PushStyleCompact()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 3));
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0, 1));
    }

    private void NoMeshSelected() => CenteredTextColored(_errorColor, "No Mesh Selected");
    private void NoSectionSelected() => CenteredTextColored(_errorColor, "No Section Selected");
    private void CenteredTextColored(Vector4 color, string text)
    {
        var region = ImGui.GetContentRegionAvail();
        var size = ImGui.CalcTextSize(text);
        ImGui.SetCursorPos(new Vector2(
                ImGui.GetCursorPosX() + (region.X - size.X) / 2,
                ImGui.GetCursorPosY() + (region.Y - size.Y) / 2));
        Controller.Bold();
        ImGui.TextColored(color, text);
        Controller.PopFont();
    }

    public static void Layout(string name, bool tooltip = false)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.Spacing();ImGui.SameLine();ImGui.Text(name);
        if (tooltip) TooltipCopy(name);
        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
    }

    public static void TooltipCopy(string label, string text = null)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(label);
            ImGui.EndTooltip();
        }
        if (ImGui.IsItemClicked()) ImGui.SetClipboardText(text ?? label);
    }

    private void Theme()
    {
        var style = ImGui.GetStyle();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        io.ConfigWindowsMoveFromTitleBarOnly = true;
        io.ConfigDockingWithShift = true;

        style.WindowPadding = new Vector2(4f);
        style.FramePadding = new Vector2(3f);
        style.CellPadding = new Vector2(3f, 2f);
        style.ItemSpacing = new Vector2(6f, 3f);
        style.ItemInnerSpacing = new Vector2(3f);
        style.TouchExtraPadding = new Vector2(0f);
        style.IndentSpacing = 20f;
        style.ScrollbarSize = 10f;
        style.GrabMinSize = 8f;
        style.WindowBorderSize = 0f;
        style.ChildBorderSize = 0f;
        style.PopupBorderSize = 0f;
        style.FrameBorderSize = 0f;
        style.TabBorderSize = 0f;
        style.WindowRounding = 0f;
        style.ChildRounding = 0f;
        style.FrameRounding = 0f;
        style.PopupRounding = 0f;
        style.ScrollbarRounding = 0f;
        style.GrabRounding = 0f;
        style.LogSliderDeadzone = 0f;
        style.TabRounding = 0f;
        style.WindowTitleAlign = new Vector2(0.5f);
        style.WindowMenuButtonPosition = ImGuiDir.Right;
        style.ColorButtonPosition = ImGuiDir.Right;
        style.ButtonTextAlign = new Vector2(0.5f);
        style.SelectableTextAlign = new Vector2(0f);
        style.DisplaySafeAreaPadding = new Vector2(3f);

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
        style.Colors[(int) ImGuiCol.TabUnfocusedActive]     = new Vector4(0.23f, 0.24f, 0.29f, 1.00f);
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
