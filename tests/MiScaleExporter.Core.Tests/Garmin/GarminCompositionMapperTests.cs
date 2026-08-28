using MiScaleExporter.Core.Composition;
using MiScaleExporter.Core.Garmin;

namespace MiScaleExporter.Core.Tests.Garmin;

public sealed class GarminCompositionMapperTests
{
    [Fact]
    public void Map_CompleteMeasurement_MapsValidOptionalFieldsAndProfile()
    {
        var measuredAt = DateTimeOffset.Parse("2026-08-22T06:30:00+03:00");
        var input = CompleteInput(measuredAt) with { Sex = BodySex.Female };

        var payload = GarminCompositionMapper.Map(input);

        Assert.Equal(measuredAt, payload.MeasuredAt);
        Assert.Equal(70, payload.WeightKg);
        Assert.Equal(22.1f, payload.Bmi);
        Assert.Equal(24.5f, payload.FatPercentage);
        Assert.Equal(55.2f, payload.HydrationPercentage);
        Assert.Equal(48.1f, payload.LeanSoftMassKg);
        Assert.Equal(2.9f, payload.BoneMassKg);
        Assert.Equal((byte)7, payload.VisceralFatRating);
        Assert.Null(payload.VisceralFatMassKg);
        Assert.Equal((byte)5, payload.PhysiqueRating);
        Assert.Equal((byte)31, payload.MetabolicAge);
        Assert.Equal(BodySex.Female, payload.Sex);
    }

    [Fact]
    public void Map_WeightOnly_OmitsEveryCompositionField()
    {
        var input = CompleteInput(DateTimeOffset.UtcNow) with { IncludeComposition = false };

        var payload = GarminCompositionMapper.Map(input);

        Assert.Null(payload.FatPercentage);
        Assert.Null(payload.HydrationPercentage);
        Assert.Null(payload.LeanSoftMassKg);
        Assert.Null(payload.BoneMassKg);
        Assert.Null(payload.VisceralFatRating);
        Assert.Null(payload.VisceralFatMassKg);
        Assert.Null(payload.PhysiqueRating);
        Assert.Null(payload.MetabolicAge);
        Assert.Equal(22.1f, payload.Bmi);
    }

    [Fact]
    public void Map_InvalidOptionalValue_OmitsOnlyThatValue()
    {
        var input = CompleteInput(DateTimeOffset.UtcNow) with
        {
            FatPercentage = double.NaN,
            MetabolicAge = 400,
        };

        var payload = GarminCompositionMapper.Map(input);

        Assert.Null(payload.FatPercentage);
        Assert.Null(payload.MetabolicAge);
        Assert.Equal(55.2f, payload.HydrationPercentage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Map_InvalidWeight_Throws(double weight)
    {
        var input = CompleteInput(DateTimeOffset.UtcNow) with { WeightKg = weight };

        Assert.Throws<GarminMappingException>(() => GarminCompositionMapper.Map(input));
    }

    private static GarminCompositionInput CompleteInput(DateTimeOffset measuredAt) =>
        new(
            measuredAt,
            70,
            22.1,
            true,
            24.5,
            55.2,
            48.1,
            2.9,
            7,
            5,
            31,
            BodySex.Male);
}
