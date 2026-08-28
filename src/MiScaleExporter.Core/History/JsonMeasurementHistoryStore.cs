using MiScaleExporter.Core.S400;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiScaleExporter.Core.History;

public sealed class JsonMeasurementHistoryStore : IMeasurementHistoryStore
{
    private const int SchemaVersion = 1;
    private static readonly TimeSpan UploadLeaseDuration = TimeSpan.FromMinutes(15);
    private readonly string _path;
    private readonly int _maxRecords;
    private readonly TimeSpan _maxAge;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public JsonMeasurementHistoryStore(
        string path,
        int maxRecords = 400,
        TimeSpan? maxAge = null,
        TimeProvider? timeProvider = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("History path is required.", nameof(path));
        }
        if (maxRecords <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRecords));
        }

        _path = path;
        _maxRecords = maxRecords;
        _maxAge = maxAge ?? TimeSpan.FromDays(400);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<MeasurementHistoryRecord>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var document = await LoadUnsafeAsync(cancellationToken);
            return document.Records
                .OrderByDescending(record => record.MeasuredAt)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertAsync(
        MeasurementHistoryRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (string.IsNullOrWhiteSpace(record.MeasurementId))
        {
            throw new ArgumentException("Measurement ID is required.", nameof(record));
        }
        if (!IsValidRecord(record))
        {
            throw new ArgumentException("Measurement history record is invalid.", nameof(record));
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var document = await LoadUnsafeAsync(cancellationToken);
            var existingRecord = document.Records.FirstOrDefault(existing =>
                string.Equals(existing.MeasurementId, record.MeasurementId, StringComparison.Ordinal));
            if (existingRecord?.UploadState is MeasurementUploadState.Uploading or MeasurementUploadState.Succeeded
                && record.UploadState != MeasurementUploadState.Succeeded)
            {
                record = record with
                {
                    UploadState = existingRecord.UploadState,
                    LastUploadAttempt = existingRecord.LastUploadAttempt,
                    UploadError = existingRecord.UploadError,
                };
            }
            document.Records.RemoveAll(existing =>
                string.Equals(existing.MeasurementId, record.MeasurementId, StringComparison.Ordinal));
            document.Records.Add(record);
            ApplyRetention(document.Records);
            await SaveUnsafeAsync(document, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MarkUploadAsync(
        string measurementId,
        MeasurementUploadState state,
        string? error,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(measurementId))
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var document = await LoadUnsafeAsync(cancellationToken);
            var index = document.Records.FindIndex(record =>
                string.Equals(record.MeasurementId, measurementId, StringComparison.Ordinal));
            if (index < 0)
            {
                throw new KeyNotFoundException($"Measurement '{measurementId}' was not found in history.");
            }

            document.Records[index] = document.Records[index] with
            {
                UploadState = state,
                LastUploadAttempt = _timeProvider.GetUtcNow(),
                UploadError = string.IsNullOrWhiteSpace(error)
                    ? null
                    : error.Trim()[..Math.Min(error.Trim().Length, 500)],
            };
            await SaveUnsafeAsync(document, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UploadBeginResult> TryBeginUploadAsync(
        string measurementId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(measurementId))
        {
            return UploadBeginResult.NotFound;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var document = await LoadUnsafeAsync(cancellationToken);
            var index = document.Records.FindIndex(record =>
                string.Equals(record.MeasurementId, measurementId, StringComparison.Ordinal));
            if (index < 0)
            {
                return UploadBeginResult.NotFound;
            }

            var record = document.Records[index];
            if (record.UploadState == MeasurementUploadState.Succeeded)
            {
                return UploadBeginResult.AlreadySucceeded;
            }
            var now = _timeProvider.GetUtcNow();
            if (record.UploadState == MeasurementUploadState.Uploading
                && record.LastUploadAttempt is { } uploadStartedAt
                && now - uploadStartedAt < UploadLeaseDuration)
            {
                return UploadBeginResult.AlreadyUploading;
            }

            document.Records[index] = record with
            {
                UploadState = MeasurementUploadState.Uploading,
                LastUploadAttempt = now,
                UploadError = null,
            };
            await SaveUnsafeAsync(document, cancellationToken);
            return UploadBeginResult.Started;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return;
            }

            var fileName = Path.GetFileName(_path);
            foreach (var path in Directory.GetFiles(directory, $"{fileName}*"))
            {
                File.Delete(path);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<HistoryDocument> LoadUnsafeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
        {
            return new HistoryDocument();
        }

        try
        {
            HistoryDocument? document;
            await using (var stream = File.OpenRead(_path))
            {
                document = await JsonSerializer.DeserializeAsync<HistoryDocument>(
                    stream,
                    _serializerOptions,
                    cancellationToken);
            }
            if (document is null)
            {
                QuarantineCorruptFile();
                return new HistoryDocument();
            }
            if (document.SchemaVersion != SchemaVersion)
            {
                QuarantineUnsupportedSchema(document.SchemaVersion);
                return new HistoryDocument();
            }
            if (!IsValidDocument(document))
            {
                QuarantineCorruptFile();
                return new HistoryDocument();
            }
            return document;
        }
        catch (JsonException)
        {
            QuarantineCorruptFile();
            return new HistoryDocument();
        }
    }

    private async Task SaveUnsafeAsync(HistoryDocument document, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{_path}.tmp-{Guid.NewGuid():N}";
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, document, _serializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            File.Move(temporaryPath, _path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private void ApplyRetention(List<MeasurementHistoryRecord> records)
    {
        var cutoff = _timeProvider.GetUtcNow() - _maxAge;
        records.RemoveAll(record => record.MeasuredAt < cutoff);
        var retained = records
            .OrderByDescending(record => record.MeasuredAt)
            .Take(_maxRecords)
            .ToArray();
        records.Clear();
        records.AddRange(retained);
    }

    private static bool IsValidDocument(HistoryDocument document)
    {
        if (document.Records is null)
        {
            return false;
        }

        var measurementIds = new HashSet<string>(StringComparer.Ordinal);
        return document.Records.All(record =>
            IsValidRecord(record)
            && measurementIds.Add(record.MeasurementId));
    }

    private static bool IsValidRecord(MeasurementHistoryRecord? record)
    {
        if (record is null)
        {
            return false;
        }

        var rawAdvertisements = record.RawAdvertisementsHex;
        if (string.IsNullOrWhiteSpace(record.MeasurementId)
            || record.MeasuredAt == default
            || !double.IsFinite(record.WeightKg)
            || record.WeightKg <= 0
            || !Enum.IsDefined(record.Quality)
            || !S400MeasurementCompletionPolicy.CanCompleteScan(record.Quality)
            || !Enum.IsDefined(record.UploadState)
            || rawAdvertisements is null
            || rawAdvertisements.Count == 0
            || rawAdvertisements.Any(value =>
                string.IsNullOrWhiteSpace(value)
                || value.Length % 2 != 0
                || !value.All(Uri.IsHexDigit))
            || record.AlgorithmVersion is null
            || record.CalibrationVersion is null)
        {
            return false;
        }

        return record.Quality switch
        {
            S400MeasurementQuality.WeightOnly =>
                record.Impedance50KhzOhm is null
                && record.Impedance250KhzOhm is null,
            S400MeasurementQuality.DualFrequencyComplete =>
                IsPositiveFinite(record.Impedance50KhzOhm)
                && IsPositiveFinite(record.Impedance250KhzOhm),
            _ => false,
        };
    }

    private static bool IsPositiveFinite(double? value) =>
        value is > 0 && double.IsFinite(value.Value);

    private void QuarantineCorruptFile()
    {
        var suffix = _timeProvider.GetUtcNow().ToString("yyyyMMddHHmmssfff");
        File.Move(_path, $"{_path}.corrupt-{suffix}", true);
    }

    private void QuarantineUnsupportedSchema(int schemaVersion)
    {
        var suffix = _timeProvider.GetUtcNow().ToString("yyyyMMddHHmmssfff");
        File.Move(_path, $"{_path}.unsupported-v{schemaVersion}-{suffix}", true);
    }

    private sealed class HistoryDocument
    {
        [JsonRequired]
        public int SchemaVersion { get; init; } = JsonMeasurementHistoryStore.SchemaVersion;

        [JsonRequired]
        public List<MeasurementHistoryRecord> Records { get; init; } = [];
    }
}