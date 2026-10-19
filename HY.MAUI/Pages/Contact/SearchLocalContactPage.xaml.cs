using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HY.MAUI.PageModels.Contact;

namespace HY.MAUI.Pages.Contact;

public partial class SearchLocalContactPage : ContentPage
{
    public SearchLocalContactPage(SearchLocalContactPageModel searchLocalContact)
    {
        InitializeComponent();

        BindingContext = searchLocalContact;
    }

    private async void SearchContactPage_OnLoaded(object? sender, EventArgs e)
    {
        if (!searchBar.IsSoftInputShowing())
        {
            await searchBar.ShowSoftInputAsync(CancellationToken.None);
        }
    }
}