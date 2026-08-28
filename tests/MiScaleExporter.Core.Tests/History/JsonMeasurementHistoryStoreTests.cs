using MiScaleExporter.Core.History;
using MiScaleExporter.Core.S400;
using System.Text.Json;

namespace MiScaleExporter.Core.Tests.History;

public sealed class JsonMeasurementHistoryStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"mi-scale-{Guid.NewGuid():N}");

    [Fact]
    public async Task UpsertAsync_SameMeasurementId_ReplacesRecord()
    {
        var store = CreateStore();
        await store.UpsertAsync(Record("one", 70));

        await store.UpsertAsync(Record("one", 71));
        var records = await store.GetAllAsync();

        var record = Assert.Single(records);
        Assert.Equal(71, record.WeightKg);
    }

    [Fact]
    public async Task UpsertAsync_WeightOnlyRecord_RoundTrips()
    {
        var store = CreateStore();
        var weightOnly = Record("one", 70) with
        {
            Quality = S400MeasurementQuality.WeightOnly,
            HeartRateBpm = null,
            Impedance50KhzOhm = null,
            Impedance250KhzOhm = null,
            AlgorithmVersion = string.Empty,
        };

        await store.UpsertAsync(weightOnly);
        var record = Assert.Single(await store.GetAllAsync());

        Assert.Equal(S400MeasurementQuality.WeightOnly, record.Quality);
        Assert.Null(record.Impedance50KhzOhm);
        Assert.Null(record.Impedance250KhzOhm);
    }

    [Fact]
    public async Task UpsertAsync_AppliesRecordAndAgeRetention()
    {
        var store = CreateStore(maxRecords: 2, maxAge: TimeSpan.FromDays(10));
        await store.UpsertAsync(Record("old", 68, DateTimeOffset.Parse("2026-07-01T06:00:00Z")));
        await store.UpsertAsync(Record("one", 69, DateTimeOffset.Parse("2026-08-20T06:00:00Z")));
        await store.UpsertAsync(Record("two", 70, DateTimeOffset.Parse("2026-08-21T06:00:00Z")));
        await store.UpsertAsync(Record("three", 71, DateTimeOffset.Parse("2026-08-22T06:00:00Z")));

        var records = await store.GetAllAsync();

        Assert.Equal(["three", "two"], records.Select(record => record.MeasurementId));
    }

    [Fact]
    public async Task MarkUploadAsync_PersistsStateWithoutCredentials()
    {
        var store = CreateStore();
        await store.UpsertAsync(Record("one", 70));

        await store.MarkUploadAsync("one", MeasurementUploadState.Succeeded, null);
        var record = Assert.Single(await store.GetAllAsync());
        var json = await File.ReadAllTextAsync(Path.Combine(_directory, "history.json"));

        Assert.Equal(MeasurementUploadState.Succeeded, record.UploadState);
        Assert.NotNull(record.LastUploadAttempt);
        Assert.DoesNotContain("BindKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AccessToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllAsync_CorruptFile_QuarantinesAndReturnsEmpty()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(Path.Combine(_directory, "history.json"), "{broken");
        var store = CreateStore();

        var records = await store.GetAllAsync();

        Assert.Empty(records);
        Assert.Single(Directory.GetFiles(_directory, "history.json.corrupt-*"));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"schemaVersion\":1,\"records\":null}")]
    public async Task GetAllAsync_InvalidDocumentStructure_QuarantinesAndReturnsEmpty(string json)
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(Path.Combine(_directory, "history.json"), json);
        var store = CreateStore();

        var records = await store.GetAllAsync();

        Assert.Empty(records);
        Assert.False(File.Exists(Path.Combine(_directory, "history.json")));
        Assert.Single(Directory.GetFiles(_directory, "history.json.corrupt-*"));
    }

    public static TheoryData<string, string> InvalidRecordDocuments => new()
    {
        { "null record", SerializeDocument((MeasurementHistoryRecord)null!) },
        { "blank ID", SerializeDocument(Record("", 70)) },
        { "default timestamp", SerializeDocument(Record("one", 70, DateTimeOffset.MinValue)) },
        { "nonpositive weight", SerializeDocument(Record("one", 0)) },
        { "invalid quality", SerializeDocument(Record("one", 70) with { Quality = (S400MeasurementQuality)99 }) },
        { "incomplete quality", SerializeDocument(Record("one", 70) with { Quality = S400MeasurementQuality.Incomplete }) },
        { "missing final impedance", SerializeDocument(Record("one", 70) with { Impedance250KhzOhm = null }) },
        { "invalid upload state", SerializeDocument(Record("one", 70) with { UploadState = (MeasurementUploadState)99 }) },
        { "missing raw packets", SerializeDocument(Record("one", 70) with { RawAdvertisementsHex = null! }) },
    };

    [Theory]
    [MemberData(nameof(InvalidRecordDocuments))]
    public async Task GetAllAsync_InvalidRecord_QuarantinesAndReturnsEmpty(
        string _,
        string json)
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(Path.Combine(_directory, "history.json"), json);
        var store = CreateStore();

        var records = await store.GetAllAsync();

        Assert.Empty(records);
        Assert.False(File.Exists(Path.Combine(_directory, "history.json")));
        Assert.Single(Directory.GetFiles(_directory, "history.json.corrupt-*"));
    }

    [Fact]
    public async Task UpsertAsync_ExistingSuccess_DoesNotRegressToPending()
    {
        var store = CreateStore();
        await store.UpsertAsync(Record("one", 70));
        await store.MarkUploadAsync("one", MeasurementUploadState.Succeeded, null);

        await store.UpsertAsync(Record("one", 71));
        var record = Assert.Single(await store.GetAllAsync());

        Assert.Equal(71, record.WeightKg);
        Assert.Equal(MeasurementUploadState.Succeeded, record.UploadState);
        Assert.NotNull(record.LastUploadAttempt);
    }

    [Fact]
    public async Task UpsertAsync_ExistingUploading_DoesNotRegressToPending()
    {
        var store = CreateStore();
        await store.UpsertAsync(Record("one", 70));
        await store.MarkUploadAsync("one", MeasurementUploadState.Uploading, null);

        await store.UpsertAsync(Record("one", 71));
        var record = Assert.Single(await store.GetAllAsync());

        Assert.Equal(MeasurementUploadState.Uploading, record.UploadState);
        Assert.NotNull(record.LastUploadAttempt);
    }

    [Fact]
    public async Task GetAllAsync_UnsupportedSchema_QuarantinesOriginal()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(
            Path.Combine(_directory, "history.json"),
            "{\"schemaVersion\":99,\"records\":[]}");
        var store = CreateStore();

        var records = await store.GetAllAsync();

        Assert.Empty(records);
        Assert.False(File.Exists(Path.Combine(_directory, "history.json")));
        Assert.Single(Directory.GetFiles(_directory, "history.json.unsupported-v99-*"));
    }

    [Fact]
    public async Task MarkUploadAsync_MissingMeasurement_Throws()
    {
        var store = CreateStore();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            store.MarkUploadAsync("missing", MeasurementUploadState.Succeeded, null));
    }

    [Fact]
    public async Task TryBeginUploadAsync_ConcurrentCalls_StartsExactlyOnce()
    {
        var store = CreateStore();
        await store.UpsertAsync(Record("one", 70));

        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => store.TryBeginUploadAsync("one")));

        Assert.Single(results, result => result == UploadBeginResult.Started);
        Assert.Equal(19, results.Count(result => result == UploadBeginResult.AlreadyUploading));
    }

    [Fact]
    public async Task TryBeginUploadAsync_SucceededRecord_ReturnsAlreadySucceeded()
    {
        var store = CreateStore();
        await store.UpsertAsync(Record("one", 70));
        await store.MarkUploadAsync("one", MeasurementUploadState.Succeeded, null);

        var result = await store.TryBeginUploadAsync("one");

        Assert.Equal(UploadBeginResult.AlreadySucceeded, result);
    }

    [Fact]
    public async Task TryBeginUploadAsync_ExpiredUploadingRecord_StartsNewLease()
    {
        var timeProvider = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-08-22T12:00:00Z"));
        var store = CreateStore(timeProvider: timeProvider);
        await store.UpsertAsync(Record("one", 70));
        Assert.Equal(UploadBeginResult.Started, await store.TryBeginUploadAsync("one"));

        timeProvider.Advance(TimeSpan.FromMinutes(16));
        var result = await store.TryBeginUploadAsync("one");

        Assert.Equal(UploadBeginResult.Started, result);
    }

    [Fact]
    public async Task ClearAsync_RemovesActiveAndQuarantinedFiles()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(Path.Combine(_directory, "history.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(_directory, "history.json.corrupt-old"), "secret");
        await File.WriteAllTextAsync(Path.Combine(_directory, "history.json.unsupported-v99-old"), "secret");
        var store = CreateStore();

        await store.ClearAsync();

        Assert.Empty(Directory.GetFiles(_directory, "history.json*"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    private JsonMeasurementHistoryStore CreateStore(
        int maxRecords = 400,
        TimeSpan? maxAge = null,
        TimeProvider? timeProvider = null) =>
        new(
            Path.Combine(_directory, "history.json"),
            maxRecords,
            maxAge ?? TimeSpan.FromDays(400),
            timeProvider ?? new FixedTimeProvider(DateTimeOffset.Parse("2026-08-22T12:00:00Z")));

    private static MeasurementHistoryRecord Record(
        string id,
        double weight,
        DateTimeOffset? measuredAt = null) =>
        new(
            id,
            measuredAt ?? DateTimeOffset.Parse("2026-08-22T06:00:00Z"),
            S400MeasurementQuality.DualFrequencyComplete,
            1,
            weight,
            60,
            540,
            495,
            ["aabb"],
            "legacy-50khz-v1",
            "identity",
            null,
            MeasurementUploadState.Pending,
            null,
            null);

    private static string SerializeDocument(MeasurementHistoryRecord record) =>
        JsonSerializer.Serialize(
            new { SchemaVersion = 1, Records = new[] { record } },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public void Advance(TimeSpan duration) => now += duration;
    }
}
