using System.Globalization;
using System.Windows.Data;

namespace Kassajournal.App.Converters;

/// <summary>Liefert true, wenn der gebundene Wert (decimal) kleiner als 0 ist - für rote Einfärbung negativer Differenzen.</summary>
public class NegativeToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is decimal d && d < 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
