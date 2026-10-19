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

    private async void SearchBar_OnFocused(object? sender, FocusEventArgs e)
    {
        if (searchBar.IsSoftInputShowing())
        {
            await searchBar.HideSoftInputAsync(CancellationToken.None);
        }

        await Shell.Current.GoToAsync(nameof(SearchLocalContactPage), false);
    }
}