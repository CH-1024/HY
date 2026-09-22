using HY.MAUI.Communication.SendQueue;
using HY.MAUI.Pages.Login;
using HY.MAUI.Services;
using HY.MAUI.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HY.MAUI
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoginService _loginService;

        public App(IServiceProvider serviceProvider, ILoginService loginService, MessageSendWorker worker)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _loginService = loginService;

            _ = worker.RunAsync(CancellationToken.None);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // 切换到深色模式
            Application.Current!.UserAppTheme = AppTheme.Dark;

            var window = new Window();

#if WINDOWS
            window.Created += OnWindowCreated;
#endif

            if (_loginService.IsLoggedIn)
            {
                var loadingPage = _serviceProvider.GetRequiredService<LoadingPage>();
                window.Page = new NavigationPage(loadingPage);
            }
            else
            {
                var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
                window.Page = new NavigationPage(loginPage);
            }

            return window;
        }




#if WINDOWS

        private void OnWindowCreated(object? sender, EventArgs e)
        {
            #region MyRegion
            //if (sender is not Window mauiWindow) return;

            //if (mauiWindow.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow) return;

            //var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);

            //var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);

            //var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            //// 固定大小
            //appWindow.Resize(new Windows.Graphics.SizeInt32(900, 900));

            //// 禁止最大化、调整大小
            //if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            //{
            //    presenter.IsResizable = false;
            //    presenter.IsMaximizable = false;
            //    presenter.IsMinimizable = false;

            //    // ★ 真正隐藏标题栏和窗口边框
            //    //presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
            //}

            //// 内容延伸到标题栏
            //if (appWindow.TitleBar is Microsoft.UI.Windowing.AppWindowTitleBar titleBar)
            //{
            //    titleBar.ExtendsContentIntoTitleBar = true;

            //    // 标题栏高度折叠
            //    titleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Collapsed;

            //    // 标题栏按钮区域透明
            //    titleBar.ButtonBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);

            //    titleBar.ButtonInactiveBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);

            //    titleBar.ButtonHoverBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);

            //    titleBar.ButtonPressedBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
            //}
            #endregion

            if (sender is not Microsoft.Maui.Controls.Window mauiWindow)
                return;

            if (mauiWindow.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
                return;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);

            // 真正设置为无边框窗口
            SetBorderlessWindow(hwnd);

            // 固定窗口大小
            SetWindowSize(hwnd, 1200, 800);

            // 居中
            CenterWindow(hwnd, 1200, 800);
        }



        private static void SetBorderlessWindow(nint hwnd)
        {
            const int GWL_STYLE = -16;

            const long WS_POPUP = 0x80000000L;

            // 读取当前 Style
            var style = GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();

            // 只保留 Popup
            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);

            style |= WS_POPUP;

            SetWindowLongPtr(hwnd, GWL_STYLE, new nint(style));

            // 重新计算窗口 Frame
            SetWindowPos(hwnd, nint.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        private static void SetWindowSize(nint hwnd, int width, int height)
        {
            SetWindowPos(hwnd, nint.Zero, 0, 0, width, height, SWP_NOMOVE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        private static void CenterWindow(nint hwnd, int width, int height)
        {
            var screen = GetPrimaryScreenWorkArea();

            int x = screen.left + (screen.right - screen.left - width) / 2;

            int y = screen.top + (screen.bottom - screen.top - height) / 2;

            SetWindowPos(hwnd, nint.Zero, x, y, width, height, SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        private static RECT GetPrimaryScreenWorkArea()
        {
            var monitor = MonitorFromWindow(Process.GetCurrentProcess().MainWindowHandle, MONITOR_DEFAULTTOPRIMARY);

            MONITORINFO info = new();
            info.cbSize = Marshal.SizeOf<MONITORINFO>();

            GetMonitorInfo(monitor, ref info);

            return info.rcWork;
        }

        #region Win32

        private const long WS_CAPTION = 0x00C00000L;
        private const long WS_THICKFRAME = 0x00040000L;
        private const long WS_MINIMIZEBOX = 0x00020000L;
        private const long WS_MAXIMIZEBOX = 0x00010000L;
        private const long WS_SYSMENU = 0x00080000L;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;

            public RECT rcMonitor;

            public RECT rcWork;

            public uint dwFlags;
        }

        #endregion

#endif



    }
}