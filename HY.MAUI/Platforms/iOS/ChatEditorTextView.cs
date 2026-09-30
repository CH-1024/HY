using Foundation;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace HY.MAUI.Platforms.iOS
{
    public class ChatEditorTextView: MauiTextView
    {
        public Action? SendAction { get; set; }

        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent? evt)
        {
            foreach (var press in presses)
            {
                if (press.Key?.KeyCode == UIKeyboardHidUsage.KeyboardReturnOrEnter)
                {
                    var modifiers = press.Key?.ModifierFlags ?? 0;

                    if (modifiers.HasFlag(UIKeyModifierFlags.Shift))
                    {
                        // Shift + Enter
                        // 不拦截，让 UITextView 正常换行
                        base.PressesBegan(presses, evt);
                        return;
                    }

                    // Enter
                    SendAction?.Invoke();
                    return;
                }
            }

            base.PressesBegan(presses, evt);
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
