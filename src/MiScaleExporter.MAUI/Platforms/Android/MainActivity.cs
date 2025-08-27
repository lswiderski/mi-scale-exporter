using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using MiScaleExporter.Droid;
using MiScaleExporter.Permission;

namespace MiScaleExporter.MAUI
{
    [Activity(Label = "MiScale Exporter", Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Platform.Init(this, savedInstanceState);
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