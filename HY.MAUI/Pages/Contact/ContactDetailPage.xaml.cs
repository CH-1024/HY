using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using HY.MAUI.PageModels.Chat;
using HY.MAUI.PageModels.Contact;
using HY.MAUI.Pages.Contact.Popups;

namespace HY.MAUI.Pages.Contact;

public partial class ContactDetailPage : ContentPage
{
	public ContactDetailPage(ContactDetailPageModel contactDetail)
	{
		InitializeComponent();

        BindingContext = contactDetail;
    }

    private async void SelectCall_Clicked(object sender, EventArgs e)
    {
        var option = new PopupOptions
        {
            // 背景色
            PageOverlayColor = Colors.Transparent,

            Shadow = null,
            Shape = null,

            // 边框形状
            //Shape = new RoundRectangle
            //{
            //    CornerRadius = new CornerRadius(5, 5, 0, 0),
            //    Stroke = Colors.Blue,
            //    StrokeThickness = 4
            //},

            // 边框阴影
            //Shadow = new Shadow
            //{
            //    Brush = Brush.Green,
            //    Opacity = 0.8f
            //}
        };

        var pop = new SelectCallPopup();
        var result = await this.ShowPopupAsync<string>(pop, option, CancellationToken.None);
        if (result.WasDismissedByTappingOutsideOfPopup)
        {
            return;
        }
        if (result.Result == "Voice")
        {
            if (BindingContext is ContactDetailPageModel vm)
            {
                vm.CreateVoiceCallCommand.Execute(null);
            }
        }
        if (result.Result == "Video")
        {
            if (BindingContext is ContactDetailPageModel vm)
            {
                vm.CreateVideoCallCommand.Execute(null);
            }
        }
    }
}