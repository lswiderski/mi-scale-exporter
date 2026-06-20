using Autofac;
using MiScaleExporter.MAUI.ViewModels;

namespace MiScaleExporter.MAUI.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OnboardingPage : ContentPage
    {
        private OnboardingViewModel vm;

        public OnboardingPage()
        {
            InitializeComponent();
            this.BindingContext = vm = App.Container.Resolve<OnboardingViewModel>();
        }

        private void SexRadioSetToMale(object sender, EventArgs e)
        {
            vm.SexRadioSetToMale();
        }

        private void SexRadioSetToFemale(object sender, EventArgs e)
        {
            vm.SexRadioSetToFemale();
        }

        private void AgeModeSetToManual(object sender, EventArgs e)
        {
            vm.AgeModeSetToManual();
        }

        private void AgeModeSetToBirthDate(object sender, EventArgs e)
        {
            vm.AgeModeSetToBirthDate();
        }

        private void ScaleTypeSetToBodyCompositionScale(object sender, EventArgs e)
        {
            vm.ScaleTypeSetToBodyCompositionScale();
        }

        private void ScaleTypeSetToMiscale(object sender, EventArgs e)
        {
            vm.ScaleTypeSetToMiscale();
        }

        private void ScaleTypeSetToS400(object sender, EventArgs e)
        {
            vm.ScaleTypeSetToS400();
        }
    }
}
