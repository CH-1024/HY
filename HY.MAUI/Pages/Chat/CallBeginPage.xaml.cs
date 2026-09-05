using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class CallBeginPage : ContentPage
{
	public CallBeginPage(CallBeginPageModel callBegin)
	{
		InitializeComponent();

		BindingContext = callBegin;
	}
}