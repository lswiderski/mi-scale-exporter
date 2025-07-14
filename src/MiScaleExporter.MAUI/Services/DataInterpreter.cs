using MiScaleBodyComposition.Contracts;
using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    public class DataInterpreter : IDataInterpreter
    {
        private MiScaleBodyComposition.S400 _s400Scale;
        public BodyComposition ComputeData(byte[] data, User _user, string btAddress)
        {

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
                        return bodyComposition;
                    }
                    else
                    {
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

                    if (_s400Scale == null)
                    {
                        _s400Scale = new MiScaleBodyComposition.S400();
                    }

                    if(data.Length> 13)
                    {

                        byte[] dataForInput = new byte[data.Length - 2];
                        Array.Copy(data, 2, dataForInput, 0, dataForInput.Length);

                        var s400Result = _s400Scale.GetBodyComposition(user, new S400InputData
                        {
                            AesKey = _user.BindKey,
                            MacOriginal = btAddress,
                            Data = dataForInput
                        });

                        if (s400Result != null)
                        {
                            var bodyComposition = new BodyComposition
                            {
                                Weight = s400Result.Weight,
                                BMI = s400Result.BMI,
                                ProteinPercentage = s400Result.ProteinPercentage,
                                IdealWeight = s400Result.IdealWeight,
                                BMR = s400Result.BMR,
                                BoneMass = s400Result.BoneMass,
                                Fat = s400Result.Fat,
                                MetabolicAge = s400Result.MetabolicAge,
                                MuscleMass = s400Result.MuscleMass,
                                VisceralFat = s400Result.VisceralFat,
                                WaterPercentage = s400Result.Water,
                                BodyType = s400Result.BodyType,
                                HasImpedance = true,
                                IsStabilized = true,
                                Date = s400Result.Date,

                            };
                            return bodyComposition;

                        }
                    }
                 

                    return null;
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
