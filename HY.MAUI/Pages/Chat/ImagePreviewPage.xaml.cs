using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class ImagePreviewPage : ContentPage
{
	public ImagePreviewPage(ImagePreviewPageModel imagePreview)
	{
		InitializeComponent();

        BindingContext = imagePreview;
    }

    private async void Image_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..", false);
    }
}