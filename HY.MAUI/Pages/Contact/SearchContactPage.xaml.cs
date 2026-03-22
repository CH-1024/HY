using HY.MAUI.PageModels.Contact;

namespace HY.MAUI.Pages.Contact;

public partial class SearchContactPage : ContentPage
{
	public SearchContactPage(SearchContactPageModel searchContact)
	{
		InitializeComponent();

        BindingContext = searchContact;
    }
}