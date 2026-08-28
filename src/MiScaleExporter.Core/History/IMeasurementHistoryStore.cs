namespace MiScaleExporter.Core.History;

public interface IMeasurementHistoryStore
{
    Task<IReadOnlyList<MeasurementHistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(MeasurementHistoryRecord record, CancellationToken cancellationToken = default);
    Task<UploadBeginResult> TryBeginUploadAsync(
        string measurementId,
        CancellationToken cancellationToken = default);
    Task MarkUploadAsync(
        string measurementId,
        MeasurementUploadState state,
        string? error,
        CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}