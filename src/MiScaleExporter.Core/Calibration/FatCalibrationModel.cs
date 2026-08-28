namespace MiScaleExporter.Core.Calibration;

public static class FatCalibrationModel
{
    public static FatCalibrationFit Fit(IEnumerable<CalibrationReference> references)
    {
        ArgumentNullException.ThrowIfNull(references);

        var valid = references
            .Where(IsValid)
            .GroupBy(reference => reference.MeasurementId, StringComparer.Ordinal)
            .Select(group => group.Last())
            .OrderBy(reference => reference.MeasuredAt)
            .ToArray();

        if (valid.Length == 0)
        {
            return CreateInactive(FatCalibrationMode.Identity, 0, "identity");
        }

        var minimum = valid.Min(reference => reference.BaselineFatPercentage);
        var maximum = valid.Max(reference => reference.BaselineFatPercentage);
        if (valid.Length < 3)
        {
            return CreateInactive(
                FatCalibrationMode.InsufficientData,
                valid.Length,
                $"insufficient-v1-{valid.Length}",
                minimum,
                maximum);
        }

        if (valid.Length < 6 || maximum - minimum < 2)
        {
            return CreateOffsetFit(valid, minimum, maximum);
        }

        var count = valid.Length;
        var sumX = valid.Sum(reference => reference.BaselineFatPercentage);
        var sumY = valid.Sum(reference => reference.ReferenceFatPercentage);
        var sumXX = valid.Sum(reference => reference.BaselineFatPercentage * reference.BaselineFatPercentage);
        var sumXY = valid.Sum(reference => reference.BaselineFatPercentage * reference.ReferenceFatPercentage);
        var denominator = count * sumXX - sumX * sumX;
        if (Math.Abs(denominator) < 1e-9)
        {
            return CreateOffsetFit(valid, minimum, maximum);
        }

        var slope = Math.Clamp((count * sumXY - sumX * sumY) / denominator, 0.5, 1.5);
        var intercept = Math.Clamp((sumY - slope * sumX) / count, -15, 15);
        var meanY = sumY / count;
        var total = valid.Sum(reference => Math.Pow(reference.ReferenceFatPercentage - meanY, 2));
        var residual = valid.Sum(reference =>
            Math.Pow(reference.ReferenceFatPercentage
                     - (slope * reference.BaselineFatPercentage + intercept), 2));
        var rSquared = total < 1e-12 ? double.NaN : 1 - residual / total;

        return new FatCalibrationFit(
            FatCalibrationMode.Linear,
            slope,
            intercept,
            rSquared,
            count,
            minimum,
            maximum,
            $"linear-v1-{count}");
    }

    private static FatCalibrationFit CreateOffsetFit(
        IReadOnlyCollection<CalibrationReference> references,
        double minimum,
        double maximum)
    {
        var offsets = references
            .Select(reference => reference.ReferenceFatPercentage - reference.BaselineFatPercentage)
            .OrderBy(value => value)
            .ToArray();
        var middle = offsets.Length / 2;
        var median = offsets.Length % 2 == 0
            ? (offsets[middle - 1] + offsets[middle]) / 2
            : offsets[middle];
        var offset = Math.Clamp(median, -15, 15);
        return new FatCalibrationFit(
            FatCalibrationMode.Offset,
            1,
            offset,
            double.NaN,
            references.Count,
            minimum,
            maximum,
            $"offset-v1-{references.Count}");
    }

    private static FatCalibrationFit CreateInactive(
        FatCalibrationMode mode,
        int count,
        string version,
        double minimum = double.NegativeInfinity,
        double maximum = double.PositiveInfinity) =>
        new(mode, 1, 0, double.NaN, count, minimum, maximum, version);

    private static bool IsValid(CalibrationReference reference) =>
        !string.IsNullOrWhiteSpace(reference.MeasurementId)
        && double.IsFinite(reference.BaselineFatPercentage)
        && double.IsFinite(reference.ReferenceFatPercentage)
        && double.IsFinite(reference.WeightKg)
        && double.IsFinite(reference.Impedance50KhzOhm)
        && double.IsFinite(reference.Impedance250KhzOhm)
        && reference.BaselineFatPercentage is >= 5 and <= 75
        && reference.ReferenceFatPercentage is >= 5 and <= 75
        && reference.WeightKg is > 0 and <= 300
        && reference.Impedance50KhzOhm > 0
        && reference.Impedance250KhzOhm > 0
        && !string.IsNullOrWhiteSpace(reference.BaselineAlgorithmVersion);
}