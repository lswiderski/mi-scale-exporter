namespace MiScaleExporter.Core.S400;

public sealed class S400DecodeException : Exception
{
    public S400DecodeException(string message)
        : base(message)
    {
    }

    public S400DecodeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}