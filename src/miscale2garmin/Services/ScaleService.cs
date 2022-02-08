using miscale2garmin.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace miscale2garmin.Services
{
    public class ScaleService : IScaleService
    {
        private IAdapter _adapter;
        private Scale _scale;
        private TaskCompletionSource<BodyComposition> _completionSource;
        public ScaleService()
        {
            _adapter = CrossBluetoothLE.Current.Adapter;
        }

        public async Task<BodyComposition> GetBodyCompositonAsync(Scale scale)
        {
            _completionSource = new TaskCompletionSource<BodyComposition>();
            _scale = scale;
            _adapter.DeviceDiscovered += DevideDiscovered;
            await _adapter.StartScanningForDevicesAsync();
            return await _completionSource.Task;
        }

        public async Task CancelSearchAsync()
        {
            try
            {
                var bc = new BodyComposition { Weight = 0, IsValid = false };
                _completionSource.SetResult(bc);
            }
            catch (Exception ex)
            {
                // TODO Log;
            }
            await StopAsync();
        }

        private void DevideDiscovered(object s, DeviceEventArgs a) {

            var obj = a.Device.NativeDevice;
            PropertyInfo propInfo = obj.GetType().GetProperty("Address");
            string address = (string)propInfo.GetValue(obj, null);

            if(address.ToLowerInvariant() == _scale.Address.ToLowerInvariant())
            {
                StopAsync().Wait();
                var bc = new BodyComposition { Weight = 70, IsValid = true };
                _completionSource.SetResult(bc);
            }
        }

        private async Task StopAsync()
        {
            await _adapter.StopScanningForDevicesAsync();

            _adapter.DeviceDiscovered -= DevideDiscovered;
        }



    }
}
