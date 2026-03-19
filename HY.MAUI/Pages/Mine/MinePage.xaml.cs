using HY.MAUI.PageModels.Login;
using HY.MAUI.PageModels.Mine;
using HY.MAUI.Pages.Login;

namespace HY.MAUI.Pages.Mine;

public partial class MinePage : ContentPage
{
	public MinePage(MinePageModel mine)
	{
		InitializeComponent();

		BindingContext = mine;
    }
}