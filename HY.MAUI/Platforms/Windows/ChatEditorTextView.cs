using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace HY.MAUI.Platforms.Windows;

public class ChatEditorTextView : TextBox
{
    public Action? SendAction { get; set; }

    public void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        // Shift + Enter
        // 不处理，让 TextBox 正常换行
        if (IsShiftDown())
            return;

        // Enter
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
            .HasFlag(global::Windows.UI.Core.CoreVirtualKeyStates.Down);
    }
}