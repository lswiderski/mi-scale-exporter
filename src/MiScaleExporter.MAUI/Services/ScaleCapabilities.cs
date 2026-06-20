using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    public class ScaleCapabilities : IScaleCapabilities
    {
        private static readonly IReadOnlyList<MetricKey> WeightOnly = new[]
        {
            MetricKey.Weight,
            MetricKey.BMI,
        };

        private static readonly IReadOnlyList<MetricKey> FullComposition = new[]
        {
            MetricKey.Weight,
            MetricKey.BMI,
            MetricKey.Fat,
            MetricKey.WaterPercentage,
            MetricKey.MuscleMass,
            MetricKey.BoneMass,
            MetricKey.VisceralFat,
            MetricKey.BMR,
            MetricKey.MetabolicAge,
            MetricKey.ProteinPercentage,
            MetricKey.IdealWeight,
            MetricKey.BodyType,
        };

        public IReadOnlyList<MetricKey> GetSupported(ScaleType type) => type switch
        {
            ScaleType.MiSmartScale => WeightOnly,
            ScaleType.MiBodyCompositionScale => FullComposition,
            ScaleType.S400 => FullComposition,
            _ => WeightOnly,
        };

        public bool Supports(ScaleType type, MetricKey key)
        {
            var list = GetSupported(type);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == key) return true;
            }
            return false;
        }
    }
}
