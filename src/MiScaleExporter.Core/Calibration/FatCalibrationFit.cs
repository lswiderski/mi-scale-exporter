namespace MiScaleExporter.Core.Calibration;

public sealed record FatCalibrationFit(
    FatCalibrationMode Mode,
    double Slope,
    double Intercept,
    double RSquared,
    int PointCount,
    double MinimumBaselineFat,
    double MaximumBaselineFat,
    string Version)
{
    private const double ExtrapolationMargin = 2;

    public bool IsActive => Mode is FatCalibrationMode.Offset or FatCalibrationMode.Linear;

    public double Correct(double baselineFatPercentage)
    {
        if (!IsActive
            || !double.IsFinite(baselineFatPercentage)
            || baselineFatPercentage < MinimumBaselineFat - ExtrapolationMargin
            || baselineFatPercentage > MaximumBaselineFat + ExtrapolationMargin)
        {
            return baselineFatPercentage;
        }

        return Math.Clamp(Slope * baselineFatPercentage + Intercept, 5, 75);
    }
}