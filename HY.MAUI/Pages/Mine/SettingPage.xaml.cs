using HY.MAUI.PageModels.Mine;

namespace HY.MAUI.Pages.Mine;

public partial class SettingPage : ContentPage
{
	public SettingPage(SettingPageModel setting)
	{
		InitializeComponent();

		BindingContext = setting;
    }

    private void OnToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            Application.Current?.UserAppTheme = AppTheme.Light;
        }
        else
        {
            Application.Current?.UserAppTheme = AppTheme.Dark;
        }
    }

    private void Button_Clicked(object sender, EventArgs e)
    {

    }
}