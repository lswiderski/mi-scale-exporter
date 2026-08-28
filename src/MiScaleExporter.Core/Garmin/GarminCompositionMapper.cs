namespace MiScaleExporter.Core.Garmin;

public static class GarminCompositionMapper
{
    public static GarminCompositionPayload Map(GarminCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!double.IsFinite(input.WeightKg) || input.WeightKg is <= 0 or > 300)
        {
            throw new GarminMappingException("Garmin weight must be between 0 and 300 kg.");
        }
        if (input.MeasuredAt.Year is < 2000 or > 2100)
        {
            throw new GarminMappingException("Garmin measurement timestamp is outside the supported range.");
        }

        return new GarminCompositionPayload(
            input.MeasuredAt,
            (float)input.WeightKg,
            OptionalFloat(input.Bmi, 5, 100),
            input.IncludeComposition ? OptionalFloat(input.FatPercentage, 1, 75) : null,
            input.IncludeComposition ? OptionalFloat(input.HydrationPercentage, 1, 100) : null,
            input.IncludeComposition ? OptionalFloat(input.LeanSoftMassKg, 0.1, input.WeightKg) : null,
            input.IncludeComposition ? OptionalFloat(input.BoneMassKg, 0.1, 10) : null,
            input.IncludeComposition ? OptionalByte(input.VisceralFatRating, 1, 59) : null,
            null,
            input.IncludeComposition ? OptionalByte(input.PhysiqueRating, 1, 9) : null,
            input.IncludeComposition ? OptionalByte(input.MetabolicAge, 1, 120) : null,
            input.Sex);
    }

    private static float? OptionalFloat(double? value, double minimum, double maximum) =>
        value.HasValue
        && double.IsFinite(value.Value)
        && value.Value >= minimum
        && value.Value <= maximum
            ? (float)value.Value
            : null;

    private static byte? OptionalByte(double? value, byte minimum, byte maximum) =>
        value.HasValue
        && double.IsFinite(value.Value)
        && value.Value >= minimum
        && value.Value <= maximum
            ? (byte)Math.Round(value.Value)
            : null;
}