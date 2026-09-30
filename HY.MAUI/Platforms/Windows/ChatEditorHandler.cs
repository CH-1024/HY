using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;

namespace HY.MAUI.Platforms.Windows;

public class ChatEditorHandler : EditorHandler
{
    protected override TextBox CreatePlatformView()
    {
        return new ChatEditorTextView();
    }

    protected override void ConnectHandler(TextBox platformView)
    {
        base.ConnectHandler(platformView);

        if (platformView is ChatEditorTextView chatView && VirtualView is Controls.ChatEditor chatEditor)
        {
            chatView.AcceptsReturn = true;

            chatView.SendAction = chatEditor.ExecuteSendCommand;

            chatView.PreviewKeyDown += chatView.OnPreviewKeyDown;
        }

        // 移除 WinUI 底部强调线
        platformView.Resources["TextControlBorderBrushFocused"] = null;
        platformView.Resources["TextControlBorderBrushPointerOver"] = null;
        platformView.Resources["TextControlBorderBrush"] = null;
    }

    protected override void DisconnectHandler(TextBox platformView)
    {
        if (platformView is ChatEditorTextView chatView)
        {
            chatView.PreviewKeyDown -= chatView.OnPreviewKeyDown;
            chatView.SendAction = null;
        }

        base.DisconnectHandler(platformView);
    }
}