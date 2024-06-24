namespace MiScaleExporter.Models;

    public class GarminFitFileCreationResult
    {
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public byte[] file { get; set; }
}

