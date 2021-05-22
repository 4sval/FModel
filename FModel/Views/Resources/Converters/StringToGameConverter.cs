using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters
{
    public class StringToGameConverter : IValueConverter
    {
        public static readonly StringToGameConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                "Fortnite" => "Fortnite",
                "Pewee" => "Rogue Company",
                "Rosemallow" => "The Outer Worlds",
                "Catnip" => "Borderlands 3",
                "AzaleaAlpha" => "The Cycle",
                "WorldExplorersLive" => "Battle Breakers",
                "Newt" => "Spellbreak",
                "VALORANT" => "Valorant",
                "MinecraftDungeons" => "Minecraft Dungeons",
                "shoebill" => "Star Wars: Jedi Fallen Order",
                "a99769d95d8f400baad1f67ab5dfe508" => "Core",
                _ => value,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}