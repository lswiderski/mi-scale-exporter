namespace MiScaleExporter.Models;

public class HealthConnectUploadResult
{
    public bool IsSuccess { get; set; }
    public bool IsUnavailable { get; set; }
    public string Message { get; set; }

    public static HealthConnectUploadResult Success() => new() { IsSuccess = true };

    public static HealthConnectUploadResult Unavailable() => new()
    {
        IsSuccess = false,
        IsUnavailable = true,
        Message = "Health Connect is not available on this device."
    };

    public static HealthConnectUploadResult Failure(string message) => new()
    {
        IsSuccess = false,
        Message = message
    };
}
