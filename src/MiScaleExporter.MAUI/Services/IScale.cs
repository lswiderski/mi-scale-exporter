using MiScaleExporter.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MiScaleExporter.Services
{
    public interface IScale
    {
        BodyComposition BodyComposition { get; set; }
        Task<BodyComposition> GetBodyCompositonAsync(string scaleAddress, User user);

        Task CancelSearchAsync();

        void StopSearch();
    }
}
