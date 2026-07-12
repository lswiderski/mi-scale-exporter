using MiScaleExporter.Models;
using MiScaleExporter.Services;
using NUnit.Framework;

namespace MiScaleExporter.UnitTests
{
    [TestFixture]
    public class S400DataInterpreterTests
    {
        private const string BluetoothAddress = "84:46:93:64:A5:E6";
        private const string BindKey = "58305740b64e4b425e518aa1f4e51339";
        private const string ImpedanceLowPacket = "4859d53b2e724a8c783dc8a392c10db411000000a8a7bad5";
        private const string ImpedanceHighPacket = "4859d53b2d3314943c58b133638c7457a4000000c3e670dc";

        private static User CreateUser()
        {
            return new User
            {
                Height = 182,
                Age = 29,
                Sex = Sex.Male,
                ScaleType = ScaleType.S400,
                BindKey = BindKey,
            };
        }

        [Test]
        public void ComputesFullS400MetricsFromBothAdvertisements()
        {
            var interpreter = new S400DataInterpreter();
            var user = CreateUser();

            var pending = interpreter.ComputeData(Convert.FromHexString(ImpedanceLowPacket), user, BluetoothAddress);
            var result = interpreter.ComputeData(Convert.FromHexString(ImpedanceHighPacket), user, BluetoothAddress);

            Assert.AreEqual(0, pending.Weight);
            Assert.IsFalse(pending.HasImpedance);
            Assert.AreEqual(74.2, result.Weight);
            Assert.IsTrue(result.HasImpedance);
            Assert.IsTrue(result.HasDualFrequencyImpedance);
            Assert.AreEqual(360.0, result.ImpedanceLow);
            Assert.AreEqual(400.9, result.ImpedanceHigh);
            Assert.AreEqual(61.7, result.LeanBodyMass);
            Assert.AreEqual(16.9, result.Fat);
            Assert.AreEqual(60.7, result.WaterPercentage);
            Assert.AreEqual(17.64, result.ExtracellularWater);
            Assert.AreEqual(27.38, result.IntracellularWater);
            Assert.AreEqual(39.2, result.EcwTbwRatio);
            Assert.AreEqual(37.51, result.BodyCellMass);
            Assert.AreEqual(40.00, result.SkeletalMuscleMass);
            Assert.AreEqual(16.2, result.ProteinPercentage);
            Assert.AreEqual(1702, result.BMR);
            Assert.AreEqual(30, result.MetabolicAge);
            Assert.Greater(result.MuscleMass, result.SkeletalMuscleMass);
            Assert.That(result.HeartRate, Is.Not.Null.And.GreaterThan(0));
        }

        [Test]
        public void WaitsForBothFrequenciesWhenWeightArrivesFirst()
        {
            var interpreter = new S400DataInterpreter();
            var user = CreateUser();

            var pending = interpreter.ComputeData(Convert.FromHexString(ImpedanceHighPacket), user, BluetoothAddress);
            var result = interpreter.ComputeData(Convert.FromHexString(ImpedanceLowPacket), user, BluetoothAddress);

            Assert.AreEqual(74.2, pending.Weight);
            Assert.IsFalse(pending.HasImpedance);
            Assert.IsNull(pending.LeanBodyMass);
            Assert.IsTrue(result.HasImpedance);
            Assert.AreEqual(61.7, result.LeanBodyMass);
        }

        [Test]
        public void ResetPreventsAnIncompleteReadingFromLeakingIntoTheNextScan()
        {
            var interpreter = new S400DataInterpreter();
            var user = CreateUser();

            interpreter.ComputeData(Convert.FromHexString(ImpedanceHighPacket), user, BluetoothAddress);
            interpreter.ResetMeasurement();
            var result = interpreter.ComputeData(Convert.FromHexString(ImpedanceLowPacket), user, BluetoothAddress);

            Assert.AreEqual(0, result.Weight);
            Assert.IsFalse(result.HasImpedance);
            Assert.IsFalse(result.HasDualFrequencyImpedance);
        }
    }
}
