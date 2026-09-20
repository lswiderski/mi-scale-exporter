using System.Globalization;

namespace MiScaleExporter.MAUI.Converters;

public class ByteArrayToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte[] byteArray)
        {
            return byteArray.Length > 0;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
