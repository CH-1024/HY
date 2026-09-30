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
            chatView.SendAction = chatEditor.ExecuteSendCommand;
        }

        // 移除 WinUI 的底部强调线
        platformView.Resources["TextControlBorderBrushFocused"] = null;
        platformView.Resources["TextControlBorderBrushPointerOver"] = null;
        platformView.Resources["TextControlBorderBrush"] = null;
    }

    protected override void DisconnectHandler(TextBox platformView)
    {
        if (platformView is ChatEditorTextView chatView)
        {
            chatView.SendAction = null;
        }

        base.DisconnectHandler(platformView);
    }
}