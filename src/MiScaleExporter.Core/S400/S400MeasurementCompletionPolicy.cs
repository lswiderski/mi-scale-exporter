namespace MiScaleExporter.Core.S400;

public static class S400MeasurementCompletionPolicy
{
    public static bool CanCompleteScan(S400MeasurementQuality quality) =>
        quality is S400MeasurementQuality.WeightOnly
            or S400MeasurementQuality.DualFrequencyComplete;

    public static bool CanPersistScan(
        S400MeasurementQuality quality,
        bool historyEnabled) =>
        historyEnabled && CanCompleteScan(quality);

    public static bool CanUploadComposition(S400MeasurementQuality quality) =>
        quality == S400MeasurementQuality.DualFrequencyComplete;

    public static bool CanPublishScan(
        S400MeasurementQuality quality,
        int expectedGeneration,
        int currentGeneration,
        bool completionPending) =>
        completionPending
            && expectedGeneration == currentGeneration
            && CanCompleteScan(quality);
}