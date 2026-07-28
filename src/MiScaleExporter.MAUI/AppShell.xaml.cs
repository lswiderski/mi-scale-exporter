using MiScaleExporter.MAUI.Views;

namespace MiScaleExporter.MAUI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("ResultPage", typeof(ResultPage));

#if DEBUG
            AddDebugPreviewMenuItems();
#endif
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

#if DEBUG
        private void AddDebugPreviewMenuItems()
        {
            void AddMenuItem(string title, string mock)
            {
                var mi = new MenuItem { Text = title };
                mi.Clicked += async (_, __) =>
                    await Shell.Current.GoToAsync($"ResultPage?mock={mock}");
                this.Items.Add(mi);
            }

            AddMenuItem("DEBUG: Preview MiSmart result", "MiSmart");
            AddMenuItem("DEBUG: Preview MiBody result", "MiBody");
            AddMenuItem("DEBUG: Preview S400 result", "S400");
        }
#endif
    }
}