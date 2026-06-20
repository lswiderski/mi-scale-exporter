using System.Globalization;
using MiScaleExporter.MAUI.Resources.Localization;
using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    public class MetricPresenter : IMetricPresenter
    {
        private static readonly CultureInfo C = CultureInfo.InvariantCulture;
        private const double KgToLb = 2.2046226218;

        private readonly IScaleCapabilities _capabilities;
        private readonly IMetricEvaluator _evaluator;

        public MetricPresenter(IScaleCapabilities capabilities, IMetricEvaluator evaluator)
        {
            _capabilities = capabilities;
            _evaluator = evaluator;
        }

        public IEnumerable<MetricCardModel> Build(
            BodyComposition composition,
            ScaleType scaleType,
            User user,
            BodyComposition previous = null)
        {
            if (composition == null)
                yield break;

            var useLbs = Preferences.Get(PreferencesKeys.DisplayWeightInLbs, false);
            var supported = _capabilities.GetSupported(scaleType);
            foreach (var key in supported)
            {
                if (!IsValuePresent(key, composition))
                    continue;

                if (RequiresImpedance(key) && !composition.HasImpedance)
                    continue;

                // Always read the raw kg value for status evaluation: the bands in
                // MetricEvaluator are kg-based and unit-independent for non-mass metrics.
                var rawValue = ExtractValue(key, composition);
                var status = _evaluator.Evaluate(key, rawValue, user);

                var displayValue = ConvertForDisplay(key, rawValue, useLbs);

                double? prevDisplay = null;
                if (previous != null && IsValuePresent(key, previous))
                {
                    prevDisplay = ConvertForDisplay(key, ExtractValue(key, previous), useLbs);
                }

                double? delta = prevDisplay.HasValue ? displayValue - prevDisplay.Value : null;
                string arrow = string.Empty;
                if (delta.HasValue)
                {
                    // Decide arrow direction on the RAW kg delta with a kg-based threshold (0.05 kg).
                    // The displayed delta number stays in the user's unit (above), but the up/down/flat
                    // classification must be unit-independent so a unit toggle never flips an arrow.
                    var rawDelta = rawValue - ExtractValue(key, previous);
                    if (rawDelta > 0.05) arrow = "▲";
                    else if (rawDelta < -0.05) arrow = "▼";
                    else arrow = "▬";
                }

                yield return new MetricCardModel
                {
                    Key = key,
                    Label = LabelFor(key),
                    FormattedValue = FormatValue(key, displayValue),
                    Unit = UnitFor(key, useLbs),
                    Status = status,
                    StatusLabel = StatusLabelFor(status),
                    Delta = delta,
                    DeltaFormatted = delta.HasValue ? FormatDelta(key, delta.Value) : string.Empty,
                    DeltaArrow = arrow,
                };
            }
        }

        private static bool IsMassMetric(MetricKey key) => key switch
        {
            MetricKey.Weight => true,
            MetricKey.MuscleMass => true,
            MetricKey.BoneMass => true,
            MetricKey.IdealWeight => true,
            _ => false,
        };

        private static double ConvertForDisplay(MetricKey key, double kgValue, bool useLbs)
            => useLbs && IsMassMetric(key) ? kgValue * KgToLb : kgValue;

        private static bool RequiresImpedance(MetricKey key) => key switch
        {
            MetricKey.Weight => false,
            MetricKey.BMI => false,
            _ => true,
        };

        private static bool IsValuePresent(MetricKey key, BodyComposition c)
        {
            var v = ExtractValue(key, c);
            // BodyType=0 is a meaningful category, not "missing"; presence is gated on HasImpedance.
            // Do NOT add `v > 0` here — a future contributor "fixing" the inconsistency would hide
            // the legitimate Obese=0 reading on scales that encode it that way.
            if (key == MetricKey.BodyType) return c.HasImpedance;
            return v > 0.0001;
        }

        private static double ExtractValue(MetricKey key, BodyComposition c) => key switch
        {
            MetricKey.Weight => c.Weight,
            MetricKey.BMI => c.BMI,
            MetricKey.Fat => c.Fat,
            MetricKey.WaterPercentage => c.WaterPercentage,
            MetricKey.MuscleMass => c.MuscleMass,
            MetricKey.BoneMass => c.BoneMass,
            MetricKey.VisceralFat => c.VisceralFat,
            MetricKey.BMR => c.BMR,
            MetricKey.MetabolicAge => c.MetabolicAge,
            MetricKey.ProteinPercentage => c.ProteinPercentage,
            MetricKey.IdealWeight => c.IdealWeight,
            MetricKey.BodyType => c.BodyType,
            _ => 0,
        };

        private static string LabelFor(MetricKey key) => key switch
        {
            MetricKey.Weight => "Weight",
            MetricKey.BMI => "BMI",
            MetricKey.Fat => "Body Fat",
            MetricKey.WaterPercentage => "Water",
            MetricKey.MuscleMass => "Muscle",
            MetricKey.BoneMass => "Bone Mass",
            MetricKey.VisceralFat => "Visceral Fat",
            MetricKey.BMR => "Basal Metabolism",
            MetricKey.MetabolicAge => "Metabolic Age",
            MetricKey.ProteinPercentage => "Protein",
            MetricKey.IdealWeight => "Ideal Weight",
            MetricKey.BodyType => "Body Type",
            _ => key.ToString(),
        };

        private static string UnitFor(MetricKey key, bool useLbs)
        {
            if (IsMassMetric(key)) return useLbs ? "lb" : "kg";
            return key switch
            {
                MetricKey.BMI => string.Empty,
                MetricKey.Fat => "%",
                MetricKey.WaterPercentage => "%",
                MetricKey.VisceralFat => string.Empty,
                MetricKey.BMR => "kcal",
                MetricKey.MetabolicAge => "yr",
                MetricKey.ProteinPercentage => "%",
                MetricKey.BodyType => string.Empty,
                _ => string.Empty,
            };
        }

        private static string FormatValue(MetricKey key, double v) => key switch
        {
            MetricKey.BMR => v.ToString("F0", C),
            MetricKey.VisceralFat => v.ToString("F0", C),
            MetricKey.MetabolicAge => v.ToString("F0", C),
            MetricKey.BodyType => v.ToString("F0", C),
            _ => v.ToString("F1", C),
        };

        private static string FormatDelta(MetricKey key, double d)
        {
            var abs = System.Math.Abs(d);
            return key switch
            {
                MetricKey.BMR or MetricKey.VisceralFat or MetricKey.MetabolicAge or MetricKey.BodyType
                    => abs.ToString("F0", C),
                _ => abs.ToString("F1", C),
            };
        }

        private static string StatusLabelFor(MetricStatusLevel s) => s switch
        {
            MetricStatusLevel.Low => AppSnippets.StatusLow,
            MetricStatusLevel.Standard => AppSnippets.StatusStandard,
            MetricStatusLevel.High => AppSnippets.StatusHigh,
            _ => string.Empty,
        };
    }
}
