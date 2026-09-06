using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class CallStartVideoPage : ContentPage
{
	public CallStartVideoPage(CallStartVideoPageModel callStartVideo)
	{
		InitializeComponent();

		BindingContext = callStartVideo;
	}
}