using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class CallChoosePage : ContentPage
{
	public CallChoosePage(CallChoosePageModel callChoose)
	{
		InitializeComponent();

		BindingContext = callChoose;
	}
}