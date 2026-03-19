using HY.MAUI.PageModels.Mine;

namespace HY.MAUI.Pages.Mine;

public partial class ProfilePage : ContentPage
{
	public ProfilePage(ProfilePageModel profile)
	{
		InitializeComponent();

		BindingContext = profile;
    }

    private void Button_Clicked(object sender, EventArgs e)
    {

    }
}