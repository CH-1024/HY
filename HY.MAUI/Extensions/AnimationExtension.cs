using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Extensions
{
    public static class AnimationExtension
    {

        extension (VisualElement view)
        {
            public Task AnimateAsync(string name, Action<double> callback, double start, double end, uint length = 250, Easing? easing = null)
            {
                var tcs = new TaskCompletionSource();

                var animation = new Animation(callback, start, end);

                animation.Commit(view, name, 16, length, easing ?? Easing.Linear, (v, c) => tcs.SetResult());

                return tcs.Task;
            }
        }

    }
}
