using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    // Reference bands are intentionally conservative and derived from public,
    // widely-cited consumer-health sources. They are guidance only, NOT medical advice.
    //
    // Sources:
    //   * BMI:              WHO classification (Underweight <18.5, Normal 18.5-24.9, Overweight >=25).
    //                       https://www.who.int/health-topics/obesity
    //   * Body Fat %:       American Council on Exercise (ACE) general fitness bands.
    //                       https://www.acefitness.org/resources/everyone/tools-calculators/percent-body-fat-calculator/
    //   * Body Water %:     USGS / general adult ranges (M ~50-65%, F ~45-60%).
    //   * Visceral Fat:     Tanita / Omron consumer scales typical guidance (1-9 healthy, 10-14 high, 15+ very high).
    //   * Bone Mass (kg):   Tanita guidance based on body-weight tiers.
    //   * Muscle Mass (kg): Coarse adult ranges; Tanita / Omron consumer guidance.
    //   * Protein %:        Generic consumer-scale ranges (~16-20%).
    //   * BMR (kcal):       Bands relative to Mifflin-St Jeor expected value; we do not duplicate
    //                       the BMR formula here, we only flag clear outliers.
    //   * MetabolicAge:     Compared to chronological age.
    public class MetricEvaluator : IMetricEvaluator
    {
        public MetricStatusLevel Evaluate(MetricKey key, double value, User user)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return MetricStatusLevel.Unknown;

            var sex = user?.Sex ?? Sex.Male;
            var age = user?.Age ?? 30;

            return key switch
            {
                MetricKey.BMI => Band(value, 18.5, 25.0),
                MetricKey.Fat => sex == Sex.Female
                    ? FatFemale(value, age)
                    : FatMale(value, age),
                MetricKey.WaterPercentage => sex == Sex.Female
                    ? Band(value, 45.0, 60.0)
                    : Band(value, 50.0, 65.0),
                MetricKey.VisceralFat => value <= 9 ? MetricStatusLevel.Standard : MetricStatusLevel.High,
                MetricKey.ProteinPercentage => Band(value, 16.0, 20.0),
                MetricKey.BoneMass => BoneMass(value, sex),
                MetricKey.MuscleMass => MuscleMass(value, sex),
                // 'younger than chronological age' has no Low band intentionally (it is good news).
                MetricKey.MetabolicAge => value <= age + 3 ? MetricStatusLevel.Standard : MetricStatusLevel.High,
                // Weight, BMR, IdealWeight, BodyType, Weight are shown without a colored band.
                _ => MetricStatusLevel.Unknown,
            };
        }

        private static MetricStatusLevel Band(double v, double low, double high)
        {
            if (v < low) return MetricStatusLevel.Low;
            if (v >= high) return MetricStatusLevel.High;
            return MetricStatusLevel.Standard;
        }

        // ACE adult bands, simplified for two age tiers.
        private static MetricStatusLevel FatMale(double v, int age)
        {
            // <40y healthy ~ 8-19%, >=40y healthy ~ 11-22%
            return age < 40 ? Band(v, 8.0, 19.0) : Band(v, 11.0, 22.0);
        }

        private static MetricStatusLevel FatFemale(double v, int age)
        {
            // <40y healthy ~ 21-32%, >=40y healthy ~ 23-34%
            return age < 40 ? Band(v, 21.0, 32.0) : Band(v, 23.0, 34.0);
        }

        // Very coarse adult bone-mass guidance (kg).
        private static MetricStatusLevel BoneMass(double v, Sex sex)
        {
            return sex == Sex.Female
                ? Band(v, 1.8, 2.5)
                : Band(v, 2.5, 3.5);
        }

        // Very coarse adult skeletal-muscle mass guidance (kg).
        private static MetricStatusLevel MuscleMass(double v, Sex sex)
        {
            return sex == Sex.Female
                ? Band(v, 20.0, 35.0)
                : Band(v, 30.0, 50.0);
        }
    }
}
