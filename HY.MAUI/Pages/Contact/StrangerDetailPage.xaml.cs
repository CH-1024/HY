using HY.MAUI.PageModels.Contact;

namespace HY.MAUI.Pages.Contact;

public partial class StrangerDetailPage : ContentPage
{
	public StrangerDetailPage(StrangerDetailPageModel strangerDetail)
    {
        InitializeComponent();

        BindingContext = strangerDetail;
    }
}