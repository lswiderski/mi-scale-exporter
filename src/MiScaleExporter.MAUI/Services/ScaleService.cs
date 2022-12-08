using MiScaleExporter.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MiScaleExporter.Services
{
    public class ScaleService : IScaleService
    {
        
       
        private User _user;
       
        
        private ILogService _logService;
        
        private byte[] _previousErrorData;
        private string weightLabel;

        public ScaleService(ILogService logService)
        {
            _logService = logService;
           
        }

        private double GetWeight(byte[] data)
        {
            return (double)(((data[12] & 0xFF) << 8) | (data[11] & 0xFF)) * 0.005;
        }
        public BodyComposition ComputeData(byte[] data, User _user)
        {
            switch (_user.ScaleType)
            {
                case ScaleType.MiBodyCompositionScale:

                    var miBodyCompositionScale = new MiScaleBodyComposition.MiScale();
                    var user = new MiScaleBodyComposition.User(_user.Height, _user.Age, (MiScaleBodyComposition.Sex)(byte)_user.Sex);
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
                default:
                    throw new NotImplementedException();
            }


        }

       

    }
}