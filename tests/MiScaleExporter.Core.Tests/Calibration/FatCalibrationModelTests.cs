using MiScaleExporter.Core.Calibration;

namespace MiScaleExporter.Core.Tests.Calibration;

public sealed class FatCalibrationModelTests
{
    [Fact]
    public void Fit_NoReferences_ReturnsIdentity()
    {
        var fit = FatCalibrationModel.Fit([]);

        Assert.Equal(FatCalibrationMode.Identity, fit.Mode);
        Assert.False(fit.IsActive);
        Assert.Equal(20, fit.Correct(20));
    }

    [Fact]
    public void Fit_TwoReferences_DoesNotApplyCorrection()
    {
        var fit = FatCalibrationModel.Fit([
            Reference("a", 20, 22),
            Reference("b", 21, 23),
        ]);

        Assert.Equal(FatCalibrationMode.InsufficientData, fit.Mode);
        Assert.False(fit.IsActive);
        Assert.Equal(20, fit.Correct(20));
    }

    [Fact]
    public void Fit_ThreeReferences_AppliesMeanOffsetWithinRange()
    {
        var fit = FatCalibrationModel.Fit([
            Reference("a", 20, 22),
            Reference("b", 21, 23),
            Reference("c", 22, 24),
        ]);

        Assert.Equal(FatCalibrationMode.Offset, fit.Mode);
        Assert.True(fit.IsActive);
        Assert.Equal(1, fit.Slope);
        Assert.Equal(2, fit.Intercept);
        Assert.Equal(23, fit.Correct(21));
    }

    [Fact]
    public void Fit_SixSpreadReferences_AppliesBoundedLinearFit()
    {
        var fit = FatCalibrationModel.Fit([
            Reference("a", 15, 15.5),
            Reference("b", 17, 17.7),
            Reference("c", 19, 19.9),
            Reference("d", 21, 22.1),
            Reference("e", 23, 24.3),
            Reference("f", 25, 26.5),
        ]);

        Assert.Equal(FatCalibrationMode.Linear, fit.Mode);
        Assert.True(fit.IsActive);
        Assert.Equal(1.1, fit.Slope, 6);
        Assert.Equal(-1, fit.Intercept, 6);
        Assert.Equal(21, fit.Correct(20), 6);
        Assert.Equal(1, fit.RSquared, 6);
    }

    [Fact]
    public void Correct_OutsideReferenceMargin_DoesNotExtrapolate()
    {
        var fit = FatCalibrationModel.Fit([
            Reference("a", 20, 22),
            Reference("b", 21, 23),
            Reference("c", 22, 24),
        ]);

        Assert.Equal(30, fit.Correct(30));
    }

    [Fact]
    public void Fit_UnpairedReferencesWithoutRawMeasurement_RemainInactive()
    {
        var fit = FatCalibrationModel.Fit([
            Reference("a", 20, 22) with { WeightKg = 0, Impedance50KhzOhm = 0, Impedance250KhzOhm = 0 },
            Reference("b", 21, 23) with { WeightKg = 0, Impedance50KhzOhm = 0, Impedance250KhzOhm = 0 },
            Reference("c", 22, 24) with { WeightKg = 0, Impedance50KhzOhm = 0, Impedance250KhzOhm = 0 },
        ]);

        Assert.False(fit.IsActive);
        Assert.Equal(0, fit.PointCount);
    }

    [Fact]
    public void Fit_OffsetMode_UsesMedianToResistMistypedReference()
    {
        var fit = FatCalibrationModel.Fit([
            Reference("a", 20, 22),
            Reference("b", 21, 23),
            Reference("c", 22, 37),
        ]);

        Assert.Equal(FatCalibrationMode.Offset, fit.Mode);
        Assert.Equal(2, fit.Intercept);
    }

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
