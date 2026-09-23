using Autofac;
using MiScaleExporter.MAUI.ViewModels;
using MiScaleExporter.Models;
using System;

namespace MiScaleExporter.MAUI.Views;

public partial class XiaomiLogin : ContentPage
{
    private XiaomiLoginViewModel _viewModel;

    public XiaomiLogin()
    {
        InitializeComponent();

        // Resolve ViewModel from Autofac container using lifetime scope
        using (var scope = App.Container.BeginLifetimeScope())
        {
            _viewModel = scope.Resolve<XiaomiLoginViewModel>();
            BindingContext = _viewModel;
        }

        // Set up PropertyChanged handler for QR code image conversion
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(XiaomiLoginViewModel.QrCodeBytes))
            {
                // Snapshot the bytes. The view model can be restarted before the
                // dispatcher callback runs, and ImageSource may read its stream later.
                var qrCodeBytes = _viewModel.QrCodeBytes;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    QrCodeImage.Source = qrCodeBytes == null || qrCodeBytes.Length == 0
                        ? null
                        : ImageSource.FromStream(() => new MemoryStream(qrCodeBytes, writable: false));
                });
            }
        };
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);

        // Cancel polling when navigating away
        _viewModel?.CancelPolling();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        this.adMobBanner.IsVisible = !Preferences.Get(PreferencesKeys.HideAds, false);
    }
}
