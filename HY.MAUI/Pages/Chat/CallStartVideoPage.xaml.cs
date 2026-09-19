using HY.MAUI.PageModels.Chat;

namespace HY.MAUI.Pages.Chat;

public partial class CallStartVideoPage : ContentPage
{
	public CallStartVideoPage(CallStartVideoPageModel callStartVideo)
	{
		InitializeComponent();

		BindingContext = callStartVideo;

		callStartVideo._bigCanvas = bigCanvas;
		callStartVideo._smallCanvas = smallCanvas;
    }

    #region 拖动
    private double _startX;
    private double _startY;

    private void OnContentPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _startX = smallCanvas.TranslationX;
                _startY = smallCanvas.TranslationY;
                break;

            case GestureStatus.Running:
                UpdateSmallCanvasPosition(_startX + e.TotalX, _startY + e.TotalY);
                break;
        }
    }

    private void UpdateSmallCanvasPosition(double x, double y)
    {
        if (smallCanvas.Parent is not VisualElement parent)
            return;

        if (parent.Width <= 0 || parent.Height <= 0 ||
            smallCanvas.Width <= 0 || smallCanvas.Height <= 0)
            return;

        double minX = -smallCanvas.X;
        double maxX = parent.Width - smallCanvas.X - smallCanvas.Width;

        double minY = -smallCanvas.Y;
        double maxY = parent.Height - smallCanvas.Y - smallCanvas.Height;

        smallCanvas.TranslationX = Math.Clamp(x, minX, maxX);
        smallCanvas.TranslationY = Math.Clamp(y, minY, maxY);
    }
    #endregion

}