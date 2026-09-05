using Microsoft.Maui.Controls;

namespace HY.MAUI.Controls
{
    public class AnimatedEllipsisLabel : Label
    {
        private CancellationTokenSource? _cts;

        // 当前真正由外部设置的基础文本
        private string _baseText = string.Empty;

        // 防止动画自己修改 Text 时触发文本变化处理
        private bool _isAnimating;

        public static readonly BindableProperty IsAnimationEnabledProperty = BindableProperty.Create(nameof(IsAnimationEnabled), typeof(bool), typeof(AnimatedEllipsisLabel), true, propertyChanged: OnIsAnimationEnabledChanged);

        public static readonly BindableProperty IntervalProperty = BindableProperty.Create(nameof(Interval), typeof(int), typeof(AnimatedEllipsisLabel), 500);

        public static readonly BindableProperty MaxDotsProperty = BindableProperty.Create(nameof(MaxDots), typeof(int), typeof(AnimatedEllipsisLabel), 3);

        /// <summary>
        /// 是否启用动画
        /// </summary>
        public bool IsAnimationEnabled
        {
            get => (bool)GetValue(IsAnimationEnabledProperty);
            set => SetValue(IsAnimationEnabledProperty, value);
        }

        /// <summary>
        /// 点变化间隔，单位毫秒
        /// </summary>
        public int Interval
        {
            get => (int)GetValue(IntervalProperty);
            set => SetValue(IntervalProperty, value);
        }

        /// <summary>
        /// 最大点数量
        /// </summary>
        public int MaxDots
        {
            get => (int)GetValue(MaxDotsProperty);
            set => SetValue(MaxDotsProperty, value);
        }

        public AnimatedEllipsisLabel()
        {
            _baseText = Text ?? string.Empty;
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler != null)
            {
                // 控件进入可视界面
                _baseText = Text ?? string.Empty;

                StartAnimation();
            }
            else
            {
                // 控件离开可视界面
                StopAnimation();
            }
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName != TextProperty.PropertyName)
                return;

            // Text 是动画自己修改的，不处理
            if (_isAnimating)
                return;

            // Text 是外部修改的
            _baseText = Text ?? string.Empty;
        }

        private static void OnIsAnimationEnabledChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (AnimatedEllipsisLabel)bindable;

            if ((bool)newValue)
            {
                control.StartAnimation();
            }
            else
            {
                control.StopAnimation();

                // 关闭动画后恢复原始文本
                control.Text = control._baseText;
            }
        }

        private void StartAnimation()
        {
            if (!IsAnimationEnabled)
                return;

            if (Handler == null)
                return;

            // 已经运行，不重复启动
            if (_cts != null)
                return;

            _cts = new CancellationTokenSource();

            _ = AnimateAsync(_cts.Token);
        }

        private void StopAnimation()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _isAnimating = false;

            // 停止后显示基础文本
            if (Text != _baseText)
            {
                _isAnimating = true;

                try
                {
                    Text = _baseText;
                }
                finally
                {
                    _isAnimating = false;
                }
            }
        }

        private async Task AnimateAsync(CancellationToken token)
        {
            int dotCount = 0;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    string animatedText = _baseText + new string('.', dotCount);

                    _isAnimating = true;

                    try
                    {
                        Text = animatedText;
                    }
                    finally
                    {
                        _isAnimating = false;
                    }

                    dotCount++;

                    if (dotCount > MaxDots)
                    {
                        dotCount = 0;
                    }

                    await Task.Delay(Interval, token);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            finally
            {
                _isAnimating = false;
            }
        }
    }
}