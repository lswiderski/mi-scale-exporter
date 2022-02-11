using System;
using MiScaleDecoder.Contracts;

namespace MiScaleDecoder
{
    public class Decoder
    {
        private double _weight;
        private double _impedance;
        private double _height;
        private double _age;
        private Sex _sex;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"> 13 bytes data array</param>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public BodyComposition Calculate(byte[] data, User userInfo)
        {
            if (userInfo is null)
            {
                throw new ArgumentNullException(nameof(userInfo), "information about user cannot be empty");
            }

            if (data is null)
            {
                throw new ArgumentNullException(nameof(data), "data cannot be empty");
            }

            var ctrlByte1 = data[1];
            var stabilized = ctrlByte1 & (1 << 5);
            var hasImpedance = ctrlByte1 & (1 << 1);
            if (stabilized <= 0 || hasImpedance <= 0)
            {
                throw new Exception(
                    "data from mi scale are not stabilized. Wait until the end of measurement");
            }

            this._weight = this.GetWeight(data);
            this._impedance = this.GetImpedance(data);
            this._height = userInfo.Height;
            this._age = userInfo.Age;
            this._sex = userInfo.Sex;

            return this.GetBodyComposition();
        }

        private double GetWeight(byte[] data)
        {
            return (((data[12] & 0xFF) << 8) | (data[11] & 0xFF)) * 0.005;
        }

        private double GetImpedance(byte[] data)
        {
            return (data[10] << 8) + data[9];
        }

        private BodyComposition GetBodyComposition()
        {
            return new BodyComposition
            {
                Weight = _weight,
                BMI = this.GetBmi(),
                ProteinPercentage = this.GetProteinPercentage(),
                IdealWeight = this.GetIdealWeight(),
                BMR = this.GetBmr(),
                BoneMass = this.GetBoneMass(),
                Fat = this.GetFatPercentage(),
                LBMCoefficient = this.GetLbmCoefficient(),
                MetabolicAge = this.GetMetabolicAge(),
                MuscleMass = this.GetMuscleMass(),
                VisceralFat = this.GetVisceralFat()
            };
        }

        private double CheckValueOverflow(double value, double min, double max)
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

        private double GetIdealWeight()
        {
            switch (_sex)
            {
                case Sex.Male:
                    return (this._height - 80) * 0.7;
                case Sex.Female:
                    return (this._height - 70) * 0.6;
                default:
                    throw new NotImplementedException();
            }
        }

