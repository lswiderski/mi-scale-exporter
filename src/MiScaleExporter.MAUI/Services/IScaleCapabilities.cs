using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    public interface IScaleCapabilities
    {
        IReadOnlyList<MetricKey> GetSupported(ScaleType type);
        bool Supports(ScaleType type, MetricKey key);
    }
}
