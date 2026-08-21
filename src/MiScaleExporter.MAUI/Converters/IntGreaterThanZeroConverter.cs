using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MiScaleExporter.MAUI.Converters
{
    public class IntGreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (value is int i) return i > 0;
            if (int.TryParse(value.ToString(), out var v)) return v > 0;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
