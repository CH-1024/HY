using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Tools
{
    public static class UI
    {
        public static void Run(Action action)
        {
            if (MainThread.IsMainThread)
            {
                action();
                return;
            }

            MainThread.BeginInvokeOnMainThread(action);
        }

        public static Task Run(Func<Task> funcTask)
        {
            if (MainThread.IsMainThread)
            {
                return funcTask();
            }

            return MainThread.InvokeOnMainThreadAsync(funcTask);
        }

        //public static Task<T> Run<T>(Func<T> func)
        //{
        //    if (MainThread.IsMainThread)
        //    {
        //        return Task.FromResult(func());
        //    }

        //    return MainThread.InvokeOnMainThreadAsync(func);
        //}

        public static Task<T> Run<T>(Func<Task<T>> funcTask)
        {
            if (MainThread.IsMainThread)
            {
                return funcTask();
            }

            return MainThread.InvokeOnMainThreadAsync(funcTask);
        }
    }
}
