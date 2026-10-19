using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class SearchLocalChatPage : ContentPage
{
    public SearchLocalChatPage(SearchLocalChatPageModel searchLocalChat)
    {
        InitializeComponent();

        BindingContext = searchLocalChat;
    }

    private async void SearchChatPage_OnLoaded(object? sender, EventArgs e)
    {
        if (!searchBar.IsSoftInputShowing())
        {
            await searchBar.ShowSoftInputAsync(CancellationToken.None);
        }
    }
}