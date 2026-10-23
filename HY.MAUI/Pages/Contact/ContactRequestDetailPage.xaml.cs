using HY.MAUI.PageModels.Contact;

namespace HY.MAUI.Pages.Contact;

public partial class ContactRequestDetailPage : ContentPage
{
	public ContactRequestDetailPage(ContactRequestDetailPageModel contactRequestDetail)
	{
		InitializeComponent();

		BindingContext = contactRequestDetail;
    }
}