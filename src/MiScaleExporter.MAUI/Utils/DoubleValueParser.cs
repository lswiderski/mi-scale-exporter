using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MiScaleExporter.MAUI.Utils
{
    public static class DoubleValueParser
    {
        public static bool IsValid(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            var parsed = ParseValueFromUsersCulture(value);

            return parsed != null;

        }

        public static string CheckValue(string value)
        {
            if (IsValid(value))
            {
                return value;
            }
            if (value.Length > 1)
            {
                return CheckValue(value.Remove(value.Length - 1));
            }
            return string.Empty;

        }

        public static double? ParseValueFromUsersCulture(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            var parsed = ParseDouble(value);
            if (parsed == null)
            {
                parsed = ParseDouble(value.Replace(',', '.'));
            }
            if (parsed == null)
            {
                parsed = ParseDouble(value.Replace('.', ','));
            }
            if (parsed == null) // when , or . is last character
            {
                parsed = ParseDouble(value.Remove(value.Length - 1));
            }

            return parsed;
        }

        private static double? ParseDouble(string value)
        {
            if (double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double parsed))
            {
                return parsed;
            }
            else
            {
                return null;
            }
        }
    }
}
