//////////////////////////////////////////////
// Apache 2.0  - 2016-2018
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfHexaEditor.Core.Converters
{
    /// <summary>
    /// Used to convert long value to hexadecimal string like this 0xFFFFFFFF.
    /// </summary>
    public sealed class LongToHexStringConverter : GenericStaticInstance<LongToHexStringConverter>,IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value != null
                ? (long.TryParse(value.ToString(), out var longValue)
                    ? (longValue > -1
                        ? "0x" + longValue
                              .ToString(ConstantReadOnly.HexLineInfoStringFormat, CultureInfo.InvariantCulture)
                              .ToUpper()
                        : ConstantReadOnly.DefaultHex8String)
                    : ConstantReadOnly.DefaultHex8String)
                : ConstantReadOnly.DefaultHex8String;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }
}