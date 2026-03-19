using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Pages.Chat.Controls
{
    public class TextMessage : Border
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(TextMessage), null);
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();

            if (Parent is not null)
            {
                MinimumHeightRequest = 0;
                MinimumWidthRequest = 0;
                Padding = new Thickness(1);
                Margin = new Thickness(0);
                StrokeThickness = 0;
                BackgroundColor = Color.FromArgb("#141414");
                VerticalOptions = LayoutOptions.Fill;
                StrokeShape = new RoundRectangle { CornerRadius = 5 };

                var editor = new Editor
                {
                    MinimumHeightRequest = 0,
                    MinimumWidthRequest = 0,
                    Margin = 0,
                    Text = Text,
                    FontSize = 16,
                    TextColor = Colors.White,
                    //HorizontalOptions = LayoutOptions.Fill,
                    //VerticalOptions = LayoutOptions.Fill,
                    HorizontalTextAlignment = TextAlignment.Start,
                    VerticalTextAlignment = TextAlignment.Center,
                    IsReadOnly = true
                    //LineBreakMode = LineBreakMode.WordWrap,
                };

                editor.SetBinding(Editor.TextProperty, new Binding(nameof(Text), source: this, mode: BindingMode.OneWay));

#if WINDOWS
                editor.HandlerChanged += (_, __) =>
                {
                    if (editor.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox tb)
                    {
                        // ① 关闭系统焦点视觉（底部提示线的来源）
                        tb.UseSystemFocusVisuals = false;

                        // ② 移除所有边框（包括主题底线）
                        tb.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                        tb.BorderBrush = null;

                        // ③ 移除 WinUI 的底部强调线（关键）
                        tb.Resources["TextControlBorderBrushFocused"] = null;
                        tb.Resources["TextControlBorderBrushPointerOver"] = null;
                        tb.Resources["TextControlBorderBrush"] = null;

                        //④ 可选：彻底禁用焦点
                        //tb.IsTabStop = false;

                        //⑤ 背景透明
                        tb.Background = null;


                        tb.Padding = new Microsoft.UI.Xaml.Thickness(10);
                        tb.Margin = new Microsoft.UI.Xaml.Thickness(0);
                        tb.MinHeight = 0;
                        tb.MinWidth = 0;

                    }
                };
#endif

#if ANDROID
                editor.HandlerChanged += (_, __) =>
                {
                    if (editor.Handler?.PlatformView is Android.Widget.EditText et)
                    {
                        // ① 保留 padding（防止背景清空后文本贴边）
                        //et.SetPadding(
                        //    et.PaddingLeft,
                        //    et.PaddingTop,
                        //    et.PaddingRight,
                        //    et.PaddingBottom);

                        // ② 移除默认背景（包括 focus underline）
                        et.Background = null;

                        // ③ 禁用焦点态下划线着色（关键）
                        et.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);

                        // ④ 可选：禁用焦点（纯展示用）
                        // et.Focusable = false;
                        // et.FocusableInTouchMode = false;

                        et.SetPadding(30,20,30,20);
                        et.SetMinWidth(0);
                        et.SetMinHeight(0);
                    }
                };
#endif

                this.Content = editor;
            }
        }
    }

}
