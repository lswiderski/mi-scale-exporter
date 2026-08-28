using MiScaleExporter.Core.Composition;
using MiScaleExporter.Core.S400;

namespace MiScaleExporter.Core.History;

public sealed record MeasurementHistoryRecord(
    string MeasurementId,
    DateTimeOffset MeasuredAt,
    S400MeasurementQuality Quality,
    byte ProfileId,
    double WeightKg,
    int? HeartRateBpm,
    double? Impedance50KhzOhm,
    double? Impedance250KhzOhm,
    IReadOnlyList<string> RawAdvertisementsHex,
    string AlgorithmVersion,
    string CalibrationVersion,
    BodyCompositionEstimate? Estimate,
    MeasurementUploadState UploadState,
    DateTimeOffset? LastUploadAttempt,
    string? UploadError);