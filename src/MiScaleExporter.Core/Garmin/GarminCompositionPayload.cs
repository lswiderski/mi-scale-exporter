using MiScaleExporter.Core.Composition;

namespace MiScaleExporter.Core.Garmin;

public sealed record GarminCompositionPayload(
    DateTimeOffset MeasuredAt,
    float WeightKg,
    float? Bmi,
    float? FatPercentage,
    float? HydrationPercentage,
    float? LeanSoftMassKg,
    float? BoneMassKg,
    byte? VisceralFatRating,
    float? VisceralFatMassKg,
    byte? PhysiqueRating,
    byte? MetabolicAge,
    BodySex Sex);