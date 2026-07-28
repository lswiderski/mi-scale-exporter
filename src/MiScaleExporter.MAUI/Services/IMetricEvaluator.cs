using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    public interface IMetricEvaluator
    {
        MetricStatusLevel Evaluate(MetricKey key, double value, User user);
    }
}
