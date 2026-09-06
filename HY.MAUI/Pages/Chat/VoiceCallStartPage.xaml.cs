using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class VoiceCallStartPage : ContentPage
{
	public VoiceCallStartPage(VoiceCallStartPageModel voiceCallStart)
	{
		InitializeComponent();

		BindingContext = voiceCallStart;
	}
}