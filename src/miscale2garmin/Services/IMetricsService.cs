using miscale2garmin.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace miscale2garmin.Services
{
    public interface IMetricsService
    {
        BodyComposition GetBodyComposition(User user, double weight, double impedance);
    }
}
