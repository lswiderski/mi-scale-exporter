using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MiScaleExporter.MAUI.Controls
{
    /// <summary>
    /// GraphicsView host for <see cref="CompositionDonutDrawable"/>. Exposes bindable
    /// properties for the four mass components, the center text and theme text color.
    /// </summary>
    public class CompositionDonutView : GraphicsView
    {
        private readonly CompositionDonutDrawable _drawable = new();

        public CompositionDonutView()
        {
            Drawable = _drawable;
            BackgroundColor = Colors.Transparent;
            WidthRequest = 180;
            HeightRequest = 180;
        }

        public static readonly BindableProperty FatProperty = BindableProperty.Create(
            nameof(Fat), typeof(double), typeof(CompositionDonutView), 0d, propertyChanged: OnChanged);

        public static readonly BindableProperty MuscleProperty = BindableProperty.Create(
            nameof(Muscle), typeof(double), typeof(CompositionDonutView), 0d, propertyChanged: OnChanged);

        public static readonly BindableProperty BoneProperty = BindableProperty.Create(
            nameof(Bone), typeof(double), typeof(CompositionDonutView), 0d, propertyChanged: OnChanged);

        public static readonly BindableProperty OtherProperty = BindableProperty.Create(
            nameof(Other), typeof(double), typeof(CompositionDonutView), 0d, propertyChanged: OnChanged);

        public static readonly BindableProperty CenterTextProperty = BindableProperty.Create(
            nameof(CenterText), typeof(string), typeof(CompositionDonutView), string.Empty, propertyChanged: OnChanged);

        public static readonly BindableProperty CenterSubTextProperty = BindableProperty.Create(
            nameof(CenterSubText), typeof(string), typeof(CompositionDonutView), string.Empty, propertyChanged: OnChanged);

        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
            nameof(TextColor), typeof(Color), typeof(CompositionDonutView), Color.FromArgb("#202124"), propertyChanged: OnChanged);

        public double Fat
        {
            get => (double)GetValue(FatProperty);
            set => SetValue(FatProperty, value);
        }

        public double Muscle
        {
            get => (double)GetValue(MuscleProperty);
            set => SetValue(MuscleProperty, value);
        }

        public double Bone
        {
            get => (double)GetValue(BoneProperty);
            set => SetValue(BoneProperty, value);
        }

        public double Other
        {
            get => (double)GetValue(OtherProperty);
            set => SetValue(OtherProperty, value);
        }

        public string CenterText
        {
            get => (string)GetValue(CenterTextProperty);
            set => SetValue(CenterTextProperty, value);
        }

        public string CenterSubText
        {
            get => (string)GetValue(CenterSubTextProperty);
            set => SetValue(CenterSubTextProperty, value);
        }

        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        private static void OnChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var view = (CompositionDonutView)bindable;
            view.Sync();
            view.Invalidate();
        }

        private void Sync()
        {
            _drawable.Fat = Fat;
            _drawable.Muscle = Muscle;
            _drawable.Bone = Bone;
            _drawable.Other = Other;
            _drawable.CenterText = CenterText;
            _drawable.CenterSubText = CenterSubText;
            _drawable.TextColor = TextColor ?? Color.FromArgb("#202124");
        }
    }
}
