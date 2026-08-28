using MiScaleExporter.Core.Composition;

namespace MiScaleExporter.Models;

public sealed class CalibrationMeasurementOption
{
    public string MeasurementId { get; init; }
    public DateTimeOffset MeasuredAt { get; init; }
    public double WeightKg { get; init; }
    public double? Impedance50KhzOhm { get; init; }
    public double? Impedance250KhzOhm { get; init; }
    public string AlgorithmVersion { get; init; }
    public BodyCompositionEstimate Estimate { get; init; }

    public string Display =>
        $"{MeasuredAt.LocalDateTime:g} · {WeightKg:0.0} kg · {Estimate.FatPercentage:0.0}% fat";
}
