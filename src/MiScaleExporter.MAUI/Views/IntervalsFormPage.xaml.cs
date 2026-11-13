using Autofac;
using MiScaleExporter.MAUI.ViewModels;
using MiScaleExporter.Models;

namespace MiScaleExporter.MAUI.Views
{
    [QueryProperty(nameof(AutoUpload), "autoUpload")]
    [QueryProperty(nameof(ContinueTo), "continueTo")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class IntervalsFormPage : ContentPage
    {
        private IIntervalsFormViewModel vm;
        private bool _autoUploadPending;

        public IntervalsFormPage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = vm = scope.Resolve<IIntervalsFormViewModel>();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await vm.LoadPreferencesAsync();
            vm.LoadBodyComposition();
            if (_autoUploadPending)
            {
                _autoUploadPending = false;
                _ = Task.Run(async () =>
                {
                    await vm.AutoUploadAsync();
                    if (!string.IsNullOrWhiteSpace(_continueTo) && _continueTo.Equals("garmin", StringComparison.OrdinalIgnoreCase))
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Shell.Current.GoToAsync("//FormPage?autoUpload=true");
                        });
                    }
                });
            }
        }

        private string _continueTo;

        public string ContinueTo
        {
            get => _continueTo;
            set => _continueTo = value;
        }

        public bool AutoUpload
        {
            set
            {
                if (value)
                {
                    _autoUploadPending = true;
                }
            }
        }
    }
}
