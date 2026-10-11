using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Platforms.Android
{
    public class ChatEditorTextView : MauiAppCompatEditText
    {
        public Action? SendAction { get; set; }

        public ChatEditorTextView(Context context) : base(context)
        {
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
        {
            if (keyCode != Keycode.Enter || e == null)
                return base.OnKeyDown(keyCode, e);

            // Shift + Enter：保留默认行为，正常换行
            if (e.IsShiftPressed)
                return base.OnKeyDown(keyCode, e);

            // Enter 长按或重复事件不重复发送
            if (e.RepeatCount > 0)
                return true;

            SendAction?.Invoke();
            return true;
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent? e)
        {
            if (keyCode == Keycode.Enter && e != null && !e.IsShiftPressed) return true;

            return base.OnKeyUp(keyCode, e);
        }

        public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
        {
            var inputConnection = base.OnCreateInputConnection(outAttrs);

            if (inputConnection == null) return null;

            return new ChatEditorInputConnection(inputConnection, this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) SendAction = null;

            base.Dispose(disposing);
        }
    }

    internal sealed class ChatEditorInputConnection : InputConnectionWrapper
    {
        private readonly ChatEditorTextView _view;

        public ChatEditorInputConnection(IInputConnection target, ChatEditorTextView view) : base(target, false)
        {
            _view = view;
        }

        public override bool SendKeyEvent(KeyEvent? e)
        {
            if (e?.KeyCode == Keycode.Enter)
            {
                // Shift + Enter：交给系统处理换行
                if (e.IsShiftPressed) return base.SendKeyEvent(e);

                // 只在按下时发送，避免 KeyDown + KeyUp 重复发送
                if (e.Action == KeyEventActions.Down)
                {
                    if (e.RepeatCount == 0) _view.SendAction?.Invoke();

                    return true;
                }

                if (e.Action == KeyEventActions.Up) return true;
            }

            return base.SendKeyEvent(e);
        }

        public override bool PerformEditorAction(ImeAction actionCode)
        {
            // 软键盘上的发送 / 完成 / 回车操作
            if (actionCode == ImeAction.Send || actionCode == ImeAction.Done || actionCode == ImeAction.Go || actionCode == ImeAction.Unspecified)
            {
                _view.SendAction?.Invoke();
                return true;
            }

            return base.PerformEditorAction(actionCode);
        }
    }
}
