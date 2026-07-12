using Xiaomi.BodyComposition.S400.Contracts;
using MiScaleExporter.Models;
using S400Library = Xiaomi.BodyComposition.S400;

namespace MiScaleExporter.Services
{
    internal sealed class S400DataInterpreter
    {
        private readonly object _sync = new object();
        private S400Library.S400Scale _scale = new S400Library.S400Scale();

        public void ResetMeasurement()
        {
            lock (_sync)
            {
                _scale = new S400Library.S400Scale();
            }
        }

        public BodyComposition ComputeData(byte[] data, User userInfo, string bluetoothAddress)
        {
            if (data == null || (data.Length != 24 && data.Length != 26))
            {
                return EmptyResult();
            }

            ValidateAesKey(userInfo.BindKey);
            ValidateBluetoothAddress(bluetoothAddress);

            var user = new S400Library.User(
                userInfo.Height,
                userInfo.Age,
                (S400Library.Sex)(byte)userInfo.Sex);

            S400Library.BodyComposition result;
            lock (_sync)
            {
                result = _scale.GetBodyComposition(user, new S400InputData
                {
                    Data = data,
                    AesKey = userInfo.BindKey,
                    MacOriginal = bluetoothAddress,
                });
            }

            if (result == null)
            {
                return EmptyResult();
            }

            var hasDualFrequencyImpedance = result.ImpedanceLow.HasValue
                && result.ImpedanceHigh.HasValue
                && result.LeanBodyMass.HasValue;

            return new BodyComposition
            {
                Weight = result.Weight,
                BMI = result.BMI,
                ProteinPercentage = result.ProteinPercentage,
                IdealWeight = result.IdealWeight,
                BMR = result.BMR,
                BoneMass = result.BoneMass,
                Fat = result.Fat,
                MetabolicAge = result.MetabolicAge,
                MuscleMass = result.MuscleMass,
                VisceralFat = result.VisceralFat,
                WaterPercentage = result.Water,
                BodyType = result.BodyType,
                ImpedanceLow = result.ImpedanceLow,
                ImpedanceHigh = result.ImpedanceHigh,
                LeanBodyMass = result.LeanBodyMass,
                ExtracellularWater = result.ExtracellularWater,
                IntracellularWater = result.IntracellularWater,
                EcwTbwRatio = result.EcwTbwRatio,
                BodyCellMass = result.BodyCellMass,
                SkeletalMuscleMass = result.SkeletalMuscleMass,
                HeartRate = result.HeartRate,
                HasImpedance = hasDualFrequencyImpedance,
                IsStabilized = result.Weight > 0,
                Date = result.Date,
            };
        }

        private static BodyComposition EmptyResult()
        {
            return new BodyComposition
            {
                Weight = 0,
                HasImpedance = false,
                IsStabilized = false,
            };
        }

        private static void ValidateAesKey(string aesKey)
        {
            if (string.IsNullOrEmpty(aesKey) || aesKey.Length != 32)
            {
                throw new ArgumentException("AES key must be a 32-character hexadecimal string.");
            }
        }

        private static void ValidateBluetoothAddress(string bluetoothAddress)
        {
            if (string.IsNullOrEmpty(bluetoothAddress)
                || bluetoothAddress.Length != 17
                || !bluetoothAddress.All(c => char.IsLetterOrDigit(c) || c == ':'))
            {
                throw new ArgumentException("Bluetooth address must be a valid 17-character string in the format XX:XX:XX:XX:XX:XX.");
            }
        }
    }
}
