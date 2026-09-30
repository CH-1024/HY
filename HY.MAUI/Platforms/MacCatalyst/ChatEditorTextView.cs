using Foundation;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace HY.MAUI.Platforms.MacCatalyst
{
    public class ChatEditorTextView : MauiTextView
    {
        public Action? SendAction { get; set; }

        public override UIKeyCommand[] KeyCommands
        {
            get
            {
                return new[]
                {
                    // Enter
                    UIKeyCommand.Create((NSString)"\r", (UIKeyModifierFlags)0, new ObjCRuntime.Selector("chat_enter:")),

                    // Shift + Enter
                    UIKeyCommand.Create((NSString)"\r", UIKeyModifierFlags.Shift, new ObjCRuntime.Selector("chat_shift_enter:"))
                };
            }
        }

        [Foundation.Export("chat_enter:")]
        private void ChatEnter(UIKeyCommand command)
        {
            // 执行发送
            SendAction?.Invoke();
        }

        [Foundation.Export("chat_shift_enter:")]
        private void ChatShiftEnter(UIKeyCommand command)
        {
            // 手动插入换行
            InsertText("\n");
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
}
