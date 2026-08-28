using MiScaleExporter.Core.Composition;

namespace MiScaleExporter.Core.Garmin;

public sealed record GarminCompositionInput(
    DateTimeOffset MeasuredAt,
    double WeightKg,
    double? Bmi,
    bool IncludeComposition,
    double? FatPercentage,
    double? HydrationPercentage,
    double? LeanSoftMassKg,
    double? BoneMassKg,
    double? VisceralFatRating,
    double? PhysiqueRating,
    double? MetabolicAge,
    BodySex Sex);