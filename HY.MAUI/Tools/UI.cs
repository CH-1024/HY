using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Tools
{
    public static class UI
    {
        public static void Run(Action action)
        {
            MainThread.BeginInvokeOnMainThread(action);
        }

        public static Task Run(Func<Task> action)
        {
            return MainThread.InvokeOnMainThreadAsync(action);
        }
    }
}
