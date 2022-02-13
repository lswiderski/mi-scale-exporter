using MiScaleExporter.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MiScaleExporter.Services
{
    public interface IScaleService
    {
        Task<BodyComposition> GetBodyCompositonAsync(Scale scale, User user);
        Task CancelSearchAsync();
    }
}
