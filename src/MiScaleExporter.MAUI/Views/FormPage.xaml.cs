
using Autofac;

using MiScaleExporter.MAUI.ViewModels;


namespace MiScaleExporter.MAUI.Views
{
    [QueryProperty(nameof(AutoUpload), "autoUpload")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FormPage : ContentPage
    {
        private IFormViewModel vm;
        public FormPage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = vm = scope.Resolve<IFormViewModel>();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            vm.LoadBodyComposition();
        }

        public bool AutoUpload
        {
            set
            {
                if (value)
                {
                    vm.AutoUpload();
                }
            }
        }
      
    }
}