        private double GetMetabolicAge()
        {
            double metabolicAge;
            switch (_sex)
            {
                case Sex.Male:
                    metabolicAge = (this._height * -0.7471) + (this._weight * 0.9161) + (this._age * 0.4184) +
                                   (this._impedance * 0.0517) + 54.2267;
                    break;
                case Sex.Female:
                    metabolicAge = (this._height * -1.1165) + (this._weight * 1.5784) + (this._age * 0.4615) +
                                   (this._impedance * 0.0415) + 83.2548;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return CheckValueOverflow(metabolicAge, 15, 80);
        }

        private double GetVisceralFat()
        {
            double subcalc, vfal;

            if (this._sex == Sex.Female)
            {
                if (this._weight > (13 - (this._height * 0.5)) * -1)
                {
                    var subsubcalc = ((this._height * 1.45) + (this._height * 0.1158) * this._height) - 120;
                    subcalc = this._weight * 500 / subsubcalc;
                    vfal = (subcalc - 6) + (this._age * 0.07);
                }
                else
                {
                    subcalc = 0.691 + (this._height * -0.0024) + (this._height * -0.0024);
                    vfal = (((this._height * 0.027) - (subcalc * this._weight)) * -1) + (this._age * 0.07) - this._age;
                }
            }
            else if (this._height < this._weight * 1.6)
            {
                subcalc = ((this._height * 0.4) - (this._height * (this._height * 0.0826))) * -1;
                vfal = ((this._weight * 305) / (subcalc + 48)) - 2.9 + (this._age * 0.15);
            }
            else
            {
                subcalc = 0.765 + this._height * -0.0015;
                vfal = (((this._height * 0.143) - (this._weight * subcalc)) * -1) + (this._age * 0.15) - 5.0;
            }

            return this.CheckValueOverflow(vfal, 1, 50);
        }

        private double GetProteinPercentage()
        {
            var proteinPercentage = (this.GetMuscleMass() / this._weight) * 100;
            proteinPercentage -= this.GetWaterPercentage();

            return this.CheckValueOverflow(proteinPercentage, 5, 32);
        }

        private double GetWaterPercentage()
        {
            var waterPercentage = (100 - this.GetFatPercentage()) * 0.7;
            var coefficient = 0.98;
            if (waterPercentage <= 50) coefficient = 1.02;
            if (waterPercentage * coefficient >= 65) waterPercentage = 75;
            return this.CheckValueOverflow(waterPercentage * coefficient, 35, 75);
        }

        private double GetBmi()
        {
            return this.CheckValueOverflow(this._weight / ((this._height / 100) * (this._height / 100)), 10, 90);
        }

        private double GetBmr()
        {
            double bmr;

            switch (this._sex)
            {
                case Sex.Male:
                    bmr = 877.8 + this._weight * 14.916;
                    bmr -= this._height * 0.726;
                    bmr -= this._age * 8.976;
                    if (bmr > 2322)
                    {
                        bmr = 5000;
                    }

                    break;
                case Sex.Female:
                    bmr = 864.6 + this._weight * 10.2036;
                    bmr -= this._height * 0.39336;
                    bmr -= this._age * 6.204;

                    if (bmr > 2996)
                    {
                        bmr = 5000;
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }

            return this.CheckValueOverflow(bmr, 500, 10000);
        }

        private double GetFatPercentage()
        {
            var value = 0.8;
            if (this._sex == Sex.Female && this._age <= 49)
            {
                value = 9.25;
            }
            else if (this._sex == Sex.Female && this._age > 49)
            {
                value = 7.25;
            }

            var LBM = this.GetLbmCoefficient();
            var coefficient = 1.0;

            if (this._sex == Sex.Male && this._weight < 61)
            {
                coefficient = 0.98;
            }
            else if (this._sex == Sex.Female && this._weight > 60)
            {
                if (this._height > 160)
                {
                    coefficient *= 1.03;
                }
                else
                {
                    coefficient = 0.96;
                }
            }
            else if (this._sex == Sex.Female && this._weight < 50)
            {
                if (this._height > 160)
                {
                    coefficient *= 1.03;
                }
                else
                {
                    coefficient = 1.02;
                }
            }

            var fatPercentage = (1.0 - (((LBM - value) * coefficient) / this._weight)) * 100;


            if (fatPercentage > 63)
            {
                fatPercentage = 75;
            }

            return this.CheckValueOverflow(fatPercentage, 5, 75);
        }

        private double GetMuscleMass()
        {
            var muscleMass = this._weight - ((this.GetFatPercentage() * 0.01) * this._weight) - this.GetBoneMass();
            if (this._sex == Sex.Female && muscleMass >= 84)
            {
                muscleMass = 120;
            }
            else if (this._sex == Sex.Male && muscleMass >= 93.5)
            {
                muscleMass = 120;
            }

            return this.CheckValueOverflow(muscleMass, 10, 120);
        }

        private double GetBoneMass()
        {
            var @base = 0.18016894;
            if (this._sex == Sex.Female)
            {
                @base = 0.245691014;
            }

            var boneMass = (@base - (this.GetLbmCoefficient() * 0.05158)) * -1;

            if (boneMass > 2.2)
            {
                boneMass += 0.1;
            }
            else
            {
                boneMass -= 0.1;
            }

            if (this._sex == Sex.Female && boneMass > 5.1)
            {
                boneMass = 8;
            }
            else if (this._sex == Sex.Male && boneMass > 5.2)
            {
                boneMass = 8;
            }

            return this.CheckValueOverflow(boneMass, 0.5, 8);
        }

        private double GetLbmCoefficient()
        {
            var lbm = (this._height * 9.058 / 100) * (this._height / 100);
            lbm += this._weight * 0.32 + 12.226;
            lbm -= this._impedance * 0.0068;
            lbm -= this._age * 0.0542;
            return lbm;
        }
    }
}