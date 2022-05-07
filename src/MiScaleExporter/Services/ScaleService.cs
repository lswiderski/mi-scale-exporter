using MiScaleExporter.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Essentials;

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
        private ILogService _logService;
        private byte[] _scannedData;

        public ScaleService(ILogService logService)
        {
            _logService = logService;
            _adapter = CrossBluetoothLE.Current.Adapter;
            _adapter.ScanTimeout = 50000;
            _adapter.ScanTimeoutElapsed += TimeOuted;
        }

        public async Task<Models.BodyComposition> GetBodyCompositonAsync(Scale scale, Models.User user)
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
                    _logService.LogError(ex.Message);

                    if (_scannedData != null)
                    {
                        _logService.LogInfo(String.Join("; ",_scannedData));
                    }
                }
                finally
                {
                    StopAsync().Wait();
                    if (bodyComposition != null)
                    {
                        bodyComposition.IsValid = true;
                    }
                   
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
                _scannedData = data;
                if(Preferences.Get(PreferencesKeys.ShowReceivedByteArray, false))
                {
                    _logService.LogInfo(String.Join("; ", _scannedData));
                }
                
                ComputeData(data);
            }
        }

        private void ComputeData(byte[] data)
        {
            var buffer = data.ToArray();

            switch (_user.ScaleType)
            {
                case ScaleType.MiBodyCompositionScale:
                    var miScale = new MiScaleBodyComposition.MiScale();
                    var bc = miScale.GetBodyComposition(buffer,
                        new MiScaleBodyComposition.User(_user.Height, _user.Age, (MiScaleBodyComposition.Sex)(byte)_user.Sex));
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
                    break;
                case ScaleType.MiSmartScale:
                    var legacyMiscale = new MiScaleBodyComposition.LegacyMiScale();
                    var legacyResult = legacyMiscale.GetWeight(buffer, _user.Height);

                    bodyComposition = new BodyComposition
                    {
                        Weight = legacyResult.Weight,
                        BMI = legacyResult.BMI,
                    };

                    break;
                default:
                    throw new NotImplementedException();
            }

         
        }

        private async Task StopAsync()
        {
            await _adapter.StopScanningForDevicesAsync();

            _adapter.DeviceDiscovered -= DevideDiscovered;
        }
        
    }
}