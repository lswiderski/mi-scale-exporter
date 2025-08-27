using CommunityToolkit.Maui;
using Plugin.AdMob;
using Plugin.AdMob.Configuration;

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
            }).UseMauiCommunityToolkit()
             .UseAdMob();

#if DEBUG
            //AdConfig.UseTestAdUnitIds = true;

#endif
            AdConfig.DefaultBannerAdUnitId = "ca-app-pub-1938975042085430/4160336701";
            return builder.Build();
        }
    }
}