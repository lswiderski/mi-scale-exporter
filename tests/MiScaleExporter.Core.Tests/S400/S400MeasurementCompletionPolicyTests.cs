using MiScaleExporter.Core.S400;

namespace MiScaleExporter.Core.Tests.S400;

public sealed class S400MeasurementCompletionPolicyTests
{
    [Theory]
    [InlineData(S400MeasurementQuality.Incomplete, false)]
    [InlineData(S400MeasurementQuality.WeightOnly, true)]
    [InlineData(S400MeasurementQuality.DualFrequencyComplete, true)]
    public void CanCompleteScan_OnlyAcceptsTerminalMeasurementQualities(
        S400MeasurementQuality quality,
        bool expected)
    {
        Assert.Equal(expected, S400MeasurementCompletionPolicy.CanCompleteScan(quality));
    }

    [Theory]
    [InlineData(7, 7, true, true)]
    [InlineData(7, 8, true, false)]
    [InlineData(7, 7, false, false)]
    public void CanPublishScan_RequiresCurrentPendingGeneration(
        int expectedGeneration,
        int currentGeneration,
        bool completionPending,
        bool expected)
    {
        Assert.Equal(
            expected,
            S400MeasurementCompletionPolicy.CanPublishScan(
                S400MeasurementQuality.DualFrequencyComplete,
                expectedGeneration,
                currentGeneration,
                completionPending));
    }

    [Theory]
    [InlineData(S400MeasurementQuality.Incomplete, true, false)]
    [InlineData(S400MeasurementQuality.WeightOnly, true, true)]
    [InlineData(S400MeasurementQuality.DualFrequencyComplete, true, true)]
    [InlineData(S400MeasurementQuality.DualFrequencyComplete, false, false)]
    public void CanPersistScan_RequiresEnabledHistoryAndTerminalMeasurement(
        S400MeasurementQuality quality,
        bool historyEnabled,
        bool expected)
    {
        Assert.Equal(
            expected,
            S400MeasurementCompletionPolicy.CanPersistScan(quality, historyEnabled));
    }

    [Theory]
    [InlineData(S400MeasurementQuality.Incomplete, false)]
    [InlineData(S400MeasurementQuality.WeightOnly, false)]
    [InlineData(S400MeasurementQuality.DualFrequencyComplete, true)]
    public void CanUploadComposition_RequiresDualFrequencyCompletion(
        S400MeasurementQuality quality,
        bool expected)
    {
        Assert.Equal(expected, S400MeasurementCompletionPolicy.CanUploadComposition(quality));
    }
}