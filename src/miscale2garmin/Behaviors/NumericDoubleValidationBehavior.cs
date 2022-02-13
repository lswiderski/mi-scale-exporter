using Xamarin.Essentials;
using Xamarin.Forms;

namespace miscale2garmin.Behaviors;

public class NumericDoubleValidationBehavior : Behavior<Entry>
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
        bool isValid = double.TryParse(args.NewTextValue, out _);
        var defaultColor = Color.Black;
        ((Entry)sender).TextColor = isValid ? defaultColor : Color.Red;
    }
}