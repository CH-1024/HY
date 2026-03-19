using HY.MAUI.PageModels.Login;

namespace HY.MAUI.Pages.Login;

public partial class LoadingPage : ContentPage
{
	public LoadingPage(LoadingPageModel loading)
	{
		InitializeComponent();

		BindingContext = loading;
    }
}