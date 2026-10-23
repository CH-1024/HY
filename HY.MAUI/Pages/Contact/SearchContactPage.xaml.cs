using HY.MAUI.PageModels.Contact;

namespace HY.MAUI.Pages.Contact;

public partial class SearchContactPage : ContentPage
{
    public SearchContactPage(SearchContactPageModel searchContact)
    {
        InitializeComponent();

        BindingContext = searchContact;
    }

    private async void SearchContactPage_OnLoaded(object? sender, EventArgs e)
    {
        if (!searchBar.IsSoftInputShowing())
        {
            await searchBar.ShowSoftInputAsync(CancellationToken.None);
        }
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        if (searchBar.IsSoftInputShowing())
        {
            await searchBar.HideSoftInputAsync(CancellationToken.None);
        }
    }

}