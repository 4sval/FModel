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
        return value switch
        {
            "Pewee" => "Rogue Company",
            "Rosemallow" => "The Outer Worlds",
            "Catnip" => "Borderlands 3",
            "AzaleaAlpha" => "The Cycle",
            "shoebill" => "Star Wars: Jedi Fallen Order",
            "Snoek" => "State Of Decay 2",
            "711c5e95dc094ca58e5f16bd48e751d6" => "MultiVersus",
            "9361c8c6d2f34b42b5f2f61093eedf48" => "PLAYERUNKNOWN'S BATTLEGROUNDS",
            381210 => "Dead By Daylight",
            578080 => "PLAYERUNKNOWN'S BATTLEGROUNDS",
            1172380 => "Star Wars: Jedi Fallen Order",
            677620 => "Splitgate",
            1172620 => "Sea of Thieves",
            1665460 => "eFootball 2023",
            _ => value
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
