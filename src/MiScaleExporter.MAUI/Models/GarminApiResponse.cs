namespace MiScaleExporter.Models;

public class GarminApiResponse
{
    public bool IsSuccess { get; set; }
    public bool LocalReceiptSaved { get; set; } = true;
    public string Message { get; set; }
    public bool MFARequested { get; set; }
    public string ExternalApiClientId { get; set; }
    public string AccessToken { get; set; }
    public string TokenSecret { get; set; }
}