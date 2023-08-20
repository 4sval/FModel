using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Misc;
using FModel.Settings;
using FModel.Views.Snooper.Animations;
using FModel.Views.Snooper.Lights;
using FModel.Views.Snooper.Models;
using FModel.Views.Snooper.Shading;

namespace FModel.Views.Snooper;

public class Options
{
    public FGuid SelectedModel { get; private set; }
    public int SelectedSection { get; private set; }
    public int SelectedMorph { get; private set; }
    public int SelectedAnimation{ get; private set; }

    public readonly Dictionary<FGuid, UModel> Models;
    public readonly Dictionary<FGuid, Texture> Textures;
    public readonly List<Light> Lights;

    public readonly TimeTracker Tracker;
    public readonly List<Animation> Animations;

    public readonly Dictionary<string, Texture> Icons;

    private readonly string _game;

    public Options()
    {
        Models = new Dictionary<FGuid, UModel>();
        Textures = new Dictionary<FGuid, Texture>();
        Lights = new List<Light>();

        Tracker = new TimeTracker();
        Animations = new List<Animation>();

        Icons = new Dictionary<string, Texture>
        {
            ["material"] = new ("materialicon"),
            ["noimage"] = new ("T_Placeholder_Item_Image"),
            ["pointlight"] = new ("pointlight"),
            ["spotlight"] = new ("spotlight"),
            ["link_on"] = new ("link_on"),
            ["link_off"] = new ("link_off"),
            ["link_has"] = new ("link_has"),
            ["tl_play"] = new ("tl_play"),
            ["tl_pause"] = new ("tl_pause"),
            ["tl_rewind"] = new ("tl_rewind"),
            ["tl_forward"] = new ("tl_forward"),
            ["tl_previous"] = new ("tl_previous"),
            ["tl_next"] = new ("tl_next"),
        };

        _game = Services.ApplicationService.ApplicationView.CUE4Parse.Provider.InternalGameName.ToUpper();

        SelectModel(Guid.Empty);
    }

    public void SetupModelsAndLights()
    {
        foreach (var model in Models.Values)
        {
            if (model.IsSetup) continue;
            model.Setup(this);
        }

        foreach (var light in Lights)
        {
            if (light.IsSetup) continue;
            light.Setup();
        }
    }

    public void SelectModel(FGuid guid)
    {
        // unselect old
        if (TryGetModel(out var model))
            model.IsSelected = false;

        // select new
        if (!TryGetModel(guid, out model))
            SelectedModel = Guid.Empty;
        else
        {
            model.IsSelected = true;
            SelectedModel = guid;
        }

        SelectedSection = 0;
        SelectedMorph = 0;
    }

    public void SelectAnimation(int animation)
    {
        SelectedAnimation = animation;
    }

    public void RemoveModel(FGuid guid)
    {
        if (!TryGetModel(guid, out var m) || m is not UModel model)
            return;

        DetachAndRemoveModels(model, true);
        model.Dispose();
        Models.Remove(guid);
    }

    private void DetachAndRemoveModels(UModel model, bool detach)
    {
        foreach (var socket in model.Sockets.ToList())
        {
            foreach (var info in socket.AttachedModels)
            {
                if (!TryGetModel(info.Guid, out var m) || m is not UModel attachedModel)
                    continue;

                var t = attachedModel.GetTransform();
                if (attachedModel.IsProp)
                {
                    attachedModel.Attachments.SafeDetach(model, t);
                    RemoveModel(info.Guid);
                }
                else if (detach) attachedModel.Attachments.SafeDetach(model, t);
            }

            if (socket.IsVirtual)
            {
                socket.Dispose();
                model.Sockets.Remove(socket);
            }
        }
    }

    public void AddAnimation(Animation animation)
    {
        Animations.Add(animation);
    }

    public void RemoveAnimations()
    {
        Tracker.Reset();
        SelectedAnimation = 0;

        foreach (var animation in Animations)
        {
            foreach (var guid in animation.AttachedModels)
            {
                if (!TryGetModel(guid, out var model) || model is not SkeletalModel animatedModel)
                    continue;

                animatedModel.Skeleton.ResetAnimatedData(true);
                DetachAndRemoveModels(animatedModel, false);
            }

            animation.Dispose();
        }

        foreach (var kvp in Models)
            if (kvp.Value.IsProp)
                RemoveModel(kvp.Key);

        Animations.Clear();
    }

    public void SelectSection(int index)
    {
        SelectedSection = index;
    }

    public void SelectMorph(int index, SkeletalModel model)
    {
        SelectedMorph = index;
        model.UpdateMorph(SelectedMorph);
    }

    public bool TryGetTexture(UTexture2D o, bool fix, out Texture texture)
    {
        var guid = o.LightingGuid;
        if (!Textures.TryGetValue(guid, out texture) &&
            o.Decode(UserSettings.Default.PreviewMaxTextureSize, UserSettings.Default.CurrentDir.TexturePlatform) is { } bitmap)
        {
            texture = new Texture(bitmap, o);
            if (fix) TextureHelper.FixChannels(_game, texture);
            Textures[guid] = texture;
            bitmap.Dispose();
        }
        return texture != null;
    }

    public bool TryGetModel(out UModel model) => Models.TryGetValue(SelectedModel, out model);
    public bool TryGetModel(FGuid guid, out UModel model) => Models.TryGetValue(guid, out model);

    public bool TryGetSection(out Section section) => TryGetSection(SelectedModel, out section);
    public bool TryGetSection(FGuid guid, out Section section)
    {
        if (TryGetModel(guid, out var model))
        {
            return TryGetSection(model, out section);
        }

        section = null;
        return false;
    }
    public bool TryGetSection(UModel model, out Section section)
    {
        if (SelectedSection >= 0 && SelectedSection < model.Sections.Length)
            section = model.Sections[SelectedSection]; else section = null;
        return section != null;
    }

    public void SwapMaterial(bool value)
    {
        Services.ApplicationService.ApplicationView.CUE4Parse.ModelIsOverwritingMaterial = value;
    }

    public void AnimateMesh(bool value)
    {
        Services.ApplicationService.ApplicationView.CUE4Parse.ModelIsWaitingAnimation = value;
    }

    public void ResetModelsLightsAnimations()
    {
        foreach (var model in Models.Values)
        {
            model.Dispose();
        }
        Models.Clear();
        Lights.Clear();
        Tracker.Reset();
        foreach (var animation in Animations)
        {
            animation.Dispose();
        }
        Animations.Clear();
    }

    public void Dispose()
    {
        ResetModelsLightsAnimations();
        foreach (var texture in Textures.Values)
        {
            texture.Dispose();
        }
        Textures.Clear();
        foreach (var texture in Icons.Values)
        {
            texture.Dispose();
        }
        Icons.Clear();
    }
}
