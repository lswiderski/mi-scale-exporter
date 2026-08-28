namespace MiScaleExporter.Core.S400;

public sealed record S400MeasurementUpdate(
    double? PreviewWeightKg,
    S400Measurement? PartialMeasurement,
    S400Measurement? Completed);