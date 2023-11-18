using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient.Dto;

namespace MiScaleExporter.Models
{
    public record GarminExternalApiResponse
    {
        public string ClientId { get; set; }
        public AuthStatus AuthStatus { get; set; }
    }
}
