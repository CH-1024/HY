using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class VideoPreviewPage : ContentPage
{
    public VideoPreviewPage(VideoPreviewPageModel videoPreview)
    {
        InitializeComponent();

        BindingContext = videoPreview;
    }
}