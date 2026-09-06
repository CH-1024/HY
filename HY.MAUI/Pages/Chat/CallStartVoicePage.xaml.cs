using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class CallStartVoicePage : ContentPage
{
	public CallStartVoicePage(CallStartVoicePageModel callStartVoice)
	{
		InitializeComponent();

		BindingContext = callStartVoice;
	}
}