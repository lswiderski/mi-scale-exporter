using MiScaleExporter.Core.Calibration;
using MiScaleExporter.Core.Composition;

namespace MiScaleExporter.Core.S400;

public sealed class S400MeasurementPipeline
{
    private readonly S400AdvertisementDecoder _decoder;
    private readonly S400MeasurementAccumulator _accumulator;
    private readonly S400BodyCompositionEstimator _estimator;

    public S400MeasurementPipeline()
        : this(
            new S400AdvertisementDecoder(),
            new S400MeasurementAccumulator(),
            new S400BodyCompositionEstimator())
    {
    }

    public S400MeasurementPipeline(
        S400AdvertisementDecoder decoder,
        S400MeasurementAccumulator accumulator,
        S400BodyCompositionEstimator estimator)
    {
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        _accumulator = accumulator ?? throw new ArgumentNullException(nameof(accumulator));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
    }

    public S400PipelineResult Process(
        byte[] advertisement,
        string bindKey,
        string macAddress,
        BodyProfile profile,
        FatCalibrationFit? calibration = null)
    {
        var packet = _decoder.Decode(advertisement, bindKey, macAddress);
        var update = _accumulator.Add(packet);
        if (update.Completed is null)
        {
            return new S400PipelineResult(
                update.PreviewWeightKg,
                update.PartialMeasurement,
                null,
                null);
        }

        var estimate = update.Completed.Quality == S400MeasurementQuality.DualFrequencyComplete
            ? _estimator.Estimate(update.Completed, profile, calibration)
            : null;
        return new S400PipelineResult(update.PreviewWeightKg, null, update.Completed, estimate);
    }

    public void Reset() => _accumulator.Reset();
}