using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class CallSelectPage : ContentPage
{
	public CallSelectPage(CallSelectPageModel callSelect)
	{
		InitializeComponent();

		BindingContext = callSelect;
	}
}