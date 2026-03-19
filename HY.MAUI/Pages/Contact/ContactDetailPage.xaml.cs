using HY.MAUI.PageModels.Contact;

namespace HY.MAUI.Pages.Contact;

public partial class ContactDetailPage : ContentPage
{
	public ContactDetailPage(ContactDetailPageModel contactDetail)
	{
		InitializeComponent();

        BindingContext = contactDetail;
    }

    private void Button_Clicked(object sender, EventArgs e)
    {

    }
}