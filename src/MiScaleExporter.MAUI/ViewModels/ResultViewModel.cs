using System.Collections.ObjectModel;
using System.Globalization;
using MiScaleExporter.MAUI.Resources.Localization;
using MiScaleExporter.Models;
using MiScaleExporter.Services;

namespace MiScaleExporter.MAUI.ViewModels
{
    public class ResultViewModel : BaseViewModel, IResultViewModel
    {
        private const double KgToLb = 2.2046226218;

        private readonly IMetricPresenter _presenter;
        private readonly IMetricEvaluator _evaluator;

        public ResultViewModel(IMetricPresenter presenter, IMetricEvaluator evaluator)
        {
            _presenter = presenter;
            _evaluator = evaluator;
            Title = "Result";
        }

        public ObservableCollection<MetricCardModel> Metrics { get; } = new();

        private string _weightDisplay = string.Empty;
        public string WeightDisplay
        {
            get => _weightDisplay;
            private set => SetProperty(ref _weightDisplay, value);
        }

        private string _weightUnit = "kg";
        public string WeightUnit
        {
            get => _weightUnit;
            private set => SetProperty(ref _weightUnit, value);
        }

        private string _weightStatusLabel = string.Empty;
        public string WeightStatusLabel
        {
            get => _weightStatusLabel;
            private set => SetProperty(ref _weightStatusLabel, value);
        }

        private MetricStatusLevel _weightStatus = MetricStatusLevel.Unknown;
        public MetricStatusLevel WeightStatus
        {
            get => _weightStatus;
            private set => SetProperty(ref _weightStatus, value);
        }

        private string _scaleTypeName = string.Empty;
        public string ScaleTypeName
        {
            get => _scaleTypeName;
            private set => SetProperty(ref _scaleTypeName, value);
        }

        private string _measuredAtDisplay = string.Empty;
        public string MeasuredAtDisplay
        {
            get => _measuredAtDisplay;
            private set => SetProperty(ref _measuredAtDisplay, value);
        }

        private bool _hasImpedance;
        public bool HasImpedance
        {
            get => _hasImpedance;
            private set => SetProperty(ref _hasImpedance, value);
        }

        private string _subtitleDisplay = string.Empty;
        public string SubtitleDisplay
        {
            get => _subtitleDisplay;
            private set => SetProperty(ref _subtitleDisplay, value);
        }

        private string _weightDeltaArrow = string.Empty;
        public string WeightDeltaArrow
        {
            get => _weightDeltaArrow;
            private set => SetProperty(ref _weightDeltaArrow, value);
        }

        private string _weightDeltaFormatted = string.Empty;
        public string WeightDeltaFormatted
        {
            get => _weightDeltaFormatted;
            private set => SetProperty(ref _weightDeltaFormatted, value);
        }

        private bool _hasWeightDelta;
        public bool HasWeightDelta
        {
            get => _hasWeightDelta;
            private set => SetProperty(ref _hasWeightDelta, value);
        }

        private bool _hasComposition;
        public bool HasComposition
        {
            get => _hasComposition;
            private set => SetProperty(ref _hasComposition, value);
        }

        private bool _hasOther;
        public bool HasOther
        {
            get => _hasOther;
            private set => SetProperty(ref _hasOther, value);
        }

        private double _compFat;
        public double CompFat
        {
            get => _compFat;
            private set => SetProperty(ref _compFat, value);
        }

        private double _compMuscle;
        public double CompMuscle
        {
            get => _compMuscle;
            private set => SetProperty(ref _compMuscle, value);
        }

        private double _compBone;
        public double CompBone
        {
            get => _compBone;
            private set => SetProperty(ref _compBone, value);
        }

        private double _compOther;
        public double CompOther
        {
            get => _compOther;
            private set => SetProperty(ref _compOther, value);
        }

        private string _compFatDisplay = string.Empty;
        public string CompFatDisplay
        {
            get => _compFatDisplay;
            private set => SetProperty(ref _compFatDisplay, value);
        }

        private string _compMuscleDisplay = string.Empty;
        public string CompMuscleDisplay
        {
            get => _compMuscleDisplay;
            private set => SetProperty(ref _compMuscleDisplay, value);
        }

        private string _compBoneDisplay = string.Empty;
        public string CompBoneDisplay
        {
            get => _compBoneDisplay;
            private set => SetProperty(ref _compBoneDisplay, value);
        }

        private string _compOtherDisplay = string.Empty;
        public string CompOtherDisplay
        {
            get => _compOtherDisplay;
            private set => SetProperty(ref _compOtherDisplay, value);
        }

