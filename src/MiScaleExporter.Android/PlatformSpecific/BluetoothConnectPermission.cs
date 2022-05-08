using MiScaleExporter.Permission;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiScaleExporter.Droid
{
    public class BluetoothConnectPermission : Xamarin.Essentials.Permissions.BasePlatformPermission, IBluetoothConnectPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new List<(string androidPermission, bool isRuntime)>
    {
        (Android.Manifest.Permission.BluetoothConnect, true),
        (Android.Manifest.Permission.BluetoothScan, true),
        (Android.Manifest.Permission.BluetoothAdvertise, true),
    }.ToArray();
    }
}
