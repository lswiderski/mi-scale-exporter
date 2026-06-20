namespace MiScaleExporter.Models
{
    public enum ScanPhase
    {
        Idle,
        Searching,
        ScaleFound,
        Reading,
        Stabilizing,
        Success,
        Failed,
        Cancelled,
    }
}
