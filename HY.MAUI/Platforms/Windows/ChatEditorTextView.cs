namespace HY.MAUI.Platforms.Windows;

public class ChatEditorTextView : TextBox
{
    public Action? SendAction { get; set; }

    public void HandlePreviewKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        // Shift + Enter：正常换行
        if (IsShiftDown())
            return;

        // 阻止 TextBox 默认换行
        e.Handled = true;

        // 长按忽略
        if (e.KeyStatus.WasKeyDown)
            return;

        SendAction?.Invoke();
    }

    private static bool IsShiftDown()
    {
        return Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        HandlePreviewKeyDown(e);

        if (!e.Handled)
            base.OnKeyDown(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SendAction = null;
        }

        base.Dispose(disposing);
    }
}