        public void Load(BodyComposition composition, ScaleType scaleType, User user, BodyComposition previous = null)
        {
            void Apply()
            {
                Metrics.Clear();
                WeightDeltaArrow = string.Empty;
                WeightDeltaFormatted = string.Empty;
                HasWeightDelta = false;
                HasComposition = false;
                HasOther = false;
                CompFat = CompMuscle = CompBone = CompOther = 0;
                CompFatDisplay = CompMuscleDisplay = CompBoneDisplay = CompOtherDisplay = string.Empty;
                if (composition == null)
                {
                    WeightDisplay = "—";
                    SubtitleDisplay = "No measurement";
                    return;
                }

                // BMI status drives the weight banner color (BMI is universally available
                // and unit-independent — evaluate on the raw value either way).
                var bmiStatus = _evaluator.Evaluate(MetricKey.BMI, composition.BMI, user);
                var useLbs = Preferences.Get(PreferencesKeys.DisplayWeightInLbs, false);
                var weightForDisplay = useLbs ? composition.Weight * KgToLb : composition.Weight;
                WeightDisplay = weightForDisplay.ToString("F1", CultureInfo.InvariantCulture);
                WeightUnit = useLbs ? "lb" : "kg";
                WeightStatus = bmiStatus;
                WeightStatusLabel = bmiStatus switch
                {
                    MetricStatusLevel.Low => AppSnippets.BmiUnderweight,
                    MetricStatusLevel.Standard => AppSnippets.BmiHealthy,
                    MetricStatusLevel.High => AppSnippets.BmiOverweight,
                    _ => string.Empty,
                };

                ScaleTypeName = ScaleTypeDisplay(scaleType);
                MeasuredAtDisplay = composition.Date == default
                    ? string.Empty
                    : composition.Date.ToString("g", CultureInfo.CurrentCulture);
                HasImpedance = composition.HasImpedance;
                // MiSmartScale has no electrodes at all, so the "stand barefoot" hint is misleading
                // for that model. Only append the hint when impedance was expected but missing
                // (i.e. on capability-bearing scales like MiBodyCompositionScale / S400).
                SubtitleDisplay = composition.HasImpedance || scaleType == ScaleType.MiSmartScale
                    ? ScaleTypeName
                    : $"{ScaleTypeName} · {AppSnippets.WeightOnlyHint}";

                HasComposition = composition.HasImpedance && composition.Weight > 0 && composition.Fat > 0 && composition.MuscleMass > 0;
                if (HasComposition)
                {
                    var fatMassKg = composition.Weight * composition.Fat / 100.0;
                    var muscleKg = composition.MuscleMass;
                    var boneKg = composition.BoneMass;
                    var otherKg = Math.Max(0, composition.Weight - fatMassKg - muscleKg - boneKg);

                    var factor = useLbs ? KgToLb : 1.0;
                    CompFat = fatMassKg * factor;
                    CompMuscle = muscleKg * factor;
                    CompBone = boneKg * factor;
                    CompOther = otherKg * factor;
                    // Xiaomi MuscleMass includes body water, so Fat+Muscle+Bone can exceed
                    // Weight and otherKg clamps to 0. When that happens omit the Other segment
                    // (donut skips value<=0) and hide its legend row.
                    HasOther = CompOther > 0.0001;

                    var unit = WeightUnit;
                    CompFatDisplay = $"{AppSnippets.CompFat} {CompFat.ToString("F1", CultureInfo.InvariantCulture)} {unit}";
                    CompMuscleDisplay = $"{AppSnippets.CompMuscle} {CompMuscle.ToString("F1", CultureInfo.InvariantCulture)} {unit}";
                    CompBoneDisplay = $"{AppSnippets.CompBone} {CompBone.ToString("F1", CultureInfo.InvariantCulture)} {unit}";
                    CompOtherDisplay = $"{AppSnippets.CompOther} {CompOther.ToString("F1", CultureInfo.InvariantCulture)} {unit}";
                }

                foreach (var card in _presenter.Build(composition, scaleType, user, previous))
                {
                    // Skip Weight here — it is already shown as the big header.
                    if (card.Key == MetricKey.Weight)
                    {
                        // Capture the Weight delta so the big header can render an arrow.
                        if (card.HasDelta)
                        {
                            WeightDeltaArrow = card.DeltaArrow;
                            WeightDeltaFormatted = card.DeltaFormatted;
                            HasWeightDelta = true;
                        }
                        continue;
                    }
                    Metrics.Add(card);
                }
            }

            if (MainThread.IsMainThread) Apply();
            else MainThread.BeginInvokeOnMainThread(Apply);
        }

        private static string ScaleTypeDisplay(ScaleType t) => t switch
        {
            ScaleType.MiSmartScale => "Mi Smart Scale",
            ScaleType.MiBodyCompositionScale => "Mi Body Composition Scale",
            ScaleType.S400 => "Mi Body Composition Scale S400",
            _ => t.ToString(),
        };
    }
}
