namespace MiScaleExporter.Core.Calibration;

public sealed record CalibrationReference(
    string MeasurementId,
    DateTimeOffset MeasuredAt,
    CalibrationReferenceSource Source,
    double BaselineFatPercentage,
    double ReferenceFatPercentage,
    double WeightKg,
    double Impedance50KhzOhm,
    double Impedance250KhzOhm,
    string BaselineAlgorithmVersion);