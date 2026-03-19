using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace HY.MAUI.Controls
{
    public class ChatEditor : Editor
    {
        public static readonly BindableProperty SendCommandProperty = BindableProperty.Create(nameof(SendCommand), typeof(ICommand), typeof(ChatEditor));

        public ICommand? SendCommand
        {
            get => (ICommand?)GetValue(SendCommandProperty);
            set => SetValue(SendCommandProperty, value);
        }

        public static readonly BindableProperty SendCommandParameterProperty = BindableProperty.Create(nameof(SendCommandParameter), typeof(object), typeof(ChatEditor));

        public object? SendCommandParameter
        {
            get => GetValue(SendCommandParameterProperty);
            set => SetValue(SendCommandParameterProperty, value);
        }

#if WINDOWS
        private Microsoft.UI.Xaml.Controls.TextBox? _windowsView;
#endif

        public ChatEditor()
        {
            HandlerChanged += OnHandlerChanged;
        }

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            if (Handler?.PlatformView == null)
                return;

#if ANDROID

            if (Handler.PlatformView is AndroidX.AppCompat.Widget.AppCompatEditText androidView)
            {
                androidView.ShowSoftInputOnFocus = false;
                androidView.Focusable = false;
                androidView.FocusableInTouchMode = false;
                androidView.LongClickable = true;
            }

#elif IOS || MACCATALYST

            if (Handler.PlatformView is UIKit.UITextView iosView)
            {
                iosView.Editable = false;
                iosView.Selectable = true;
                iosView.UserInteractionEnabled = true;
            }

#elif WINDOWS

            if (_windowsView != null)
            {
                _windowsView.PreviewKeyDown -= OnPreviewKeyDown;
                _windowsView = null;
            }

            if (Handler.PlatformView is Microsoft.UI.Xaml.Controls.TextBox windowsView)
            {
                _windowsView = windowsView;
                _windowsView.PreviewKeyDown += OnPreviewKeyDown;

                // 移除 WinUI 的底部强调线
                _windowsView.Resources["TextControlBorderBrushFocused"] = null;
                _windowsView.Resources["TextControlBorderBrushPointerOver"] = null;
                _windowsView.Resources["TextControlBorderBrush"] = null;
            }

#endif

        }

#if WINDOWS

        private void OnPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter)
                return;

            // Shift + Enter => 换行
            if (IsShiftDown())
                return;

            // 阻止 TextBox 处理 Enter（否则一定换行）
            e.Handled = true;

            // 长按忽略
            if (e.KeyStatus.WasKeyDown)
                return;

            // 空内容忽略
            if (string.IsNullOrWhiteSpace(Text))
                return;

            var parameter = SendCommandParameter ?? Text;

            if (SendCommand?.CanExecute(parameter) == true)
            {
                SendCommand.Execute(parameter);
            }
        }

        private static bool IsShiftDown()
        {
            return Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        }

#endif

    }
}
