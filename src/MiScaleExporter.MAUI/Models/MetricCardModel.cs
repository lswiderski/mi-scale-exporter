namespace MiScaleExporter.Models
{
    public class MetricCardModel
    {
        public MetricKey Key { get; init; }
        public string Label { get; init; } = string.Empty;
        public string FormattedValue { get; init; } = string.Empty;
        public string Unit { get; init; } = string.Empty;
        public MetricStatusLevel Status { get; init; } = MetricStatusLevel.Unknown;
        public string StatusLabel { get; init; } = string.Empty;
        public double? Delta { get; init; }
        public string DeltaFormatted { get; init; } = string.Empty;
        public string DeltaArrow { get; init; } = string.Empty;
        public bool HasDelta => Delta.HasValue;
        public bool HasStatus => Status != MetricStatusLevel.Unknown;
    }
}
