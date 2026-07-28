using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MiScaleExporter.Models;

namespace MiScaleExporter.MAUI.Converters
{
    public class MetricStatusToColorConverter : IValueConverter
    {
        // Resolve via Application.Current.Resources so theming (Light/Dark) follows
        // the same palette keys defined in Colors.xaml.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value is MetricStatusLevel l ? l : MetricStatusLevel.Unknown;
            var resourceKey = key switch
            {
                MetricStatusLevel.Low => "StatusLowColor",
                MetricStatusLevel.Standard => "StatusStandardColor",
                MetricStatusLevel.High => "StatusHighColor",
                _ => "StatusUnknownColor",
            };

            if (Application.Current != null
                && Application.Current.Resources.TryGetValue(resourceKey, out var resource)
                && resource is Color c)
            {
                if (targetType == typeof(Brush)) return new SolidColorBrush(c);
                return c;
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
