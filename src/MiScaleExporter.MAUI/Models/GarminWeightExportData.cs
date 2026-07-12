namespace MiScaleExporter.Models;

/// <summary>
/// Canonical Garmin weight payload used by direct upload, FIT generation, and the proxy API.
/// </summary>
public sealed record GarminWeightExportData
{
    public DateTime TimeStamp { get; init; }
    public float Weight { get; init; }
    public float PercentFat { get; init; }
    public float PercentHydration { get; init; }
    public float BoneMass { get; init; }
    public float SkeletalMuscleMass { get; init; }
    public byte VisceralFatRating { get; init; }
    public float VisceralFatMass { get; init; }
    public byte PhysiqueRating { get; init; }
    public byte MetabolicAge { get; init; }
    public float BodyMassIndex { get; init; }
    public float? BasalMet { get; init; }
}
