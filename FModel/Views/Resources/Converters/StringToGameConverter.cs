using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public class StringToGameConverter : IValueConverter
{
    public static readonly StringToGameConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            "Newt" => "Spellbreak",
            "Nebula" => "Days Gone",
            "Fortnite" => "Fortnite",
            "VALORANT" => "Valorant",
            "Pewee" => "Rogue Company",
            "Catnip" => "Borderlands 3",
            "AzaleaAlpha" => "The Cycle",
            "Snoek" => "State of Decay 2",
            "Rosemallow" => "The Outer Worlds",
            "WorldExplorersLive" => "Battle Breakers",
            "MinecraftDungeons" => "Minecraft Dungeons",
            "shoebill" => "Star Wars: Jedi Fallen Order",
            "a99769d95d8f400baad1f67ab5dfe508" => "Core",
            381210 => "Dead By Daylight",
            578080 => "PLAYERUNKNOWN'S BATTLEGROUNDS",
            677620 => "Splitgate",
            1172620 => "Sea of Thieves",
            _ => value,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}