 

namespace MiScaleExporter.MAUI.Behaviors;

public class NumericIntValidationBehavior : Behavior<Entry>
{
    protected override void OnAttachedTo(Entry entry)
    {
        entry.TextChanged += OnEntryTextChanged;
        base.OnAttachedTo(entry);
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnEntryTextChanged;
        base.OnDetachingFrom(entry);
    }

    void OnEntryTextChanged(object sender, TextChangedEventArgs args)
    {
        bool isValid = int.TryParse(args.NewTextValue, out _);
        var defaultColor = Application.Current.RequestedTheme == AppTheme.Dark ? Color.FromRgb(255, 255, 255) : Color.FromRgb(0, 0, 0);
        ((Entry)sender).TextColor = isValid ? defaultColor : Color.FromRgb(255, 0, 0);
    }
}