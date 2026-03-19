namespace HY.MAUI.Pages.Chat.Controls;

public partial class ChatBubble : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(ChatBubble), string.Empty, propertyChanged: OnTextChanged);
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty IsSelfProperty = BindableProperty.Create(nameof(IsSelf), typeof(bool), typeof(ChatBubble), null, propertyChanged: OnDirectionChanged);
    public bool? IsSelf
    {
        get => (bool?)GetValue(IsSelfProperty);
        set => SetValue(IsSelfProperty, value);
    }

    public static readonly BindableProperty AvatarProperty = BindableProperty.Create(nameof(Avatar), typeof(ImageSource), typeof(ChatBubble));
    public ImageSource Avatar
    {
        get => (ImageSource)GetValue(AvatarProperty);
        set => SetValue(AvatarProperty, value);
    }




    public ChatBubble()
    {
        InitializeComponent();
    }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((ChatBubble)bindable).MessageEditor.Text = newValue?.ToString();
    }

    private static void OnDirectionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((ChatBubble)bindable).UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (IsSelf == true)
        {
            // ×Ô¼º£ºÓÒ²à
            //Grid.SetColumn(Bubble, 1);
            Bubble.HorizontalOptions = LayoutOptions.End;

            LeftAvatar.IsVisible = false;
            RightAvatar.IsVisible = true;
            RightAvatar.Source = Avatar;

            // Î¢ÐÅÓÒÆøÅÝÔ²½Ç
            BubbleRadius.CornerRadius = new CornerRadius(12, 12, 0, 12);
        }
        else
        {
            // ¶Ô·½£º×ó²à
            //Grid.SetColumn(Bubble, 1);
            Bubble.HorizontalOptions = LayoutOptions.Start;

            LeftAvatar.IsVisible = true;
            RightAvatar.IsVisible = false;
            LeftAvatar.Source = Avatar;

            // Î¢ÐÅ×óÆøÅÝÔ²½Ç
            BubbleRadius.CornerRadius = new CornerRadius(12, 12, 12, 0);
        }
    }
}