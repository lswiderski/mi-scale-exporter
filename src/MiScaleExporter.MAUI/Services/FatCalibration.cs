using System;
using System.Collections.Generic;
using System.Linq;
using MiScaleExporter.Models;
using Newtonsoft.Json;

namespace MiScaleExporter.Services
{
    /// <summary>
    /// Linear calibration of the scale's estimated body fat % against trusted
    /// reference measurements: trueFat ≈ a * scaleFat + b, fitted by least squares.
    ///
    /// The fitted correction is applied to a <see cref="BodyComposition"/> as a delta,
    /// so an identity fit (a=1, b=0) changes nothing. Only fat-derived fields are
    /// adjusted (fat %, muscle mass, water %, protein %); bone mass, BMR, visceral fat,
    /// metabolic age and BMI are produced by the library independently of fat % and are
    /// left untouched.
    /// </summary>
    public static class FatCalibration
    {
        // Library clamp bounds, mirrored so corrected values stay in the same ranges.
        private const double FatMin = 5, FatMax = 75;
        private const double WaterMin = 35, WaterMax = 75;
        private const double ProteinMin = 5, ProteinMax = 32;

        public readonly struct Fit
        {
            public Fit(double a, double b, double r2, int pointCount)
            {
                A = a;
                B = b;
                R2 = r2;
                PointCount = pointCount;
            }

            /// <summary>Slope (proportional error).</summary>
            public double A { get; }

            /// <summary>Intercept (fixed offset).</summary>
            public double B { get; }

            /// <summary>Coefficient of determination; NaN when undefined (&lt;2 points or no spread).</summary>
            public double R2 { get; }

            public int PointCount { get; }

            /// <summary>True when the fit actually changes the reading.</summary>
            public bool IsMeaningful => PointCount > 0 && (Math.Abs(A - 1.0) > 1e-9 || Math.Abs(B) > 1e-9);

            public static Fit Identity => new Fit(1.0, 0.0, double.NaN, 0);

            public double Correct(double scaleFat) => A * scaleFat + B;
        }

        /// <summary>
        /// Fit a linear correction over the supplied calibration points.
        /// 0 points → identity; 1 point → pure ratio (a = true/scale, b = 0);
        /// 2+ points → ordinary least squares (falls back to ratio if all scale values are equal).
        /// </summary>
        public static Fit ComputeFit(IList<FatCalibrationPoint> points)
        {
            if (points == null || points.Count == 0)
            {
                return Fit.Identity;
            }

            var valid = points.Where(p => p.ScaleFat > 0 && p.TrueFat > 0).ToList();
            if (valid.Count == 0)
            {
                return Fit.Identity;
            }

            if (valid.Count == 1)
            {
                var p = valid[0];
                return new Fit(p.TrueFat / p.ScaleFat, 0.0, double.NaN, 1);
            }

            int n = valid.Count;
            double sumX = valid.Sum(p => p.ScaleFat);
            double sumY = valid.Sum(p => p.TrueFat);
            double sumXX = valid.Sum(p => p.ScaleFat * p.ScaleFat);
            double sumXY = valid.Sum(p => p.ScaleFat * p.TrueFat);

            double denom = n * sumXX - sumX * sumX;
            if (Math.Abs(denom) < 1e-9)
            {
                // No spread in scale values: can't fit a slope, fall back to ratio of means.
                return new Fit(sumY / sumX, 0.0, double.NaN, n);
            }

            double a = (n * sumXY - sumX * sumY) / denom;
            double b = (sumY - a * sumX) / n;

            // R²
            double meanY = sumY / n;
            double ssTot = valid.Sum(p => (p.TrueFat - meanY) * (p.TrueFat - meanY));
            double ssRes = valid.Sum(p =>
            {
                double predicted = a * p.ScaleFat + b;
                double resid = p.TrueFat - predicted;
                return resid * resid;
            });
            double r2 = ssTot < 1e-12 ? double.NaN : 1.0 - ssRes / ssTot;

            return new Fit(a, b, r2, n);
        }

        /// <summary>
        /// Apply a fitted correction in place. Adjusts fat % and recomputes the fat-derived
        /// fields as deltas off the library's values, so consistency is preserved and an
        /// identity fit is a no-op. Returns the same instance for convenience.
        /// </summary>
        public static BodyComposition Apply(BodyComposition bc, Fit fit)
        {
            if (bc == null || !fit.IsMeaningful || bc.Fat <= 0 || bc.Weight <= 0)
            {
                return bc;
            }

            double fatOld = bc.Fat;
            double fatNew = Math.Clamp(fit.Correct(fatOld), FatMin, FatMax);
            if (Math.Abs(fatNew - fatOld) < 1e-9)
            {
                return bc;
            }

            double fatMassOld = bc.Weight * fatOld / 100.0;
            double fatMassNew = bc.Weight * fatNew / 100.0;

            bc.Fat = Math.Round(fatNew, 2);

            // Muscle mass = weight - fatMass - boneMass (library formula). Weight and bone
            // are unchanged, so muscle rises by exactly the reduction in fat mass.
            bc.MuscleMass = Math.Round(bc.MuscleMass + (fatMassOld - fatMassNew), 2);

            // Water % = (100 - fat) * 0.7 in the library; shift by the same delta.
            bc.WaterPercentage = Math.Round(
                Math.Clamp(bc.WaterPercentage + 0.7 * (fatOld - fatNew), WaterMin, WaterMax), 2);

            // Protein % = muscle/weight*100 - water; recompute from the corrected values.
            bc.ProteinPercentage = Math.Round(
                Math.Clamp(bc.MuscleMass / bc.Weight * 100.0 - bc.WaterPercentage, ProteinMin, ProteinMax), 2);

            return bc;
        }

        // ---- Persistence (JSON list in Preferences) -------------------------------------

        public static List<FatCalibrationPoint> LoadPoints()
        {
            var json = Preferences.Get(PreferencesKeys.FatCalibrationPoints, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<FatCalibrationPoint>();
            }

            try
            {
                return JsonConvert.DeserializeObject<List<FatCalibrationPoint>>(json)
                       ?? new List<FatCalibrationPoint>();
            }
            catch
            {
                return new List<FatCalibrationPoint>();
            }
        }

        public static void SavePoints(IEnumerable<FatCalibrationPoint> points)
        {
            Preferences.Set(PreferencesKeys.FatCalibrationPoints,
                JsonConvert.SerializeObject(points ?? Enumerable.Empty<FatCalibrationPoint>()));
        }

        public static bool IsEnabled => Preferences.Get(PreferencesKeys.UseFatCalibration, false);

        /// <summary>Convenience: apply the saved, enabled calibration to a measurement.</summary>
        public static BodyComposition ApplySaved(BodyComposition bc)
        {
            if (bc == null || !IsEnabled)
            {
                return bc;
            }

            return Apply(bc, ComputeFit(LoadPoints()));
        }
    }
}
