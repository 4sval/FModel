using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public sealed class FolderTreeIconConverter : IValueConverter
{
    public static readonly FolderTreeIconConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var name = value as string;

        // This should get reworked to be aligned with asset explorer somehow

        return name?.ToLowerInvariant() switch
        {
            "config" => "/FModel;component/Resources/gear.png",
            "plugins" => "/FModel;component/Resources/puzzle.png",

            "windows" => "/FModel;component/Resources/windows.png",
            "android" => "/FModel;component/Resources/android.png",
            "ios" or "mac" => "/FModel;component/Resources/apple.png",
            "linux" or "linuxaarch64" => "/FModel;component/Resources/linux.png",
            "unix" => "/FModel;component/Resources/unix.png",
            "platforms" => "/FModel;component/Resources/pc.png",

            "weapons" => "/FModel;component/Resources/weapon.png",
            "localization" or "internationalization" => "/FModel;component/Resources/localization.png",
            "ui" => "/FModel;component/Resources/ui.png",
            "sounds" or "audio" => "/FModel;component/Resources/sound.png",
            "2dassets" or "textures" => "/FModel;component/Resources/texture.png",
            "engine" or "enginecontent" => "/FModel;component/Resources/engine.png",
            "materials" => "/FModel;component/Resources/materialicon.png",
            "blueprints" => "/FModel;component/Resources/blueprint.png",
            "cinematics" or "movies" => "/FModel;component/Resources/cinematics.png",

            "fortnite" => "/FModel;component/Resources/fortnite.png",
            "fortnitegame" => "/FModel;component/Resources/fortnitebr.png",
            "worldexplorers" => "/FModel;component/Resources/battlebreakers.png",
            "oakgame" => "/FModel;component/Resources/borderlands.png",
            "shootergame" => "/FModel;component/Resources/valorant.png",
            "roguecompany" => "/FModel;component/Resources/roguecompany.png",
            "g3" => "/FModel;component/Resources/spellbreak.png",
            "swgame" => "/FModel;component/Resources/fallenorder.png",
            "prospect" => "/FModel;component/Resources/thecycle.png",
            "creative" => "/FModel;component/Resources/creative.png",
            "athena" => "/FModel;component/Resources/athena.png",
            "stateofdecay2" => "/FModel;component/Resources/stateofdecay2.png",
            _ => null
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
