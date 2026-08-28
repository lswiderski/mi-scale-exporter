namespace MiScaleExporter.Core.S400;

public sealed record S400Packet(
    S400PacketKind Kind,
    byte ProfileId,
    uint UnixTimestamp,
    double? WeightKg,
    int? HeartRateBpm,
    double? Impedance50KhzOhm,
    double? Impedance250KhzOhm,
    bool IsFinal,
    byte[] RawAdvertisement);