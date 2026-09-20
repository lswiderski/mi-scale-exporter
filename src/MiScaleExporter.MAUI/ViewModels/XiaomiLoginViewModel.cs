using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MiScaleExporter.Services;
using Microsoft.Maui.Controls;

namespace MiScaleExporter.MAUI.ViewModels
{
    public class XiaomiLoginViewModel : BaseViewModel
    {
        private readonly XiaomiService _xiaomiService;
        private CancellationTokenSource _pollingCancellationTokenSource;
        private Task _pollingTask;
        private int _restartInProgress;
        private int _loginGeneration;

        private byte[] _qrCodeBytes;
        public byte[] QrCodeBytes
        {
            get { return _qrCodeBytes; }
            set { SetProperty(ref _qrCodeBytes, value); }
        }

        private string _loginUrl;
        public string LoginUrl
        {
            get { return _loginUrl; }
            set { SetProperty(ref _loginUrl, value); }
        }

        private string _loginStatus = "Ready";
        public string LoginStatus
        {
            get { return _loginStatus; }
            set { SetProperty(ref _loginStatus, value); }
        }

        private bool _isPolling;
        public bool IsPolling
        {
            get { return _isPolling; }
            set { SetProperty(ref _isPolling, value); }
        }

        public ICommand GetPassTokenCommand { get; }
        public ICommand RestartLoginCommand { get; }
        public ICommand OpenLoginUrlCommand { get; }

        public XiaomiLoginViewModel(XiaomiService xiaomiService)
        {
            _xiaomiService = xiaomiService ?? throw new ArgumentNullException(nameof(xiaomiService));
            Title = "Xiaomi Login";

            GetPassTokenCommand = new Command(async () => await StartLoginAsync());
            RestartLoginCommand = new Command(async () => await RestartLoginAsync());
            OpenLoginUrlCommand = new Command(async () => await OpenLoginUrl());
        }

        private Task StartLoginAsync()
        {
            var generation = Volatile.Read(ref _loginGeneration);
            return HandleGetPassTokenClickAsync(generation);
        }

        private async Task HandleGetPassTokenClickAsync(int generation)
        {
            using var cancellationSource = new CancellationTokenSource();
            _pollingCancellationTokenSource = cancellationSource;

            try
            {
                IsBusy = true;
                IsPolling = true;
                LoginStatus = "Getting QR code...";

                // Call LoginAsync to get QR code and login URL
                var loginResult = await _xiaomiService.LoginAsync();

                if (!IsCurrentGeneration(generation))
                {
                    return;
                }

                QrCodeBytes = loginResult.QrCodeBytes;
                LoginUrl = loginResult.LoginUrl;

                LoginStatus = "QR code ready. Scan with Xiaomi app or click the link below.";

                // Start polling for login status
                _pollingTask = CheckLoginStatusLoop(cancellationSource.Token, generation);
                await _pollingTask;
            }
            catch (Exception ex)
            {
                if (IsCurrentGeneration(generation))
                {
                    LoginStatus = $"Error: {ex.Message}";
                    IsPolling = false;
                    await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
                }
            }
            finally
            {
                if (ReferenceEquals(_pollingCancellationTokenSource, cancellationSource))
                {
                    _pollingCancellationTokenSource = null;
                    IsBusy = false;
                }
            }
        }

        private async Task RestartLoginAsync()
        {
            // Ignore double taps while the old flow is being shut down.
            if (Interlocked.Exchange(ref _restartInProgress, 1) != 0)
            {
                return;
            }

            try
            {
            Interlocked.Increment(ref _loginGeneration);
            CancelPolling();

            QrCodeBytes = null;
            LoginUrl = null;
            LoginStatus = "Starting a new login...";

            // The new login owns a long-running polling task. Do not await it here,
            // otherwise _restartInProgress remains set until login completes and
            // all subsequent Restart clicks are ignored.
            _ = StartLoginAsync();
            }
            finally
            {
                Volatile.Write(ref _restartInProgress, 0);
            }
        }

        private bool IsCurrentGeneration(int generation) => generation == Volatile.Read(ref _loginGeneration);

        private async Task CheckLoginStatusLoop(CancellationToken cancellationToken, int generation)
        {
            try
            {
                // Poll every 2 seconds
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var statusResponse = await _xiaomiService.CheckLoginStatusAsync();

                        if (statusResponse.Status == "completed")
                        {
                            if (!IsCurrentGeneration(generation))
                            {
                                break;
                            }

                            LoginStatus = "Login completed successfully!";
                            IsPolling = false;

                            await Application.Current.MainPage.DisplayAlert(
                                "Success",
                                "You have been successfully logged in.",
                                "OK");

                            // Navigate back
                            await Shell.Current.GoToAsync("Settings");
                            break;
                        }
                        else
                        {
                            if (IsCurrentGeneration(generation))
                            {
                                LoginStatus = "Waiting for QR code scan...";
                            }
                        }

                        // Wait before next poll
                        await Task.Delay(2000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (IsCurrentGeneration(generation))
                {
                    LoginStatus = $"Polling error: {ex.Message}";
                    await Application.Current.MainPage.DisplayAlert("Polling Error", ex.Message, "OK");
                }
            }
            finally
            {
                if (IsCurrentGeneration(generation))
                {
                    IsPolling = false;
                }
            }
        }

        private async Task OpenLoginUrl()
        {
            if (string.IsNullOrWhiteSpace(LoginUrl))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Login URL is not available.", "OK");
                return;
            }

            try
            {
                // Open the URL in the default browser
                await Launcher.Default.OpenAsync(new Uri(LoginUrl));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open URL: {ex.Message}", "OK");
            }
        }

        public void CancelPolling()
        {
            try
            {
                _pollingCancellationTokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The login flow has already completed.
            }
        }
    }
}
