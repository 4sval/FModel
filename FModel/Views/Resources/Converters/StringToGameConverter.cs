using System;
using System.Globalization;
using System.Windows.Data;
using FModel.Extensions;

namespace FModel.Views.Resources.Converters;

public class StringToGameConverter : IValueConverter
{
    public static readonly StringToGameConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var ret = value switch
        {
            "Newt" => FGame.g3,
            "Nebula" => FGame.BendGame,
            "Fortnite" => FGame.FortniteGame,
            "VALORANT" => FGame.ShooterGame,
            "Pewee" => FGame.RogueCompany,
            "Catnip" => FGame.OakGame,
            "AzaleaAlpha" => FGame.Prospect,
            "Snoek" => FGame.StateOfDecay2,
            "Rosemallow" => FGame.Indiana,
            "WorldExplorersLive" => FGame.WorldExplorers,
            "MinecraftDungeons" => FGame.Dungeons,
            "shoebill" => FGame.SwGame,
            "a99769d95d8f400baad1f67ab5dfe508" => FGame.Platform,
            "711c5e95dc094ca58e5f16bd48e751d6" => FGame.PandaGame,
            381210 => FGame.DeadByDaylight,
            578080 => FGame.TslGame,
            677620 => FGame.PortalWars,
            1172620 => FGame.Athena,
            _ => FGame.Unknown
        };
        return ret == FGame.Unknown ? value : ret.GetDescription();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
