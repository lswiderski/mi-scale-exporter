using MiScaleExporter.Models;
using MiScaleExporter.Services;
using NUnit.Framework;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace MiScaleExporter.UnitTests;

[TestFixture]
public class GarminWeightExportMapperTests
{
    private static readonly DateTime MeasurementTime =
        new(2026, 7, 12, 8, 30, 0, DateTimeKind.Utc);

    [Test]
    public void S400UsesSkeletalMuscleMassAndBasalMetAcrossAllPayloads()
    {
        var bodyComposition = CreateBodyComposition();
        bodyComposition.MuscleMass = 58.6;
        bodyComposition.SkeletalMuscleMass = 40.0;
        bodyComposition.BMR = 1702;

        var export = GarminWeightExportMapper.Map(bodyComposition, MeasurementTime);
        var direct = GarminWeightExportMapper.ToGarminDto(export);
        var proxy = GarminWeightExportMapper.ToProxyRequest(export, new CredentialsData
        {
            Email = "user@example.com",
            Password = "password",
        });

        Assert.That(export.SkeletalMuscleMass, Is.EqualTo(40.0f));
        Assert.That(export.BasalMet, Is.EqualTo(1702f));
        Assert.That(direct.SkeletalMuscleMass, Is.EqualTo(export.SkeletalMuscleMass));
        Assert.That(direct.BasalMet, Is.EqualTo(1702f));
        Assert.That(proxy.SkeletalMuscleMass, Is.EqualTo(export.SkeletalMuscleMass));
        Assert.That(proxy.MuscleMass, Is.EqualTo(export.SkeletalMuscleMass));
        Assert.That(proxy.BasalMet, Is.EqualTo(export.BasalMet));
        Assert.That(proxy.TimeStamp, Is.EqualTo(new DateTimeOffset(MeasurementTime).ToUnixTimeSeconds()));
    }

    [Test]
    public void LegacyMeasurementUsesTotalMuscleMass()
    {
        var bodyComposition = CreateBodyComposition();
        bodyComposition.MuscleMass = 58.6;
        bodyComposition.SkeletalMuscleMass = null;

        var export = GarminWeightExportMapper.Map(bodyComposition, MeasurementTime);

        Assert.That(export.SkeletalMuscleMass, Is.EqualTo(58.6f).Within(0.001f));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void NonPositiveBmrIsOmitted(double bmr)
    {
        var bodyComposition = CreateBodyComposition();
        bodyComposition.BMR = bmr;

        var export = GarminWeightExportMapper.Map(bodyComposition, MeasurementTime);

        Assert.That(export.BasalMet, Is.Null);
    }

    [Test]
    public void NonFiniteBmrIsOmitted()
    {
        var bodyComposition = CreateBodyComposition();
        bodyComposition.BMR = double.NaN;

        var export = GarminWeightExportMapper.Map(bodyComposition, MeasurementTime);

        Assert.That(export.BasalMet, Is.Null);
    }

    private static BodyComposition CreateBodyComposition() => new()
    {
        Weight = 74.2,
        Fat = 16.9,
        WaterPercentage = 60.7,
        BoneMass = 3.1,
        MuscleMass = 58.6,
        VisceralFat = 7,
        BodyType = 5,
        MetabolicAge = 30,
        BMI = 22.4,
        BMR = 1702,
    };
}
