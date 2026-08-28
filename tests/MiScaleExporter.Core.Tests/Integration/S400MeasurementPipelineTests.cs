using MiScaleExporter.Core.Composition;
using MiScaleExporter.Core.S400;

namespace MiScaleExporter.Core.Tests.Integration;

public sealed class S400MeasurementPipelineTests
{
    [Fact]
    public void Process_TwoPacketSequence_ReturnsPreviewThenCompleteEstimate()
    {
        var pipeline = new S400MeasurementPipeline();
        var profile = new BodyProfile(182, 29, BodySex.Male);

        var preview = pipeline.Process(
            Convert.FromHexString("4859D53B0ABC078FF2348C844138E930220000009E538599"),
            "0728974d657a4b60964c1b1677f35f7c",
            "8C:D0:B2:F6:BE:EF",
            profile);
        var complete = pipeline.Process(
            Convert.FromHexString("4859D53B0BD6EF0B25DB72785E7E2F46D6000000D8642DF6"),
            "0728974d657a4b60964c1b1677f35f7c",
            "8C:D0:B2:F6:BE:EF",
            profile);

        Assert.Equal(69.9, preview.PreviewWeightKg);
        Assert.Null(preview.Measurement);
        Assert.Null(preview.Estimate);
        Assert.NotNull(complete.Measurement);
        Assert.NotNull(complete.Estimate);
        Assert.Equal(S400MeasurementQuality.DualFrequencyComplete, complete.Measurement!.Quality);
        Assert.Equal(21.1, complete.Estimate!.Bmi);
        Assert.Equal("legacy-50khz-v1", complete.Estimate.AlgorithmVersion);
    }

    [Fact]
    public void Process_FirstPacketWithoutFinal_ExposesIncompleteMeasurementForFallback()
    {
        var pipeline = new S400MeasurementPipeline();

        var result = pipeline.Process(
            Convert.FromHexString("4859D53B0ABC078FF2348C844138E930220000009E538599"),
            "0728974d657a4b60964c1b1677f35f7c",
            "8C:D0:B2:F6:BE:EF",
            new BodyProfile(182, 29, BodySex.Male));

        Assert.Equal(69.9, result.PreviewWeightKg);
        Assert.NotNull(result.PartialMeasurement);
        Assert.Equal(S400MeasurementQuality.Incomplete, result.PartialMeasurement!.Quality);
        Assert.Equal(69.9, result.PartialMeasurement.WeightKg);
        Assert.Equal(543.2, result.PartialMeasurement.Impedance50KhzOhm);
        Assert.Single(result.PartialMeasurement.RawAdvertisements);
        Assert.Null(result.Measurement);
        Assert.Null(result.Estimate);
    }

    [Fact]
    public void Process_WeightOnlyPacket_CompletesWithoutEstimate()
    {
        var pipeline = new S400MeasurementPipeline();

        var result = pipeline.Process(
            Convert.FromHexString("4859D53B71530438B5894B242C209908DA000000479ECDA3"),
            "02d2900363ef629c736a4549677acbee",
            "04:AE:47:67:C6:7C",
            new BodyProfile(182, 29, BodySex.Male));

        Assert.Equal(S400MeasurementQuality.WeightOnly, result.Measurement!.Quality);
        Assert.Null(result.Estimate);
    }
}
