using MiScaleExporter.Core.Calibration;
using MiScaleExporter.Core.S400;

namespace MiScaleExporter.Core.Composition;

public sealed class S400BodyCompositionEstimator
{
    public const string AlgorithmVersion = "legacy-50khz-v1";

    public BodyCompositionEstimate Estimate(
        S400Measurement measurement,
        BodyProfile profile,
        FatCalibrationFit? calibration = null)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        ArgumentNullException.ThrowIfNull(profile);

        if (measurement.Quality != S400MeasurementQuality.DualFrequencyComplete
            || !measurement.Impedance50KhzOhm.HasValue)
        {
            throw new ArgumentException("A complete dual-frequency S400 measurement is required.", nameof(measurement));
        }
        if (profile.HeightCm is < 90 or > 220 || profile.Age is < 6 or > 120)
        {
            throw new ArgumentOutOfRangeException(nameof(profile), "Profile is outside supported S400 composition ranges.");
        }

        var weight = measurement.WeightKg;
        var height = profile.HeightCm;
        var age = profile.Age;
        var impedance = measurement.Impedance50KhzOhm.Value;
        var heightMeters = height / 100.0;
        var bmi = Clamp(weight / (heightMeters * heightMeters), 10, 90);
        var leanCoefficient = GetLeanCoefficient(weight, heightMeters, age, impedance);
        var baselineFat = GetFatPercentage(weight, height, age, profile.Sex, leanCoefficient);
        var fat = calibration?.Correct(baselineFat) ?? baselineFat;
        var isCalibrated = calibration?.IsActive == true && Math.Abs(fat - baselineFat) > 1e-9;
        var bone = GetBoneMass(profile.Sex, leanCoefficient);
        var leanSoftMass = Clamp(weight - (fat / 100.0 * weight) - bone, 10, 120);
        var water = GetWaterPercentage(fat);
        var protein = Clamp(leanSoftMass / weight * 100.0 - water, 5, 32);
        var visceralFat = GetVisceralFat(weight, height, age, profile.Sex);
        var metabolicAge = GetMetabolicAge(weight, height, age, impedance, profile.Sex);
        var bmr = profile.Sex == BodySex.Male
            ? 10 * weight + 6.25 * height - 5 * age + 5
            : 10 * weight + 6.25 * height - 5 * age - 161;
        var physiqueRating = GetPhysiqueRating(fat, leanSoftMass, height, age, profile.Sex);

