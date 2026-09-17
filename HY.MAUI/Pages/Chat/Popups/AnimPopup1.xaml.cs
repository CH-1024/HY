using CommunityToolkit.Maui.Views;

namespace HY.MAUI.Pages.Chat.Popups;

public partial class AnimPopup1 : Popup
{
    // 弹窗高度
    public double PopLength => 400;

    public AnimPopup1()
    {
        InitializeComponent();
    }

    async void OnAnimateEntry(object sender, EventArgs e)
    {
        await content.TranslateToAsync(0, 0, easing: Easing.CubicIn);
    }

    async void OnAnimateExit(object sender, EventArgs e)
    {
        await content.TranslateToAsync(0, PopLength, easing: Easing.CubicOut);
        await this.CloseAsync();
    }

    async void OnAnimateExit(object sender, TappedEventArgs e)
    {
        await content.TranslateToAsync(0, PopLength, easing: Easing.CubicOut);
        await this.CloseAsync();
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
    private double _panStartY;

    private const double DismissThreshold = 150;

    private void OnContentPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                // 记录开始拖动时的位置
                _startTranslationY = content.TranslationY;
                _panStartY = 0;
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
        var newY = _startTranslationY + totalY;

        // 不允许继续向上拖出屏幕
        newY = Math.Max(0, newY);

        content.TranslationY = newY;
    }

    private async void HandlePanCompleted()
    {
        var currentY = content.TranslationY;

        // 向下拖超过阈值 -> 关闭
        if (currentY >= DismissThreshold)
        {
            await CloseByDrag();
        }
        else
        {
            // 没超过阈值 -> 弹回原位
            await content.TranslateToAsync(0, _panStartY, easing: Easing.CubicOut);
        }
    }

    private async void HandlePanCanceled()
    {
        // 取消 -> 弹回原位
        await content.TranslateToAsync(0, _panStartY, easing: Easing.CubicOut);
    }

    private async Task CloseByDrag()
    {
        // 直接滑到底部
        await content.TranslateToAsync(0, PopLength, easing: Easing.CubicIn);

        await this.CloseAsync();
    }

    #endregion
}