using MiScaleExporter.Core.Composition;
using MiScaleExporter.Core.S400;
using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    public class DataInterpreter : IDataInterpreter
    {
        private readonly S400MeasurementPipeline _s400Pipeline = new();

        public void ResetSession() => _s400Pipeline.Reset();

        private void ValidateAesKey(string aesKey)
        {
            if (string.IsNullOrEmpty(aesKey) || aesKey.Length != 32)
            {
                throw new ArgumentException("AES key must be a 32-character hexadecimal string.");
            }
        }
        private void ValidateBluetoothAddress(string btAddress)
        {
            if (string.IsNullOrEmpty(btAddress) || btAddress.Length != 17 || !btAddress.All(c => char.IsLetterOrDigit(c) || c == ':'))
            {
                throw new ArgumentException("Bluetooth address must be a valid 17-character string in the format XX:XX:XX:XX:XX:XX.");
            }
        }

        public BodyComposition ComputeData(byte[] data, User _user, string btAddress)
        {
            if (data == null) return null;

            var user = new MiScaleBodyComposition.User(_user.Height, _user.Age, (MiScaleBodyComposition.Sex)(byte)_user.Sex);
            switch (_user.ScaleType)
            {
                case ScaleType.MiBodyCompositionScale:

                    var miBodyCompositionScale = new MiScaleBodyComposition.MiScale();

                    var isStabilized = miBodyCompositionScale.Istabilized(data, user);
                    var hasImpedance = miBodyCompositionScale.HasImpedance(data, user);

                    if (data.Length > 13)
                    {
                        data = data.Skip(data.Length - 13).ToArray();
                    }

                    if (isStabilized)
                    {
                        var bc = miBodyCompositionScale.GetBodyComposition(data, user, true);
                        var bodyComposition = new BodyComposition
                        {
                            Weight = bc.Weight,
                            BMI = bc.BMI,
                            ProteinPercentage = bc.ProteinPercentage,
                            IdealWeight = bc.IdealWeight,
                            BMR = bc.BMR,
                            BoneMass = bc.BoneMass,
                            Fat = bc.Fat,
                            MetabolicAge = bc.MetabolicAge,
                            MuscleMass = bc.MuscleMass,
                            VisceralFat = bc.VisceralFat,
                            WaterPercentage = bc.Water,
                            BodyType = bc.BodyType,
                            HasImpedance = hasImpedance,
                            IsStabilized = isStabilized,
                            Date = bc.Date,

                        };
                        return FatCalibration.ApplySaved(bodyComposition);
                    }
                    else
                    {
                        if (data.Length < 13)
                        {
                            return new BodyComposition
                            {
                                Weight = 0,
                                HasImpedance = hasImpedance,
                                IsStabilized = false,
                            };
                        }
                        var bodyComposition = new BodyComposition
                        {
                            Weight = GetWeight(data),
                            HasImpedance = hasImpedance,
                            IsStabilized = isStabilized,
                        };
                        return bodyComposition;
                    }
                case ScaleType.MiSmartScale:
                    var legacyMiscale = new MiScaleBodyComposition.LegacyMiScale();

                    if (legacyMiscale.Istabilized(data))
                    {
                        var legacyResult = legacyMiscale.GetWeight(data, _user.Height, true);

                        var bodyComposition = new BodyComposition
                        {
                            Weight = legacyResult.Weight,
                            BMI = legacyResult.BMI,
                            Date = legacyResult.Date,
                            IsStabilized = true
                        };

                        return bodyComposition;
                    }
                    else
                    {
                        return null;
                    }
                case ScaleType.S400:
                    if (data.Length is 24 or 26)
                    {
                        this.ValidateAesKey(_user.BindKey);
                        this.ValidateBluetoothAddress(btAddress);

                        var profile = new BodyProfile(
                            _user.Height,
                            _user.Age,
                            _user.Sex == Models.Sex.Female ? BodySex.Female : BodySex.Male);
                        var pipelineResult = _s400Pipeline.Process(
                            data,
                            _user.BindKey,
                            btAddress,
                            profile,
                            FatCalibration.LoadCoreFit());

                        var measurement = pipelineResult.Measurement
                            ?? pipelineResult.PartialMeasurement;
                        if (measurement is null)
                        {
                            return pipelineResult.PreviewWeightKg.HasValue
                                ? new BodyComposition
                                {
                                    Weight = pipelineResult.PreviewWeightKg.Value,
                                    HasImpedance = false,
                                    IsStabilized = false,
                                }
                                : null;
                        }

                        var measuredAt = measurement.MeasuredAt.LocalDateTime;
                        if (pipelineResult.Estimate is null)
                        {
                            var heightMeters = _user.Height / 100.0;
                            return new BodyComposition
                            {
                                Weight = measurement.WeightKg,
                                BMI = Math.Round(measurement.WeightKg / (heightMeters * heightMeters), 1),
                                HasImpedance = false,
                                IsStabilized = true,
                                Date = measuredAt,
                                MeasuredAt = measurement.MeasuredAt,
                                MeasurementId = measurement.MeasurementId,
                                MeasurementQuality = measurement.Quality,
                                HeartRate = measurement.HeartRateBpm,
                                ScaleProfileId = measurement.ProfileId,
                                RawDataLog = measurement.RawAdvertisements.Select(value => value.ToArray()).ToList(),
                            };
                        }

                        var estimate = pipelineResult.Estimate;
                        return new BodyComposition
                        {
                            Weight = estimate.WeightKg,
                            BMI = estimate.Bmi,
                            ProteinPercentage = estimate.ProteinPercentage,
                            IdealWeight = estimate.IdealWeightKg,
                            BMR = estimate.BasalMetabolicRateKcal,
                            BoneMass = estimate.BoneMassKg,
                            Fat = estimate.FatPercentage,
                            MetabolicAge = estimate.MetabolicAge,
                            MuscleMass = estimate.LeanSoftMassKg,
                            VisceralFat = estimate.VisceralFatRating,
                            WaterPercentage = estimate.WaterPercentage,
                            BodyType = estimate.PhysiqueRating,
                            HasImpedance = measurement.Quality == S400MeasurementQuality.DualFrequencyComplete,
                            IsStabilized = true,
                            Date = measuredAt,
                            MeasuredAt = measurement.MeasuredAt,
                            MeasurementId = measurement.MeasurementId,
                            AlgorithmVersion = estimate.AlgorithmVersion,
                            CalibrationVersion = estimate.CalibrationVersion,
                            IsCalibrated = estimate.IsCalibrated,
                            CalibrationPointCount = estimate.CalibrationPointCount,
                            CalibrationRSquared = estimate.CalibrationRSquared,
                            AppliedFatCorrection = estimate.AppliedFatCorrection,
                            BaselineFatPercentage = estimate.BaselineFatPercentage,
                            MeasurementQuality = measurement.Quality,
                            Impedance50Khz = measurement.Impedance50KhzOhm,
                            Impedance250Khz = measurement.Impedance250KhzOhm,
                            HeartRate = measurement.HeartRateBpm,
                            ScaleProfileId = measurement.ProfileId,
                            RawDataLog = measurement.RawAdvertisements.Select(value => value.ToArray()).ToList(),
                        };
                    }
                    var emptyBC = new BodyComposition
                    {
                        Weight = 0,
                        HasImpedance = false,
                        IsStabilized = false,
                    };
                    return emptyBC;

                default:
                    throw new NotImplementedException();
            }
        }

        private double GetWeight(byte[] data)
        {
            return (double)(((data[12] & 0xFF) << 8) | (data[11] & 0xFF)) * 0.005;
        }
    }
}
