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
}