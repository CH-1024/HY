using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class CallWaitPage : ContentPage
{
	public CallWaitPage(CallWaitPageModel callWait)
	{
        InitializeComponent();

        BindingContext = callWait;
    }
}