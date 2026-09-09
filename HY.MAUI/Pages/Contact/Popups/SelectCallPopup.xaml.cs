using CommunityToolkit.Maui.Views;

namespace HY.MAUI.Pages.Contact.Popups;

public partial class SelectCallPopup : Popup<string>
{
    //// 弹窗高度
    public double PopLength => 250;
    public uint length => 250;

    public SelectCallPopup()
    {
        InitializeComponent();
    }

    async void OnAnimateEntry(object sender, EventArgs e)
    {
        await content.TranslateToAsync(0, 0, length, Easing.CubicIn);
    }

    private async void OnAnimateExit(object sender, TappedEventArgs e)
    {
        await content.TranslateToAsync(0, PopLength, length, Easing.CubicOut);
        await this.CloseAsync();
    }





    private async void Voice_Clicked(object sender, EventArgs e)
    {
        await content.TranslateToAsync(0, PopLength, length, Easing.CubicOut);
        await this.CloseAsync("Voice");
    }

    private async void Video_Clicked(object sender, EventArgs e)
    {
        await content.TranslateToAsync(0, PopLength, length, Easing.CubicOut);
        await this.CloseAsync("Video");
    }
}