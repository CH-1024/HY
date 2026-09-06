using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class CallCreatePage : ContentPage
{
	public CallCreatePage(CallCreatePageModel callCreate)
	{
        InitializeComponent();

        BindingContext = callCreate;
    }
}