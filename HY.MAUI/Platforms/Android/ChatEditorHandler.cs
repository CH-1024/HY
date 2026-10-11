using Android.Content;
using HY.MAUI.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Platforms.Android
{
    public class ChatEditorHandler : EditorHandler
    {
        protected override MauiAppCompatEditText CreatePlatformView()
        {
            return new ChatEditorTextView(Context);
        }

        protected override void ConnectHandler(MauiAppCompatEditText platformView)
        {
            base.ConnectHandler(platformView);

            if (platformView is ChatEditorTextView chatView && VirtualView is ChatEditor chatEditor)
            {
                chatView.SendAction = chatEditor.ExecuteSendCommand;
            }
        }

        protected override void DisconnectHandler(MauiAppCompatEditText platformView)
        {
            if (platformView is ChatEditorTextView chatView)
            {
                chatView.SendAction = null;
            }

            base.DisconnectHandler(platformView);
        }
    }
}
