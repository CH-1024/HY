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

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView && collectionView.SelectedItem != null)
        {
            if (BindingContext is ContactPageModel contactPage)
            {
                contactPage.SelectionChangedCommand.Execute(collectionView.SelectedItem);
            }
            // 立即清除视觉选中状态
            collectionView.SelectedItem = null;
        }
    }
}