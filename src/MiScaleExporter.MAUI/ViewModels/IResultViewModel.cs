using System.Collections.ObjectModel;
using MiScaleExporter.Models;

namespace MiScaleExporter.MAUI.ViewModels
{
    public interface IResultViewModel
    {
        ObservableCollection<MetricCardModel> Metrics { get; }
        string WeightDisplay { get; }
        string WeightUnit { get; }
        string WeightStatusLabel { get; }
        MetricStatusLevel WeightStatus { get; }
        string ScaleTypeName { get; }
        string MeasuredAtDisplay { get; }
        bool HasImpedance { get; }
        string SubtitleDisplay { get; }
        string WeightDeltaArrow { get; }
        string WeightDeltaFormatted { get; }
        bool HasWeightDelta { get; }

        void Load(BodyComposition composition, ScaleType scaleType, User user, BodyComposition previous = null);
    }
}
