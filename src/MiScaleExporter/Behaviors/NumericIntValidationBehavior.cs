using Xamarin.Forms;

namespace MiScaleExporter.Behaviors;

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
        var defaultColor = Color.Black;
        ((Entry)sender).TextColor = isValid ? defaultColor : Color.Red;
    }
}