using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using MiScaleExporter.Droid;
using MiScaleExporter.Permission;
using Plugin.MauiMTAdmob;

namespace MiScaleExporter.MAUI
{
    [Activity(Label = "MiScale Exporter", Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Platform.Init(this, savedInstanceState);
            CrossMauiMTAdmob.Current.Init(this, "ca-app-pub-1938975042085430~7383010816");
            CrossMauiMTAdmob.Current.UserPersonalizedAds = false;
            DependencyService.Register<IBluetoothConnectPermission, BluetoothConnectPermission>();
            // LoadApplication(app);
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}