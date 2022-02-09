using miscale2garmin.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace miscale2garmin.Services
{
    public class MetricsService : IMetricsService
    {
        private double weight;
        private double impedance;
        private int height;
        private int age;
        private Sex sex;

        public MetricsService()
        {

        }

        public BodyComposition GetBodyComposition(User user, double weight, double impedance)
        {
            this.weight = weight;
            this.impedance = impedance;
            this.height = user.Height;
            this.age = user.Age;
            this.sex = user.Sex;

            var bc = new BodyComposition
            {
                IsValid = true,
                Weight = weight,
                BMI = this.getBMI(),
                IdealWeight = this.getIdealWeight(),
                BMR = this.getBMR(),
                BoneMass = this.getBoneMass(),
                Fat = this.getFatPercentage(),
                LBMCoefficient = this.getLBMCoefficient(),
                MetabolicAge = this.getMetabolicAge(),
                MuscleMass = this.getMuscleMass(),
                VisceralFat = this.getVisceralFat()
            };
            return bc;
        }

        private double checkValueOverflow(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }
            return value;
        }

        private double getIdealWeight()
        {
            switch (this.sex)
            {
                case Sex.Male:
                    return (this.height - 80) * 0.7;
                case Sex.Female:
                    return (this.height - 70) * 0.6;
                default:
                    throw new NotImplementedException();
            }
        }

        private double getMetabolicAge()
        {
            double metabolicAge;
            switch (this.sex)
            {
                case Sex.Male:
                    metabolicAge = (this.height * -0.7471) + (this.weight * 0.9161) + (this.age * 0.4184) + (this.impedance * 0.0517) + 54.2267;
                    break;
                case Sex.Female:
                    metabolicAge = (this.height * -1.1165) + (this.weight * 1.5784) + (this.age * 0.4615) + (this.impedance * 0.0415) + 83.2548;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return checkValueOverflow(metabolicAge, 15, 80);
        }

        private double getVisceralFat()
        {
            double subsubcalc, subcalc, vfal;

            if (this.sex == Sex.Female)
            {
                if (this.weight > (13 - (this.height * 0.5)) * -1)
                {
                    subsubcalc = ((this.height * 1.45) + (this.height * 0.1158) * this.height) - 120;
                    subcalc = this.weight * 500 / subsubcalc;
                    vfal = (subcalc - 6) + (this.age * 0.07);
                }
                else
                {
                    subcalc = 0.691 + (this.height * -0.0024) + (this.height * -0.0024);
                    vfal = (((this.height * 0.027) - (subcalc * this.weight)) * -1) + (this.age * 0.07) - this.age;
                }
            }
            else if (this.height < this.weight * 1.6)
            {
                subcalc = ((this.height * 0.4) - (this.height * (this.height * 0.0826))) * -1;
                vfal = ((this.weight * 305) / (subcalc + 48)) - 2.9 + (this.age * 0.15);
            }
            else
            {
                subcalc = 0.765 + this.height * -0.0015;
                vfal = (((this.height * 0.143) - (this.weight * subcalc)) * -1) + (this.age * 0.15) - 5.0;
            }
            return this.checkValueOverflow(vfal, 1, 50);
        }

        private double getProteinPercentage()
        {
            var proteinPercentage = (this.getMuscleMass() / this.weight) * 100;
            proteinPercentage -= this.getWaterPercentage();


            return this.checkValueOverflow(proteinPercentage, 5, 32);
        }

        private double getWaterPercentage()
        {
            var waterPercentage = (100 - this.getFatPercentage()) * 0.7;
            var coefficient = 0.98;
            if (waterPercentage <= 50) coefficient = 1.02;
            if (waterPercentage * coefficient >= 65) waterPercentage = 75;
            return this.checkValueOverflow(waterPercentage * coefficient, 35, 75);
        }

        private double getBMI()
        {
            return this.checkValueOverflow(this.weight / ((this.height / 100) * (this.height / 100)), 10, 90);
        }

        private double getBMR()
        {
            double bmr;

            switch (this.sex)
            {
                case Sex.Male:
                    bmr = 877.8 + this.weight * 14.916;
                    bmr -= this.height * 0.726;
                    bmr -= this.age * 8.976;
                    if (bmr > 2322)
                    {
                        bmr = 5000;
                    }
                    break;
                case Sex.Female:
                    bmr = 864.6 + this.weight * 10.2036;
                    bmr -= this.height * 0.39336;
                    bmr -= this.age * 6.204;

                    if (bmr > 2996)
                    {
                        bmr = 5000;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            return this.checkValueOverflow(bmr, 500, 10000);
        }

        private double getFatPercentage()
        {
            var value = 0.8;
            if (this.sex == Sex.Female && this.age <= 49)
            {
                value = 9.25;
            }
            else if (this.sex == Sex.Female && this.age > 49)
            {
                value = 7.25;
            }

            var LBM = this.getLBMCoefficient();
            var coefficient = 1.0;

            if (this.sex == Sex.Male && this.weight < 61)
            {
                coefficient = 0.98;
            }
            else if (this.sex == Sex.Female && this.weight > 60)
            {
                if (this.height > 160)
                {
                    coefficient *= 1.03;
                }
                else
                {
                    coefficient = 0.96;
                }
            }
            else if (this.sex == Sex.Female && this.weight < 50)
            {
                if (this.height > 160)
                {
                    coefficient *= 1.03;
                }
                else
                {
                    coefficient = 1.02;
                }
            }
            var fatPercentage = (1.0 - (((LBM - value) * coefficient) / this.weight)) * 100;


            if (fatPercentage > 63)
            {
                fatPercentage = 75;
            }
            return this.checkValueOverflow(fatPercentage, 5, 75);
        }

        private double getMuscleMass()
        {
            var muscleMass = this.weight - ((this.getFatPercentage() * 0.01) * this.weight) - this.getBoneMass();
            if (this.sex == Sex.Female && muscleMass >= 84)
            {
                muscleMass = 120;
            }
            else if (this.sex == Sex.Male && muscleMass >= 93.5)
            {
                muscleMass = 120;
            }
            return this.checkValueOverflow(muscleMass, 10, 120);
        }

        private double getBoneMass()
        {
            var _base = 0.18016894;
            if (this.sex == Sex.Female)
            {
                _base = 0.245691014;
            }

            var boneMass = (_base - (this.getLBMCoefficient() * 0.05158)) * -1;

            if (boneMass > 2.2)
            {
                boneMass += 0.1;
            }
            else
            {
                boneMass -= 0.1;
            }

            if (this.sex == Sex.Female && boneMass > 5.1)
            {
                boneMass = 8;
            }
            else if (this.sex == Sex.Male && boneMass > 5.2)
            {
                boneMass = 8;
            }

            return this.checkValueOverflow(boneMass, 0.5, 8);
        }

        private double getLBMCoefficient()
        {
            var lbm = (this.height * 9.058 / 100) * (this.height / 100);
            lbm += this.weight * 0.32 + 12.226;
            lbm -= this.impedance * 0.0068;
            lbm -= this.age * 0.0542;
            return lbm;
        }
    }
}