        return new BodyCompositionEstimate(
            AlgorithmVersion,
            weight,
            Math.Round(bmi, 1),
            Math.Round(fat, 1),
            Math.Round(water, 1),
            Math.Round(leanSoftMass, 2),
            Math.Round(bone, 2),
            Math.Round(protein, 1),
            Math.Round(visceralFat, 2),
            Math.Round(Clamp(bmr, 500, 10000), 0),
            Math.Round(metabolicAge, 0),
            Math.Round(22 * heightMeters * heightMeters, 2),
            physiqueRating,
            isCalibrated,
            calibration?.Version ?? "identity",
            calibration?.PointCount ?? 0,
            calibration != null && double.IsFinite(calibration.RSquared)
                ? calibration.RSquared
                : null,
            isCalibrated ? fat - baselineFat : 0,
            Math.Round(baselineFat, 1));
    }

    private static double GetLeanCoefficient(double weight, double heightMeters, int age, double impedance) =>
        9.058 * heightMeters * heightMeters
        + 0.32 * weight
        + 12.226
        - 0.0068 * impedance
        - 0.0542 * age;

    private static double GetFatPercentage(
        double weight,
        double height,
        int age,
        BodySex sex,
        double leanCoefficient)
    {
        var offset = sex switch
        {
            BodySex.Female when age <= 49 => 9.25,
            BodySex.Female => 7.25,
            _ => 0.8,
        };
        var coefficient = 1.0;
        if (sex == BodySex.Male && weight < 61)
        {
            coefficient = 0.98;
        }
        else if (sex == BodySex.Female && weight > 60)
        {
            coefficient = height > 160 ? 1.03 : 0.96;
        }
        else if (sex == BodySex.Female && weight < 50)
        {
            coefficient = height > 160 ? 1.03 : 1.02;
        }

        var fat = (1.0 - ((leanCoefficient - offset) * coefficient / weight)) * 100;
        if (fat > 63)
        {
            fat = 75;
        }
        return Clamp(fat, 5, 75);
    }

    private static double GetWaterPercentage(double fatPercentage)
    {
        var water = (100 - fatPercentage) * 0.7;
        var coefficient = water <= 50 ? 1.02 : 0.98;
        if (water * coefficient >= 65)
        {
            water = 75;
        }
        return Clamp(water * coefficient, 35, 75);
    }

    private static double GetBoneMass(BodySex sex, double leanCoefficient)
    {
        var baseline = sex == BodySex.Female ? 0.245691014 : 0.18016894;
        var boneMass = (baseline - leanCoefficient * 0.05158) * -1;
        boneMass += boneMass > 2.2 ? 0.1 : -0.1;
        if ((sex == BodySex.Female && boneMass > 5.1)
            || (sex == BodySex.Male && boneMass > 5.2))
        {
            boneMass = 8;
        }
        return Clamp(boneMass, 0.5, 8);
    }

    private static double GetMetabolicAge(
        double weight,
        double height,
        int age,
        double impedance,
        BodySex sex)
    {
        var value = sex == BodySex.Male
            ? height * -0.7471 + weight * 0.9161 + age * 0.4184 + impedance * 0.0517 + 54.2267
            : height * -1.1165 + weight * 1.5784 + age * 0.4615 + impedance * 0.0415 + 83.2548;
        return Clamp(value, 15, 80);
    }

    private static double GetVisceralFat(
        double weight,
        double height,
        int age,
        BodySex sex)
    {
        double value;
        if (sex == BodySex.Female)
        {
            if (weight > (13 - height * 0.5) * -1)
            {
                var denominator = height * 1.45 + height * 0.1158 * height - 120;
                value = weight * 500 / denominator - 6 + age * 0.07;
            }
            else
            {
                var coefficient = 0.691 + height * -0.0024 + height * -0.0024;
                value = ((height * 0.027 - coefficient * weight) * -1) + age * 0.07 - age;
            }
        }
        else if (height < weight * 1.6)
        {
            var denominator = ((height * 0.4) - height * (height * 0.0826)) * -1 + 48;
            value = weight * 305 / denominator - 2.9 + age * 0.15;
        }
        else
        {
            var coefficient = 0.765 + height * -0.0015;
            value = ((height * 0.143 - weight * coefficient) * -1) + age * 0.15 - 5;
        }
        return Clamp(value, 1, 50);
    }

    private static int GetPhysiqueRating(
        double fat,
        double leanSoftMass,
        double height,
        int age,
        BodySex sex)
    {
        var fatScale = GetFatScale(age, sex);
        var fatFactor = fat > fatScale[2] ? 0 : fat < fatScale[1] ? 2 : 1;
        var muscleScale = GetMuscleScale(height, sex);
        var muscleFactor = leanSoftMass > muscleScale[1] ? 2 : leanSoftMass < muscleScale[0] ? 0 : 1;
        return muscleFactor + fatFactor * 3 + 1;
    }

    private static double[] GetFatScale(int age, BodySex sex)
    {
        if (sex == BodySex.Male)
        {
            if (age < 18) return [7, 16, 25, 30];
            if (age < 40) return [11, 17, 22, 27];
            if (age < 60) return [12, 18, 23, 28];
            return [14, 20, 25, 30];
        }
        if (age < 12) return [12, 21, 30, 34];
        if (age < 14) return [15, 24, 33, 37];
        if (age < 16) return [18, 27, 36, 40];
        if (age < 18) return [20, 28, 37, 41];
        if (age < 40) return [21, 28, 35, 40];
        if (age < 60) return [22, 29, 36, 41];
        return [23, 30, 37, 42];
    }

    private static double[] GetMuscleScale(double height, BodySex sex)
    {
        if (sex == BodySex.Male)
        {
            if (height >= 170) return [49.4, 59.5];
            if (height >= 160) return [44.0, 52.5];
            return [38.5, 46.6];
        }
        if (height >= 160) return [36.5, 42.6];
        if (height >= 150) return [32.9, 37.6];
        return [29.1, 34.8];
    }

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Clamp(value, minimum, maximum);
}