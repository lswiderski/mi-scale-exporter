using System.Globalization;

namespace MiScaleExporter.Core.S400;

public sealed class S400MeasurementAccumulator
{
    private readonly Dictionary<(byte ProfileId, uint UnixTimestamp), PendingMeasurement> _pending = [];
    private readonly Dictionary<byte, uint> _latestTimestampByProfile = [];
    private readonly HashSet<(byte ProfileId, uint UnixTimestamp)> _completed = [];
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _partialLifetime;

    public S400MeasurementAccumulator(
        TimeProvider? timeProvider = null,
        TimeSpan? partialLifetime = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _partialLifetime = partialLifetime ?? TimeSpan.FromSeconds(15);
        if (_partialLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(partialLifetime));
        }
    }

    public S400MeasurementUpdate Add(S400Packet packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (packet.Kind == S400PacketKind.Reset)
        {
            Reset();
            return new S400MeasurementUpdate(null, null, null);
        }

        ExpirePartialMeasurements();
        var key = (packet.ProfileId, packet.UnixTimestamp);
        if (_completed.Contains(key))
        {
            return new S400MeasurementUpdate(null, null, null);
        }

        if (_latestTimestampByProfile.TryGetValue(packet.ProfileId, out var latestTimestamp))
        {
            if (packet.UnixTimestamp < latestTimestamp)
            {
                return new S400MeasurementUpdate(null, null, null);
            }
            if (packet.UnixTimestamp > latestTimestamp)
            {
                foreach (var staleKey in _pending.Keys
                             .Where(value => value.ProfileId == packet.ProfileId)
                             .ToArray())
                {
                    _pending.Remove(staleKey);
                }
                _latestTimestampByProfile[packet.ProfileId] = packet.UnixTimestamp;
            }
        }
        else
        {
            _latestTimestampByProfile.Add(packet.ProfileId, packet.UnixTimestamp);
        }

        if (!_pending.TryGetValue(key, out var pending))
        {
            pending = new PendingMeasurement(
                packet.ProfileId,
                packet.UnixTimestamp,
                _timeProvider.GetUtcNow());
            _pending.Add(key, pending);
        }

        pending.LastReceivedAt = _timeProvider.GetUtcNow();
        pending.AddRaw(packet.RawAdvertisement);
        pending.WeightKg ??= packet.WeightKg;
        pending.HeartRateBpm ??= packet.HeartRateBpm;
        pending.Impedance50KhzOhm ??= packet.Impedance50KhzOhm;
        pending.Impedance250KhzOhm ??= packet.Impedance250KhzOhm;

        if (packet.Kind == S400PacketKind.WeightOnly && pending.WeightKg.HasValue)
        {
            var measurement = CreateMeasurement(pending, S400MeasurementQuality.WeightOnly);
            _pending.Remove(key);
            _completed.Add(key);
            return new S400MeasurementUpdate(measurement.WeightKg, null, measurement);
        }

        if (pending.WeightKg.HasValue
            && pending.Impedance50KhzOhm.HasValue
            && pending.Impedance250KhzOhm.HasValue)
        {
            var measurement = CreateMeasurement(pending, S400MeasurementQuality.DualFrequencyComplete);
            _pending.Remove(key);
            _completed.Add(key);
            return new S400MeasurementUpdate(measurement.WeightKg, null, measurement);
        }

        var partialMeasurement = pending.WeightKg.HasValue
            ? CreateMeasurement(pending, S400MeasurementQuality.Incomplete)
            : null;
        return new S400MeasurementUpdate(pending.WeightKg, partialMeasurement, null);
    }

    public void Reset()
    {
        _pending.Clear();
        _latestTimestampByProfile.Clear();
        _completed.Clear();
    }

    private void ExpirePartialMeasurements()
    {
        var cutoff = _timeProvider.GetUtcNow() - _partialLifetime;
        foreach (var key in _pending
                     .Where(entry => entry.Value.LastReceivedAt < cutoff)
                     .Select(entry => entry.Key)
                     .ToArray())
        {
            _pending.Remove(key);
            _completed.Add(key);
        }
    }

    private static S400Measurement CreateMeasurement(
        PendingMeasurement pending,
        S400MeasurementQuality quality)
    {
        var weightKg = pending.WeightKg!.Value;
        var weightDecigrams = Math.Round(weightKg * 10).ToString("0", CultureInfo.InvariantCulture);
        var measurementId = $"s400-{pending.ProfileId:x2}-{pending.UnixTimestamp}-{weightDecigrams}";

        return new S400Measurement(
            measurementId,
            pending.ProfileId,
            pending.UnixTimestamp,
            DateTimeOffset.FromUnixTimeSeconds(pending.UnixTimestamp),
            weightKg,
            pending.HeartRateBpm,
            pending.Impedance50KhzOhm,
            pending.Impedance250KhzOhm,
            quality,
            pending.RawAdvertisements.Select(value => value.ToArray()).ToArray());
    }

    private sealed class PendingMeasurement(
        byte profileId,
        uint unixTimestamp,
        DateTimeOffset lastReceivedAt)
    {
        public byte ProfileId { get; } = profileId;
        public uint UnixTimestamp { get; } = unixTimestamp;
        public double? WeightKg { get; set; }
        public int? HeartRateBpm { get; set; }
        public double? Impedance50KhzOhm { get; set; }
        public double? Impedance250KhzOhm { get; set; }
        public List<byte[]> RawAdvertisements { get; } = [];
        public DateTimeOffset LastReceivedAt { get; set; } = lastReceivedAt;

        public void AddRaw(byte[] advertisement)
        {
            if (!RawAdvertisements.Any(existing => existing.AsSpan().SequenceEqual(advertisement)))
            {
                RawAdvertisements.Add(advertisement.ToArray());
            }
        }
    }
}