using MiScaleExporter.Core.S400;

namespace MiScaleExporter.Core.Tests.S400;

public sealed class S400MeasurementAccumulatorTests
{
    private const string BindKey = "0728974d657a4b60964c1b1677f35f7c";
    private const string MacAddress = "8C:D0:B2:F6:BE:EF";
    private readonly S400AdvertisementDecoder _decoder = new();

    [Fact]
    public void Add_LowThenHigh_CompletesDualFrequencyMeasurement()
    {
        var accumulator = new S400MeasurementAccumulator();
        var low = Decode("4859D53B0ABC078FF2348C844138E930220000009E538599");
        var high = Decode("4859D53B0BD6EF0B25DB72785E7E2F46D6000000D8642DF6");

        var preview = accumulator.Add(low);
        var completed = accumulator.Add(high);

        Assert.Equal(69.9, preview.PreviewWeightKg);
        Assert.Null(preview.Completed);
        Assert.NotNull(completed.Completed);
        Assert.Equal(S400MeasurementQuality.DualFrequencyComplete, completed.Completed!.Quality);
        Assert.Equal(69.9, completed.Completed.WeightKg);
        Assert.Equal(543.2, completed.Completed.Impedance50KhzOhm);
        Assert.Equal(497.6, completed.Completed.Impedance250KhzOhm);
        Assert.Equal(92, completed.Completed.HeartRateBpm);
        Assert.Equal(2, completed.Completed.RawAdvertisements.Count);
    }

    [Fact]
    public void Add_HighThenLow_CompletesSameMeasurement()
    {
        var accumulator = new S400MeasurementAccumulator();
        var low = Decode("4859D53B0ABC078FF2348C844138E930220000009E538599");
        var high = Decode("4859D53B0BD6EF0B25DB72785E7E2F46D6000000D8642DF6");

        var pending = accumulator.Add(high);
        var completed = accumulator.Add(low);

        Assert.Null(pending.PreviewWeightKg);
        Assert.Null(pending.Completed);
        Assert.Equal(S400MeasurementQuality.DualFrequencyComplete, completed.Completed!.Quality);
    }

    [Fact]
    public void Add_DuplicatePacket_KeepsOneRawCopy()
    {
        var accumulator = new S400MeasurementAccumulator();
        var low = Decode("4859D53B0ABC078FF2348C844138E930220000009E538599");
        var high = Decode("4859D53B0BD6EF0B25DB72785E7E2F46D6000000D8642DF6");

        accumulator.Add(low);
        accumulator.Add(low);
        var completed = accumulator.Add(high).Completed;

        Assert.NotNull(completed);
        Assert.Equal(2, completed!.RawAdvertisements.Count);
    }

    [Fact]
    public void Add_DifferentTimestamp_DoesNotCombinePackets()
    {
        var accumulator = new S400MeasurementAccumulator();
        var low = Decode("4859D53B0ABC078FF2348C844138E930220000009E538599");
        var high = Decode("4859D53B0BD6EF0B25DB72785E7E2F46D6000000D8642DF6") with
        {
            UnixTimestamp = low.UnixTimestamp + 1,
        };

        accumulator.Add(low);
        var result = accumulator.Add(high);

        Assert.Null(result.Completed);
    }

    [Fact]
    public void Add_ResetClearsPendingMeasurement()
    {
        var accumulator = new S400MeasurementAccumulator();
        var low = Decode("4859D53B0ABC078FF2348C844138E930220000009E538599");
        var high = Decode("4859D53B0BD6EF0B25DB72785E7E2F46D6000000D8642DF6");
        var reset = low with
        {
            Kind = S400PacketKind.Reset,
            WeightKg = null,
            HeartRateBpm = null,
            Impedance50KhzOhm = null,
            IsFinal = false,
        };

        accumulator.Add(low);
        accumulator.Add(reset);
        var result = accumulator.Add(high);

        Assert.Null(result.Completed);
        Assert.Null(result.PreviewWeightKg);
    }

    [Fact]
    public void Add_WeightOnlyPacket_CompletesWithoutImpedance()
    {
        var accumulator = new S400MeasurementAccumulator();
        var packet = _decoder.Decode(
            Convert.FromHexString("4859D53B71530438B5894B242C209908DA000000479ECDA3"),
            "02d2900363ef629c736a4549677acbee",
            "04:AE:47:67:C6:7C");

        var completed = accumulator.Add(packet).Completed;

        Assert.NotNull(completed);
        Assert.Equal(S400MeasurementQuality.WeightOnly, completed!.Quality);
        Assert.Equal(74.7, completed.WeightKg);
        Assert.Null(completed.Impedance50KhzOhm);
        Assert.Null(completed.Impedance250KhzOhm);
        Assert.Single(completed.RawAdvertisements);
    }

    [Fact]
    public void Add_ReplayedCompletePair_DoesNotCompleteTwice()
    {
        var accumulator = new S400MeasurementAccumulator();
        var low = Decode("4859D53B0ABC078FF2348C844138E930220000009E538599");
        var high = Decode("4859D53B0BD6EF0B25DB72785E7E2F46D6000000D8642DF6");

        Assert.NotNull(accumulator.Add(low).Completed ?? accumulator.Add(high).Completed);
        var replayLow = accumulator.Add(low);
        var replayHigh = accumulator.Add(high);

        Assert.Null(replayLow.Completed);
        Assert.Null(replayHigh.Completed);
    }

    [Fact]
    public void Add_NewerTimestamp_DiscardsOlderPartialMeasurement()
    {
        var accumulator = new S400MeasurementAccumulator();
        var low = Decode("4859D53B0ABC078FF2348C844138E930220000009E538599");
        var newerLow = low with { UnixTimestamp = low.UnixTimestamp + 1 };
        var delayedOldHigh = Decode("4859D53B0BD6EF0B25DB72785E7E2F46D6000000D8642DF6");

        accumulator.Add(low);
        accumulator.Add(newerLow);
        var result = accumulator.Add(delayedOldHigh);

        Assert.Null(result.Completed);
        Assert.Null(result.PreviewWeightKg);
    }

    [Fact]
    public void Add_ExpiredPartialMeasurement_DoesNotCombine()
    {
        var time = new ManualTimeProvider(DateTimeOffset.Parse("2026-08-22T06:00:00Z"));
        var accumulator = new S400MeasurementAccumulator(time, TimeSpan.FromSeconds(15));
        var low = Decode("4859D53B0ABC078FF2348C844138E930220000009E538599");
        var high = Decode("4859D53B0BD6EF0B25DB72785E7E2F46D6000000D8642DF6");

        accumulator.Add(low);
        time.Advance(TimeSpan.FromSeconds(16));
        var expiredHigh = accumulator.Add(high);
        var replayedLow = accumulator.Add(low);

        Assert.Null(expiredHigh.Completed);
        Assert.Null(expiredHigh.PreviewWeightKg);
        Assert.Null(replayedLow.Completed);
        Assert.Null(replayedLow.PreviewWeightKg);
    }

    private S400Packet Decode(string value) =>
        _decoder.Decode(Convert.FromHexString(value), BindKey, MacAddress);

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;
    }
}
