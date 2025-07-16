using MiScaleBodyComposition.Contracts;
using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    public class DataInterpreter : IDataInterpreter
    {
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

        private MiScaleBodyComposition.S400Scale _s400Scale;
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
                        _s400Scale = new MiScaleBodyComposition.S400Scale();
                    }

                    if(data.Length == 26)
                    {
                        this.ValidateAesKey(_user.BindKey);
                        this.ValidateBluetoothAddress(btAddress);

                        var s400Result = _s400Scale.GetBodyComposition(user, new S400InputData
                        {
                            //AesKeyBytes = aesKey,
                            //MacBytes = mac,
                            Data = data,
                            AesKey = _user.BindKey,
                            MacOriginal = btAddress,
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
