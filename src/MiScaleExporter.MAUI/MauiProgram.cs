using CommunityToolkit.Maui;

namespace MiScaleExporter.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("FontAwesome6Regular.otf", "FontAwesome6Regular");
                fonts.AddFont("FontAwesome6Solid.otf", "FontAwesome6Solid");
            }).UseMauiCommunityToolkit();
            return builder.Build();
        }
    }
}