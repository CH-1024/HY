using HY.MAUI.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace HY.MAUI.Platforms.MacCatalyst
{
    public class ChatEditorHandler : EditorHandler
    {
        protected override MauiTextView CreatePlatformView()
        {
            return new ChatEditorTextView();
        }

        protected override void ConnectHandler(MauiTextView platformView)
        {
            base.ConnectHandler(platformView);

            if (platformView is ChatEditorTextView chatView && VirtualView is ChatEditor chatEditor)
            {
                chatView.SendAction = chatEditor.ExecuteSendCommand;
            }
        }

        protected override void DisconnectHandler(MauiTextView platformView)
        {
            if (platformView is ChatEditorTextView chatView)
            {
                chatView.SendAction = null;
            }

            base.DisconnectHandler(platformView);
        }
    }
}
