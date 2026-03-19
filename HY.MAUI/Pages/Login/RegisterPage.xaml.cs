using HY.MAUI.PageModels.Login;

namespace HY.MAUI.Pages.Login;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterPageModel register)
	{
		InitializeComponent();

        BindingContext = register;
    }
}