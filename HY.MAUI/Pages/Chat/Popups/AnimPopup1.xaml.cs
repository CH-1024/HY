using CommunityToolkit.Maui.Views;

namespace HY.MAUI.Pages.Chat.Popups;

public partial class AnimPopup1 : Popup
{
    public double TotalTranslateY => PopLength + BottomMargin;
    public double PopLength => 400;
    private double BottomMargin => 80;
    private double DismissThreshold => 100;

    public AnimPopup1()
    {
        InitializeComponent();
    }



    async Task Exit(string? result = null)
    {
        await content.TranslateToAsync(0, TotalTranslateY, easing: Easing.CubicOut);
        await this.CloseAsync();
    }

    async void OnAnimateEntry(object sender, EventArgs e)
    {
        await content.TranslateToAsync(0, BottomMargin, easing: Easing.CubicIn);
    }

    async void OnAnimateExit(object sender, EventArgs e)
    {
        await Exit();
    }

    async void OnAnimateExit(object sender, TappedEventArgs e)
    {
        await Exit();
    }

    #region 拖动
    //private double _startX;
    //private double _startY;

    //private void OnContentPanUpdated(object sender, PanUpdatedEventArgs e)
    //{
    //    switch (e.StatusType)
    //    {
    //        case GestureStatus.Started:
    //            _startX = content.TranslationX;
    //            _startY = content.TranslationY;
    //            break;

    //        case GestureStatus.Running:
    //            content.TranslationX = _startX + e.TotalX;
    //            content.TranslationY = _startY + e.TotalY;
    //            break;

    //        case GestureStatus.Completed:
    //        case GestureStatus.Canceled:
    //            break;
    //    }
    //}
    #endregion

    #region 下滑动画

    private double _startTranslationY;

    private void OnContentPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                // 记录开始拖动时的位置
                _startTranslationY = content.TranslationY;
                break;

            case GestureStatus.Running:
                HandlePanRunning(e.TotalY);
                break;

            case GestureStatus.Completed:
                HandlePanCompleted();
                break;

            case GestureStatus.Canceled:
                HandlePanCanceled();
                break;
        }
    }

    private async void HandlePanRunning(double totalY)
    {
        // e.TotalY：
        // 向下拖 > 0
        // 向上拖 < 0

        // 不允许继续向上拖出屏幕
        if (totalY < 0) return;

        var newY = _startTranslationY + totalY;

        content.TranslationY = newY;
    }

    private async void HandlePanCompleted()
    {
        var diffY = content.TranslationY - _startTranslationY;

        // 向下拖超过阈值 -> 关闭
        if (diffY >= DismissThreshold)
        {
            await Exit();
        }
        else
        {
            // 没超过阈值 -> 弹回原位
            await content.TranslateToAsync(0, _startTranslationY, easing: Easing.CubicOut);
        }
    }

    private async void HandlePanCanceled()
    {
        // 取消 -> 弹回原位
        await content.TranslateToAsync(0, _startTranslationY, easing: Easing.CubicOut);
    }

    #endregion
}