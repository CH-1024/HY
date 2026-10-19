using HY.MAUI.PageModels.Chat;
using HY.MAUI.PageModels.Login;

namespace HY.MAUI.Pages.Chat;

public partial class ChatPage : ContentPage
{
    public ChatPage(ChatPageModel chat)
    {
        InitializeComponent();

        BindingContext = chat;
    }

    private async void SearchBar_OnFocused(object? sender, FocusEventArgs e)
    {
        if (searchBar.IsSoftInputShowing())
        {
            await searchBar.HideSoftInputAsync(CancellationToken.None);
        }

        await Shell.Current.GoToAsync(nameof(SearchLocalChatPage), false);
    }
}