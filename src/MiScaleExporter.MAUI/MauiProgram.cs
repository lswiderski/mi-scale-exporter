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
            }).UseMauiCommunityToolkit();
            builder.UseAdMob(); //.UseConsentDebugSettings(new ConsentDebugSettings { Reset = true });
#if DEBUG
            AdConfig.UseTestAdUnitIds = true;

#endif
            // AdConfig.DisableConsentCheck = true;
            // Plugin.AdMob reads the default unit while registering its handlers.
            AdConfig.DefaultBannerAdUnitId = "ca-app-pub-1938975042085430/4160336701";
           
            return builder.Build();
        }
    }
}