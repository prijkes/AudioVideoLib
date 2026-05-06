namespace AudioVideoLib.Studio.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

public sealed class BoolToInTagConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is bool b && b) ? "in tag" : "—";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
