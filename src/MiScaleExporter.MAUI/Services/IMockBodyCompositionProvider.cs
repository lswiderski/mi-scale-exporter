using MiScaleExporter.Models;

namespace MiScaleExporter.Services
{
    public interface IMockBodyCompositionProvider
    {
        BodyComposition Get(ScaleType type);
        BodyComposition GetPrevious(ScaleType type);
        User GetUser();
    }
}
