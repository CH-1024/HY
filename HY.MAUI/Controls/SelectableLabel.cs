using Microsoft.Maui.Handlers;

namespace HY.MAUI.Controls
{
    public class SelectableLabel : Label
    {
#if WINDOWS
        private Microsoft.UI.Xaml.Controls.TextBlock? _windowsView;
#endif

        public SelectableLabel()
        {
            HandlerChanged += OnHandlerChanged;
        }

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            if (Handler?.PlatformView == null)
                return;

#if WINDOWS
            if (_windowsView != null)
            {
                _windowsView = null;
            }

            if (Handler.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock windowsView)
            {
                _windowsView = windowsView;
                _windowsView.IsTextSelectionEnabled = true;
                _windowsView.ContextFlyout = null;
            }
#endif
        }

    }
}
