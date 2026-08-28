namespace MiScaleExporter.Core.Composition;

public sealed record BodyCompositionEstimate(
    string AlgorithmVersion,
    double WeightKg,
    double Bmi,
    double FatPercentage,
    double WaterPercentage,
    double LeanSoftMassKg,
    double BoneMassKg,
    double ProteinPercentage,
    double VisceralFatRating,
    double BasalMetabolicRateKcal,
    double MetabolicAge,
    double IdealWeightKg,
    int PhysiqueRating,
    bool IsCalibrated,
    string CalibrationVersion,
    int CalibrationPointCount = 0,
    double? CalibrationRSquared = null,
    double AppliedFatCorrection = 0,
    double BaselineFatPercentage = 0);