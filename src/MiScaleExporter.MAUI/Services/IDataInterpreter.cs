using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    public interface IDataInterpreter
    {
        BodyComposition ComputeData(byte[] data, User _user, string btAddress);
    }
}
