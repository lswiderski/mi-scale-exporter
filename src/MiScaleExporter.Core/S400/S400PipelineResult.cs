using MiScaleExporter.Core.Composition;

namespace MiScaleExporter.Core.S400;

public sealed record S400PipelineResult(
    double? PreviewWeightKg,
    S400Measurement? PartialMeasurement,
    S400Measurement? Measurement,
    BodyCompositionEstimate? Estimate);