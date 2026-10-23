namespace HY.MAUI.Controls;

public partial class BadgeView : ContentView
{
    public BadgeView()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(BadgeView), "0");

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty BadgeColorProperty = BindableProperty.Create(nameof(BadgeColor), typeof(Color), typeof(BadgeView), Colors.Red);

    public Color BadgeColor
    {
        get => (Color)GetValue(BadgeColorProperty);
        set => SetValue(BadgeColorProperty, value);
    }

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(BadgeView), Colors.White);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }
}