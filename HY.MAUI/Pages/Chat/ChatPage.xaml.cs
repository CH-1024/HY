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

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView && collectionView.SelectedItem != null)
        {
            if (BindingContext is ChatPageModel chatPage)
            {
                chatPage.SelectionChangedCommand.Execute(collectionView.SelectedItem);
            }
            // 立即清除视觉选中状态
            collectionView.SelectedItem = null;
        }
    }

}