using HY.MAUI.PageModels.Contact;
using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Contact;

public partial class ContactPage : ContentPage
{
	public ContactPage(ContactPageModel contact)
	{
		InitializeComponent();

		BindingContext = contact;
    }
}