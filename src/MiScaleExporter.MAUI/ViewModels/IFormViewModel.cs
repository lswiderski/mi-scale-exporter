namespace MiScaleExporter.MAUI.ViewModels
{
    public interface IFormViewModel
    {
        void LoadBodyComposition();
        Task LoadPreferencesAsync();

        void AutoUpload();
    }
}