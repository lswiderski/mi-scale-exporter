using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using MiScaleExporter.MAUI.ViewModels;
using MiScaleExporter.MAUI.Resources.Localization;
using MiScaleExporter.Models;
using MiScaleExporter.Services;
using Autofac;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
#if ANDROID
using CommunityToolkit.Maui.Alerts;
#endif

namespace MiScaleExporter.MAUI.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [QueryProperty(nameof(MockPreview), "mock")]
    [QueryProperty(nameof(UploadedFlag), "uploaded")]
    [QueryProperty(nameof(GuestFlag), "guest")]
    [QueryProperty(nameof(AutoUploadFlag), "autoupload")]
    public partial class ResultPage : ContentPage
    {
        private IResultViewModel _vm;

        public ResultPage()
        {
            InitializeComponent();
            _vm = App.Container.Resolve<IResultViewModel>();
            this.BindingContext = _vm;
        }

        public string MockPreview { get; set; }

        private string _uploadedFlag;
        public string UploadedFlag
        {
            get => _uploadedFlag;
            set
            {
                _uploadedFlag = value;
                ShowUploadedBanner = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool ShowUploadedBanner { get; private set; }

        private string _guestFlag;
        public string GuestFlag
        {
            get => _guestFlag;
            set
            {
                _guestFlag = value;
                IsGuestResult = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool IsGuestResult { get; private set; }

        private string _autoUploadFlag;
        public string AutoUploadFlag
        {
            get => _autoUploadFlag;
            set
            {
                _autoUploadFlag = value;
                AutoUploadRequested = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool AutoUploadRequested { get; private set; }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Sync the uploaded-success banner visibility from the QueryProperty before any
            // early return below (mock-preview path also needs the banner hidden by default).
            if (UploadedBanner != null)
            {
                UploadedBanner.IsVisible = ShowUploadedBanner;
            }

            // Guest banner + hide upload button when this is a guest measurement.
            if (GuestBanner != null)
            {
                GuestBanner.IsVisible = IsGuestResult;
            }
            if (UploadButton != null)
            {
                UploadButton.IsVisible = !IsGuestResult;
            }

#if DEBUG
            if (!string.IsNullOrEmpty(MockPreview))
            {
                LoadMock(MockPreview);
                MockPreview = null;
                return;
            }
#endif

            // Production path: bind from the last captured measurement (App.BodyComposition).
            if (App.BodyComposition != null)
            {
                var scaleType = (ScaleType)Preferences.Get(
                    PreferencesKeys.ScaleType, (byte)ScaleType.MiBodyCompositionScale);
                var user = new User
                {
                    Age = Preferences.Get(PreferencesKeys.UserAge, 25),
                    Height = Preferences.Get(PreferencesKeys.UserHeight, 170),
                    Sex = (Sex)Preferences.Get(PreferencesKeys.UserSex, (byte)Sex.Male),
                };

                if (IsGuestResult)
                {
                    // Guest mode: never read the owner's previous-measurement cache (no leaked
                    // owner deltas into the guest view) and NEVER write the guest result to
                    // that cache (must not pollute the owner's history).
                    // Prefer the guest's own demographics (snapshotted by StartGuestScan) for
                    // status-band coloring, instead of owner prefs.
                    var guestUser = App.LastGuestUser ?? user;
                    _vm.Load(App.BodyComposition, scaleType, guestUser, null);

                    // Bound App.IsGuestMeasurement's lifetime to [StartGuestScan .. ResultPage
                    // rendered], AND clear the global guest BC so it can never be read by FormPage
                    // (Garmin upload) after the flag is reset. The VM has already extracted the
                    // values it needs in Load(), so nulling the global is safe.
                    App.BodyComposition = null;
                    App.IsGuestMeasurement = false;
                    App.LastGuestUser = null;
                }
                else
                {
                    var previous = TryLoadPreviousMeasurement();
                    _vm.Load(App.BodyComposition, scaleType, user, previous);

                    // Persist the CURRENT measurement so the NEXT one can render deltas vs this one.
                    // Only persist valid measurements. Never persist when loaded from DEBUG mock path
                    // or from a guest measurement (handled above).
                    if (App.BodyComposition.IsValid)
                    {
                        TrySavePreviousMeasurement(App.BodyComposition);
                    }

                    if (AutoUploadRequested)
                    {
                        // One-click flow: upload silently in the background. Reset the flag first so a
                        // later OnAppearing (e.g. returning to this page) can't re-trigger an upload.
                        AutoUploadRequested = false;
                        _ = UploadToGarminAsync(true);
                    }
                }
            }
            else
            {
                // Empty state: ResultViewModel.Load(null, ...) handles the no-data UI, but honor
                // the user's real prefs so any future code path reading them gets sane values.
                var scaleType = (ScaleType)Preferences.Get(
                    PreferencesKeys.ScaleType, (byte)ScaleType.MiBodyCompositionScale);
                var user = new User
                {
                    Age = Preferences.Get(PreferencesKeys.UserAge, 25),
                    Height = Preferences.Get(PreferencesKeys.UserHeight, 170),
                    Sex = (Sex)Preferences.Get(PreferencesKeys.UserSex, (byte)Sex.Male),
                };
                _vm.Load(null, scaleType, user);
            }
        }

        private static BodyComposition TryLoadPreviousMeasurement()
        {
            var json = Preferences.Get(PreferencesKeys.PreviousMeasurementJson, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonConvert.DeserializeObject<BodyComposition>(json);
            }
            catch
            {
                // Corrupt/incompatible payload — treat as no previous measurement.
                return null;
            }
        }

        private static void TrySavePreviousMeasurement(BodyComposition composition)
        {
            try
            {
                // Strip large raw byte payloads (ReceivedRawData, RawDataLog) from the cached JSON.
                // They bloat Preferences storage and only the numeric/scalar fields are needed for
                // delta computation. Use JObject so we don't mutate the live App.BodyComposition.
                var jo = JObject.FromObject(composition);
                jo.Remove("ReceivedRawData");
                jo.Remove("RawDataLog");
                Preferences.Set(PreferencesKeys.PreviousMeasurementJson, jo.ToString(Formatting.None));
            }
            catch
            {
                // Never let cache write failures affect the user-visible result screen.
            }
        }

        private async void OnUploadClicked(object sender, EventArgs e) => await UploadToGarminAsync(false);

        private async Task UploadToGarminAsync(bool silentIfNoCreds = false)
        {
#if ANDROID
            try
            {
                if (App.BodyComposition == null) return;
                var email = Preferences.Get(PreferencesKeys.GarminUserEmail, string.Empty);
                var password = await SecureStorage.GetAsync(PreferencesKeys.GarminUserPassword);
                var accessToken = await SecureStorage.GetAsync(PreferencesKeys.GarminUserAccessToken);
                if (silentIfNoCreds && string.IsNullOrEmpty(password) && string.IsNullOrEmpty(accessToken))
                {
                    // No usable credentials (no password and no saved token): skip the silent
                    // auto-upload so we don't surface a failure alert. The user can still tap the
                    // Upload button to upload manually.
                    return;
                }
                SetUploading(true);
                var garmin = App.Container.Resolve<IGarminService>();
                var creds = new YetAnotherGarminConnectClient.Dto.Garmin.Fit.CredentialsData
                {
                    Email = email,
                    Password = password,
                    AccessToken = accessToken,
                    TokenSecret = await SecureStorage.GetAsync(PreferencesKeys.GarminUserTokenSecret),
                };
                var when = App.BodyComposition.Date == default ? DateTime.Now : App.BodyComposition.Date;
                var resp = await garmin.UploadAsync(App.BodyComposition, when, creds);
                if (resp != null && !resp.LocalReceiptSaved)
                {
                    if (resp.IsSuccess)
                    {
                        if (UploadedBanner != null) UploadedBanner.IsVisible = true;
                        if (UploadButton != null) UploadButton.IsVisible = false;
                    }
                    await DisplayAlert(AppSnippets.Response, resp.Message, AppSnippets.OK);
                    return;
                }
                if (resp != null && resp.MFARequested)
                {
                    var code = await DisplayPromptAsync(AppSnippets.GarminEnterMfa, AppSnippets.GarminEnterMfa, keyboard: Keyboard.Numeric);
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        App.BodyComposition.MFACode = code;
                        App.BodyComposition.ExternalApiClientId = resp.ExternalApiClientId;
                        resp = await garmin.UploadAsync(App.BodyComposition, when, creds);
                        App.BodyComposition.MFACode = null;
                        App.BodyComposition.ExternalApiClientId = null;
                    }
                }
                if (resp != null && !resp.LocalReceiptSaved)
                {
                    if (resp.IsSuccess)
                    {
                        if (UploadedBanner != null) UploadedBanner.IsVisible = true;
                        if (UploadButton != null) UploadButton.IsVisible = false;
                    }
                    await DisplayAlert(AppSnippets.Response, resp.Message, AppSnippets.OK);
                    return;
                }
                if (resp != null && resp.IsSuccess)
                {
                    // persist tokens if returned
                    if (!string.IsNullOrEmpty(resp.AccessToken)) await SecureStorage.SetAsync(PreferencesKeys.GarminUserAccessToken, resp.AccessToken);
                    if (!string.IsNullOrEmpty(resp.TokenSecret)) await SecureStorage.SetAsync(PreferencesKeys.GarminUserTokenSecret, resp.TokenSecret);
                    try { await Toast.Make(AppSnippets.UploadedToGarmin).Show(); } catch { }
                    if (UploadedBanner != null) UploadedBanner.IsVisible = true;
                    if (UploadButton != null) UploadButton.IsVisible = false;
                }
                else if (!silentIfNoCreds)
                {
                    await DisplayAlert(AppSnippets.Response, resp?.Message ?? AppSnippets.GarminConnectFailed, AppSnippets.OK);
                }
                else
                {
                    try { await Toast.Make(AppSnippets.GarminConnectFailed).Show(); } catch { }
                }
            }
            catch (Exception ex)
            {
                TryLogNavFailure(ex);
            }
            finally { SetUploading(false); }
#else
            if (!silentIfNoCreds)
            {
                await DisplayAlert("Upload", "Upload runs on device only.", "OK");
            }
#endif
        }

        private void SetUploading(bool uploading)
        {
            // Toggle a lightweight busy state on the upload button. No dedicated spinner exists on
            // this screen, so disabling the button is enough to prevent double taps mid-upload.
            if (UploadButton != null)
            {
                UploadButton.IsEnabled = !uploading;
            }
        }

#if DEBUG
        private void LoadMock(string which)
        {
            var provider = App.Container.Resolve<IMockBodyCompositionProvider>();
            var type = which switch
            {
                "MiSmart" => ScaleType.MiSmartScale,
                "S400" => ScaleType.S400,
                _ => ScaleType.MiBodyCompositionScale,
            };
            var user = provider.GetUser();
            user.ScaleType = type;
            // Mock preview must NOT persist into PreviousMeasurementJson so it can't clobber the
            // user's real previous record.
            _vm.Load(provider.Get(type), type, user, provider.GetPrevious(type));
        }
#endif

        private async void OnShareClicked(object sender, EventArgs e)
        {
            try
            {
                var composition = App.BodyComposition;
                if (composition == null)
                {
                    return;
                }

                var useLbs = Preferences.Get(PreferencesKeys.DisplayWeightInLbs, false);
                var massFactor = useLbs ? KgToLb : 1.0;
                var unit = useLbs ? "lb" : "kg";
                var parts = new List<string>();

                if (composition.Weight > 0)
                {
                    parts.Add($"{(composition.Weight * massFactor).ToString("F1", CultureInfo.InvariantCulture)} {unit}");
                }
                if (composition.BMI > 0)
                {
                    parts.Add($"BMI {composition.BMI.ToString("F1", CultureInfo.InvariantCulture)}");
                }
                if (composition.HasImpedance && composition.Fat > 0)
                {
                    parts.Add($"Body fat {composition.Fat.ToString("F1", CultureInfo.InvariantCulture)}%");
                }
                if (composition.HasImpedance && composition.MuscleMass > 0)
                {
                    parts.Add($"Muscle {(composition.MuscleMass * massFactor).ToString("F1", CultureInfo.InvariantCulture)} {unit}");
                }
                if (composition.HasImpedance && composition.BoneMass > 0)
                {
                    parts.Add($"Bone {(composition.BoneMass * massFactor).ToString("F1", CultureInfo.InvariantCulture)} {unit}");
                }

                if (parts.Count == 0)
                {
                    return;
                }

                var summary = $"My weigh-in: {string.Join(", ", parts)} — via MiScale Exporter";

                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Text = summary,
                    Title = AppSnippets.ShareTitle,
                });
            }
            catch (Exception ex)
            {
                TryLogNavFailure(ex);
            }
        }

        private const double KgToLb = 2.2046226218;

        private static void TryLogNavFailure(Exception ex)
        {
            try
            {
                var log = App.Container.Resolve<ILogService>();
                log?.LogError($"ResultPage nav/share failure: {ex.Message}");
            }
            catch
            {
                // Logger unreachable — swallow so a Shell nav failure can't tear down the app.
            }
        }
    }
}
