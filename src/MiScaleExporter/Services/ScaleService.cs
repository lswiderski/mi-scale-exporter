using MiScaleExporter.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MiScaleBodyComposition;
using Sex = MiScaleBodyComposition.Contracts.Sex;

namespace MiScaleExporter.Services
{
    public class ScaleService : IScaleService
    {
        private IAdapter _adapter;
        private IDevice _scaleDevice;
        private Scale _scale;
        private User _user;
        private TaskCompletionSource<BodyComposition> _completionSource;
        private BodyComposition bodyComposition;
        private MiScale _decoder;
        private ILogService _logService;

        public ScaleService(ILogService logService)
        {
            _logService = logService;
            _adapter = CrossBluetoothLE.Current.Adapter;
            _adapter.ScanTimeout = 50000;
            _adapter.ScanTimeoutElapsed += TimeOuted;
            _decoder = new MiScale();
        }

        public async Task<BodyComposition> GetBodyCompositonAsync(Scale scale, User user)
        {
            _completionSource = new TaskCompletionSource<BodyComposition>();

            _scale = scale;
            _user = user;
            _adapter.DeviceDiscovered += DevideDiscovered;
            await _adapter.StartScanningForDevicesAsync();
            return await _completionSource.Task;
        }

        public async Task CancelSearchAsync()
        {
            try
            {
                bodyComposition.IsValid = false;
                _completionSource.SetResult(bodyComposition);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex.Message);
            }

            await StopAsync();
        }

        private void TimeOuted(object s, EventArgs e)
        {
            StopAsync().Wait();
            bodyComposition.IsValid = false;
            _completionSource.SetResult(bodyComposition);
        }

        private void DevideDiscovered(object s, DeviceEventArgs a)
        {
            var obj = a.Device.NativeDevice;
            PropertyInfo propInfo = obj.GetType().GetProperty("Address");
            string address = (string) propInfo.GetValue(obj, null);

            if (address.ToLowerInvariant() == _scale.Address?.ToLowerInvariant())
            {
                try
                {
                    _scaleDevice = a.Device;
                    GetScanData();
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    StopAsync().Wait();
                    bodyComposition.IsValid = true;
                    _completionSource.SetResult(bodyComposition);
                }
            }
        }

        private void GetScanData()
        {
            if (_scaleDevice != null)
            {
                var data = _scaleDevice.AdvertisementRecords
                    .Where(x => x.Type == Plugin.BLE.Abstractions.AdvertisementRecordType.ServiceData) //0x16
                    .Select(x => x.Data)
                    .FirstOrDefault();
                ComputeData(data);
            }
        }

        private void ComputeData(byte[] data)
        {
            var buffer = data.Skip(2).ToArray(); // checks why the array is shifted by 2 bytes
            var ctrlByte1 = buffer[1];
            var stabilized = ctrlByte1 & (1 << 5);
            if (stabilized <= 0) return;
            var bc = this._decoder.GetBodyComposition(buffer,
                new MiScaleBodyComposition.Contracts.User(_user.Age, _user.Height, (Sex) (byte) _user.Sex));

            bodyComposition = new BodyComposition
            {
                Weight = bc.Weight,
                BMI = bc.BMI,
                ProteinPercentage = bc.ProteinPercentage,
                IdealWeight = bc.IdealWeight,
                BMR = bc.BMR,
                BoneMass = bc.BoneMass,
                Fat = bc.Fat,
                MetabolicAge = bc.MetabolicAge,
                MuscleMass = bc.MuscleMass,
                VisceralFat = bc.VisceralFat,
                WaterPercentage = bc.Water,
                BodyType = bc.BodyType,
            };
        }

        private async Task StopAsync()
        {
            await _adapter.StopScanningForDevicesAsync();

            _adapter.DeviceDiscovered -= DevideDiscovered;
        }
        
    }
}