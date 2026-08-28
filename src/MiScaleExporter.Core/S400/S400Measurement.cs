namespace MiScaleExporter.Core.S400;

public sealed record S400Measurement(
    string MeasurementId,
    byte ProfileId,
    uint UnixTimestamp,
    DateTimeOffset MeasuredAt,
    double WeightKg,
    int? HeartRateBpm,
    double? Impedance50KhzOhm,
    double? Impedance250KhzOhm,
    S400MeasurementQuality Quality,
    IReadOnlyList<byte[]> RawAdvertisements);