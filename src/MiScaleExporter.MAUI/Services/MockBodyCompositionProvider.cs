using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    // DEBUG/preview only. Produces representative payloads to demo the ResultPage
    // without needing real hardware. Not registered on release builds is OK either
    // way — it is harmless and only invoked by the temporary preview entry.
    public class MockBodyCompositionProvider : IMockBodyCompositionProvider
    {
        public User GetUser() => new User
        {
            Age = 32,
            Height = 178,
            Sex = Sex.Male,
            ScaleType = ScaleType.MiBodyCompositionScale,
        };

        public BodyComposition Get(ScaleType type) => type switch
        {
            ScaleType.MiSmartScale => new BodyComposition
            {
                Weight = 78.4,
                BMI = 24.7,
                IsValid = true,
                HasImpedance = false,
                IsStabilized = true,
                Date = DateTime.Now,
            },
            ScaleType.MiBodyCompositionScale => new BodyComposition
            {
                Weight = 78.4,
                BMI = 24.7,
                Fat = 19.8,
                WaterPercentage = 56.2,
                MuscleMass = 58.1,
                BoneMass = 3.1,
                VisceralFat = 8,
                BMR = 1742,
                MetabolicAge = 28,
                ProteinPercentage = 17.4,
                IdealWeight = 73.0,
                BodyType = 3,
                IsValid = true,
                HasImpedance = true,
                IsStabilized = true,
                Date = DateTime.Now,
            },
            ScaleType.S400 => new BodyComposition
            {
                Weight = 80.1,
                BMI = 25.3,
                Fat = 22.4,
                WaterPercentage = 54.0,
                MuscleMass = 57.0,
                BoneMass = 3.0,
                VisceralFat = 10,
                BMR = 1760,
                MetabolicAge = 33,
                ProteinPercentage = 16.9,
                IdealWeight = 73.5,
                BodyType = 4,
                IsValid = true,
                HasImpedance = true,
                IsStabilized = true,
                Date = DateTime.Now,
            },
            _ => new BodyComposition { Weight = 0, IsValid = false },
        };

        public BodyComposition GetPrevious(ScaleType type) => type switch
        {
            ScaleType.MiSmartScale => new BodyComposition
            {
                Weight = 79.0,
                BMI = 24.9,
                HasImpedance = false,
                IsValid = true,
                Date = DateTime.Now.AddDays(-3),
            },
            ScaleType.MiBodyCompositionScale => new BodyComposition
            {
                Weight = 79.1,
                BMI = 24.9,
                Fat = 20.5,
                WaterPercentage = 55.7,
                MuscleMass = 57.8,
                BoneMass = 3.1,
                VisceralFat = 9,
                BMR = 1730,
                MetabolicAge = 29,
                ProteinPercentage = 17.1,
                IdealWeight = 73.0,
                BodyType = 3,
                HasImpedance = true,
                IsValid = true,
                Date = DateTime.Now.AddDays(-3),
            },
            ScaleType.S400 => new BodyComposition
            {
                Weight = 80.6,
                BMI = 25.5,
                Fat = 22.9,
                WaterPercentage = 53.6,
                MuscleMass = 56.7,
                BoneMass = 3.0,
                VisceralFat = 10,
                BMR = 1755,
                MetabolicAge = 33,
                ProteinPercentage = 16.7,
                IdealWeight = 73.5,
                BodyType = 4,
                HasImpedance = true,
                IsValid = true,
                Date = DateTime.Now.AddDays(-3),
            },
            _ => null,
        };
    }
}
