using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HY.MAUI.Models;

namespace HY.MAUI.PageModels.Chat;

public class SearchLocalChatPageModel : ObservableObject
{

    private ObservableCollection<ChatVM> chatCollection = null;
    public ObservableCollection<ChatVM> ChatCollection
    {
        get { return chatCollection; }
        set { SetProperty(ref chatCollection, value); }
    }

    private ChatVM? selectedChat = null;
    public ChatVM? SelectedChat
    {
        get { return selectedChat; }
        set { SetProperty(ref selectedChat, value); }
    }


    public SearchLocalChatPageModel()
    {

    }

}