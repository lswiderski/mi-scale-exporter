using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    public interface IDataInterpreter
    {
        void ResetSession();
        BodyComposition ComputeData(byte[] data, User _user, string btAddress);
    }
}
