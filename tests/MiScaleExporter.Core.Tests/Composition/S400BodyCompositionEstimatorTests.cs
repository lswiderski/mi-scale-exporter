using MiScaleExporter.Core.Calibration;
using MiScaleExporter.Core.Composition;
using MiScaleExporter.Core.S400;

namespace MiScaleExporter.Core.Tests.Composition;

public sealed class S400BodyCompositionEstimatorTests
{
    [Fact]
    public void Estimate_CompleteMeasurement_ReturnsVersionedBoundedEstimate()
    {
        var measurement = new S400Measurement(
            "s400-01-1744258797-699",
            1,
            1744258797,
            DateTimeOffset.FromUnixTimeSeconds(1744258797),
            69.9,
            92,
            543.2,
            497.6,
            S400MeasurementQuality.DualFrequencyComplete,
            []);
        var profile = new BodyProfile(182, 29, BodySex.Male);
        var estimator = new S400BodyCompositionEstimator();

        var result = estimator.Estimate(measurement, profile);

        Assert.Equal("legacy-50khz-v1", result.AlgorithmVersion);
        Assert.Equal(21.1, result.Bmi);
        Assert.Equal(1696, result.BasalMetabolicRateKcal);
        Assert.Equal(16.3, result.FatPercentage);
        Assert.InRange(result.WaterPercentage, 35, 75);
        Assert.InRange(result.BoneMassKg, 0.5, 8);
        Assert.InRange(result.LeanSoftMassKg, 10, 120);
        Assert.InRange(result.ProteinPercentage, 5, 32);
        Assert.InRange(result.VisceralFatRating, 1, 50);
        Assert.InRange(result.MetabolicAge, 15, 80);
        Assert.InRange(result.PhysiqueRating, 1, 9);
        Assert.False(result.IsCalibrated);
    }

    [Fact]
    public void Estimate_ActiveCalibration_RecomputesFatDerivedMetrics()
    {
        var measurement = CreateMeasurement();
        var profile = new BodyProfile(182, 29, BodySex.Male);
        var estimator = new S400BodyCompositionEstimator();
        var fit = FatCalibrationModel.Fit([
            Reference("a", 15, 17),
            Reference("b", 16, 18),
            Reference("c", 17, 19),
        ]);

        var baseline = estimator.Estimate(measurement, profile);
        var calibrated = estimator.Estimate(measurement, profile, fit);

        Assert.Equal(18.3, calibrated.FatPercentage);
        Assert.NotEqual(baseline.WaterPercentage, calibrated.WaterPercentage);
        Assert.NotEqual(baseline.LeanSoftMassKg, calibrated.LeanSoftMassKg);
        Assert.NotEqual(baseline.ProteinPercentage, calibrated.ProteinPercentage);
        Assert.True(calibrated.IsCalibrated);
        Assert.Equal("offset-v1-3", calibrated.CalibrationVersion);
        Assert.Equal(3, calibrated.CalibrationPointCount);
        Assert.Null(calibrated.CalibrationRSquared);
        Assert.Equal(2, calibrated.AppliedFatCorrection, 6);
        Assert.Equal(baseline.FatPercentage, baseline.BaselineFatPercentage);
        Assert.Equal(baseline.FatPercentage, calibrated.BaselineFatPercentage);
    }

    private static S400Measurement CreateMeasurement() =>
        new(
            "s400-01-1744258797-699",
            1,
            1744258797,
            DateTimeOffset.FromUnixTimeSeconds(1744258797),
            69.9,
            92,
            543.2,
            497.6,
            S400MeasurementQuality.DualFrequencyComplete,
            []);

    private static CalibrationReference Reference(string id, double baseline, double reference) =>
        new(
            id,
            DateTimeOffset.Parse("2026-08-01T06:00:00Z"),
            CalibrationReferenceSource.XiaomiHome,
            baseline,
            reference,
            70,
            540,
            495,
            "legacy-50khz-v1");
}
