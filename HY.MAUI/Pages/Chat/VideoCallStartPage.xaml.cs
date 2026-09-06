using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class VideoCallStartPage : ContentPage
{
	public VideoCallStartPage(VideoCallStartPageModel videoCallStart)
	{
		InitializeComponent();

		BindingContext = videoCallStart;
	}
}