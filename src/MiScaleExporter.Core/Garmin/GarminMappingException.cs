namespace MiScaleExporter.Core.Garmin;

public sealed class GarminMappingException : Exception
{
    public GarminMappingException(string message)
        : base(message)
    {
    }
}