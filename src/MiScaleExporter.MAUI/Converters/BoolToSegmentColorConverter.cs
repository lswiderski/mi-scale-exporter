using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MiScaleExporter.MAUI.Converters
{
    // Drives the segmented-selector look: maps a "selected" bool to a fill/text Color.
    // ConverterParameter "text" returns the foreground color, anything else returns the
    // segment background. Resolves theme colors from Application resources so Light/Dark
    // follow the same palette keys; pure MAUI (preview-safe, no platform deps).
    public class BoolToSegmentColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var selected = value is bool b && b;
            var mode = (parameter as string)?.ToLowerInvariant() ?? "background";
            var dark = Application.Current?.RequestedTheme == AppTheme.Dark;

            if (mode == "text")
            {
                if (selected) return Colors.White;
                return ResolveColor(dark ? "MutedTextDark" : "MutedTextLight", Colors.Gray);
            }

            return selected
                ? ResolveColor("Primary", Color.FromArgb("#007CC3"))
                : Colors.Transparent;
        }

        private static Color ResolveColor(string key, Color fallback)
        {
            if (Application.Current != null
                && Application.Current.Resources.TryGetValue(key, out var resource)
                && resource is Color c)
            {
                return c;
            }
            return fallback;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
