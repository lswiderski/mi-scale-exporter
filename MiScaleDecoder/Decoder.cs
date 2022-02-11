using System;
using System.Collections.Generic;
using System.Linq;
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

        private static MuscleMassScale[] MuscleMassScales => new[]
        {
            new MuscleMassScale
            {
                Min = new Dictionary<Sex, double>() {{Sex.Male, 170}, {Sex.Female, 160}},
                Female = new[] {36.5, 42.6},
                Male = new[] {49.4, 59.5}
            },
            new MuscleMassScale
            {
                Min = new Dictionary<Sex, double>() {{Sex.Male, 160}, {Sex.Female, 150}},
                Female = new[] {32.9, 37.6},
                Male = new[] {44.0, 52.5}
            },
            new MuscleMassScale
            {
                Min = new Dictionary<Sex, double>() {{Sex.Male, 0}, {Sex.Female, 0}},
                Female = new[] {29.1, 34.8},
                Male = new[] {38.5, 46.6}
            }
        };

        //The included tables where quite strange, maybe bogus, replaced them with better ones...
        private static FatPercentageScale[] FatPercentageScales => new[]
        {
            new FatPercentageScale
            {
                Min = 0, Max = 12,
                Female = new double[] {12, 21, 30, 34},
                Male = new double[] {7, 16, 25, 30}
            },
            new FatPercentageScale
            {
                Min = 12, Max = 14,
                Female = new double[] {15.0, 24.0, 33.0, 37.0},
                Male = new double[] {7.0, 16.0, 25.0, 30.0},
            },
            new FatPercentageScale
            {
                Min = 14, Max = 16,
                Female = new double[] {18.0, 27.0, 36.0, 40.0},
                Male = new double[] {7.0, 16.0, 25.0, 30.0},
            },
            new FatPercentageScale
            {
                Min = 16, Max = 18,
                Female = new double[] {20.0, 28.0, 37.0, 41.0},
                Male = new double[] {7.0, 16.0, 25.0, 30.0},
            },
            new FatPercentageScale
            {
                Min = 18, Max = 40,
                Female = new double[] {21.0, 28.0, 35.0, 40.0},
                Male = new double[] {11.0, 17.0, 22.0, 27.0},
            },
            new FatPercentageScale
            {
                Min = 40, Max = 60,
                Female = new double[] {22.0, 29.0, 36.0, 41.0},
                Male = new double[] {12.0, 18.0, 23.0, 28.0},
            },
            new FatPercentageScale
            {
                Min = 60, Max = 100,
                Female = new double[] {23.0, 30.0, 37.0, 42.0},
                Male = new double[] {14.0, 20.0, 25.0, 30.0},
            }
        };

        private static string[] BodyTypeScale => new[]
        {
            "obese", "overweight", "thick-set", "lack-exerscise", "balanced", "balanced-muscular", "skinny",
            "balanced-skinny", "skinny-muscular"
        };

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
            if (data.Length != 13)
            {
                throw new Exception( "data must 13 bytes long");
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

            return this.GetBodyComposition(data);
        }

        private double GetWeight(byte[] data)
        {
            return (((data[12] & 0xFF) << 8) | (data[11] & 0xFF)) * 0.005;
        }

        private double GetImpedance(byte[] data)
        {
            return (data[10] << 8) + data[9];
        }

        private BodyComposition GetBodyComposition(byte[] data)
        {
            var bodyType = this.GetBodyType();
            
            return new BodyComposition
            {
                Weight = _weight,
                BMI =  Math.Round(this.GetBmi(),1),
                ProteinPercentage = Math.Round(this.GetProteinPercentage(),1),
                IdealWeight = Math.Round(this.GetIdealWeight(),2),
                BMR = Math.Round(this.GetBmr(),0),
                BoneMass = Math.Round(this.GetBoneMass(),2),
                Fat = Math.Round(this.GetFatPercentage(),1),
                LBMCoefficient = this.GetLbmCoefficient(),
                MetabolicAge = Math.Round(this.GetMetabolicAge(),0),
                MuscleMass = Math.Round(this.GetMuscleMass(),2),
                VisceralFat = Math.Round(this.GetVisceralFat(),0),
                Water = Math.Round(this.GetWater(),1),
                BodyType = bodyType+1,
                BodyTypeName = BodyTypeScale[bodyType],
                Day = data[5],
                Month = data[4],
                Hour = data[6],
                Minute = data[8],
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

        private double GetWater()
        {
            double coefficient;
            double waterPercentage = (100 - this.GetFatPercentage()) * 0.7;

            if (waterPercentage <= 50)
            {
                coefficient = 1.02;
            }
            else
            {
                coefficient = 0.98;
            }

            // Capping water percentage
            if (waterPercentage * coefficient >= 65)
            {
                waterPercentage = 75;
            }

            return CheckValueOverflow(waterPercentage * coefficient, 35, 75);
        }

        public int GetBodyType()
        {
            int factor;
            if (this.GetFatPercentage() > this.GetFatPercentageScale()[2])
            {
                factor = 0;
            }
            else if (this.GetFatPercentage() < this.GetFatPercentageScale()[1])
            {
                factor = 2;
            }
            else
            {
                factor = 1;
            }


            if (this.GetMuscleMass() > this.GetMuscleMassScale()[1])
            {
                return 2 + (factor * 3);
            }
            else if (this.GetMuscleMass() < this.GetMuscleMassScale()[0])
            {
                return (factor * 3);
            }
            else
            {
                return 1 + (factor * 3);
            }
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

        private double[] GetMuscleMassScale()
        {
            var scale = MuscleMassScales.FirstOrDefault(s => this._height >= s.Min[this._sex]);

            switch (_sex)
            {
                case Sex.Female:
                    return scale.Female;
                case Sex.Male:
                    return scale.Male;
                default:
                    throw new NotImplementedException();
            }
        }

        private double[] GetFatPercentageScale()
        {
            var scale = FatPercentageScales
                .FirstOrDefault(s =>
                    this._age >= s.Min
                    && this._age < s.Max);

            switch (_sex)
            {
                case Sex.Female:
                    return scale.Female;
                case Sex.Male:
                    return scale.Male;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}