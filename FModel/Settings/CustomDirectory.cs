using System.Collections.Generic;
using FModel.Framework;

namespace FModel.Settings;

public class CustomDirectory : ViewModel
{
    public static IList<CustomDirectory> Default(string gameName)
    {
        switch (gameName)
        {
            case "Fortnite":
            case "Fortnite [LIVE]":
                return new List<CustomDirectory>
                {
                    new("Cosmetics", "FortniteGame/Content/Athena/Items/Cosmetics/"),
                    new("Emotes [AUDIO]", "FortniteGame/Content/Athena/Sounds/Emotes/"),
                    new("Music Packs [AUDIO]", "FortniteGame/Content/Athena/Sounds/MusicPacks/"),
                    new("Weapons", "FortniteGame/Content/Athena/Items/Weapons/"),
                    new("Strings", "FortniteGame/Content/Localization/")
                };
            case "VALORANT":
            case "VALORANT [LIVE]":
                return new List<CustomDirectory>
                {
                    new("Audio", "ShooterGame/Content/WwiseAudio/Media/"),
                    new("Characters", "ShooterGame/Content/Characters/"),
                    new("Gun Buddies", "ShooterGame/Content/Equippables/Buddies/"),
                    new("Cards and Sprays", "ShooterGame/Content/Personalization/"),
                    new("Shop Backgrounds", "ShooterGame/Content/UI/OutOfGame/MainMenu/Store/Shared/Textures/"),
                    new("Weapon Renders", "ShooterGame/Content/UI/Screens/OutOfGame/MainMenu/Collection/Assets/Large/")
                };
            default:
                return new List<CustomDirectory>();
        }
    }

    private string _header;
    public string Header
    {
        get => _header;
        set => SetProperty(ref _header, value);
    }

    private string _directoryPath;
    public string DirectoryPath
    {
        get => _directoryPath;
        set => SetProperty(ref _directoryPath, value);
    }

    public CustomDirectory()
    {
        Header = string.Empty;
        DirectoryPath = string.Empty;
    }

    public CustomDirectory(string header, string path)
    {
        Header = header;
        DirectoryPath = path;
    }

    public override string ToString() => Header;
}
