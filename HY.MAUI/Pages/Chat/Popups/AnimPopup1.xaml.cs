using CommunityToolkit.Maui.Views;

namespace HY.MAUI.Pages.Chat.Popups;

public partial class AnimPopup1 : Popup
{
    // µ¯´°¸ß¶È
    public double PopLength => 400;

    public AnimPopup1()
    {
        InitializeComponent();
    }

    async void OnAnimateEntry(object sender, EventArgs e)
    {
        await content.TranslateToAsync(0, 0, 150, Easing.CubicIn);
    }

    async void OnAnimateExit(object sender, EventArgs e)
    {
        await content.TranslateToAsync(0, PopLength, 150, Easing.CubicOut);
        await this.CloseAsync();
    }

    async void OnAnimateExit(object sender, TappedEventArgs e)
    {
        await content.TranslateToAsync(0, PopLength, 150, Easing.CubicOut);
        await this.CloseAsync();
    }
}