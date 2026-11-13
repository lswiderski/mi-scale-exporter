#nullable enable
namespace MiScaleExporter.MAUI;

// Application.MainPage is obsolete in .NET 10; route alerts through the
// active window's page instead. Keeps a single entry point for UI dialogs.
public static class Dialog
{
    private static Page? CurrentPage =>
        Application.Current?.Windows.FirstOrDefault()?.Page;

    public static Task AlertAsync(string title, string message, string cancel = "OK")
    {
        var page = CurrentPage;
        if (page is null)
        {
            return Task.CompletedTask;
        }

        return MainThread.InvokeOnMainThreadAsync(() => page.DisplayAlertAsync(title, message, cancel));
    }
}
