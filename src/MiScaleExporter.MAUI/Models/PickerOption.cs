namespace MiScaleExporter.Models;

public class PickerOption
{
    public string Value { get; set; }
    public string Text { get; set; }

    public PickerOption(string value, string text)
    {
        Value = value;
        Text = text;
    }

    public override string ToString() => Text;
}
