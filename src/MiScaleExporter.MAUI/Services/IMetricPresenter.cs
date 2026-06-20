using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    public interface IMetricPresenter
    {
        IEnumerable<MetricCardModel> Build(
            BodyComposition composition,
            ScaleType scaleType,
            User user,
            BodyComposition previous = null);
    }
}
