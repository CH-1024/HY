using HY.MAUI.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using TextAlignment = Microsoft.Maui.TextAlignment;
using Thickness = Microsoft.UI.Xaml.Thickness;

namespace HY.MAUI.Platforms.Windows
{
    public class SelectableLabelHandler : ViewHandler<SelectableLabel, TextBlock>
    {
        public static IPropertyMapper<SelectableLabel, SelectableLabelHandler> Mapper = new PropertyMapper<SelectableLabel, SelectableLabelHandler>
        {
            [nameof(Label.Text)] = MapText,
            [nameof(Label.TextColor)] = MapTextColor,

            [nameof(Label.FontFamily)] = MapFont,
            [nameof(Label.FontSize)] = MapFont,
            [nameof(Label.FontAttributes)] = MapFont,

            [nameof(Label.HorizontalTextAlignment)] = MapAlignment,
            [nameof(Label.VerticalTextAlignment)] = MapAlignment,

            [nameof(Label.Padding)] = MapPadding,
        };

        public SelectableLabelHandler() : base(Mapper)
        {
        }

        protected override TextBlock CreatePlatformView()
        {
            return new TextBlock
            {
                // 允许选择文字
                IsTextSelectionEnabled = true,

                // 多行
                TextWrapping = TextWrapping.WrapWholeWords,

                // Windows TextBlock 默认 Padding
                Padding = new Thickness(0),

                // 禁用右键菜单
                ContextFlyout = null
            };
        }

        protected override void ConnectHandler(TextBlock platformView)
        {
            base.ConnectHandler(platformView);

            UpdateText();
            UpdateTextColor();
            UpdateFont();
            UpdateAlignment();
            UpdatePadding();
        }

        protected override void DisconnectHandler(TextBlock platformView)
        {
            base.DisconnectHandler(platformView);
        }

        // ---------------------------------------------------------
        // Text
        // ---------------------------------------------------------

        private static void MapText(SelectableLabelHandler handler, SelectableLabel view)
        {
            handler.UpdateText();
        }

        private void UpdateText()
        {
            if (PlatformView == null)
                return;

            PlatformView.Text = VirtualView.Text ?? string.Empty;
        }

        // ---------------------------------------------------------
        // TextColor
        // ---------------------------------------------------------

        private static void MapTextColor(SelectableLabelHandler handler, SelectableLabel view)
        {
            handler.UpdateTextColor();
        }

        private void UpdateTextColor()
        {
            if (PlatformView == null)
                return;

            PlatformView.Foreground = VirtualView.TextColor.ToPlatform();
        }

        // ---------------------------------------------------------
        // Font
        // ---------------------------------------------------------

        private static void MapFont(SelectableLabelHandler handler, SelectableLabel view)
        {
            handler.UpdateFont();
        }

        private void UpdateFont()
        {
            if (PlatformView == null)
                return;

            var fontManager = MauiContext?.Services.GetService<IFontManager>();

            if (fontManager == null)
                return;

            //var fontFamily = fontManager.GetFontFamily(((ITextStyle)VirtualView).Font);
            //var fontSize = fontManager.GetFontSize(((ITextStyle)VirtualView).Font);

            PlatformView.FontFamily = fontManager.GetFontFamily(((ITextStyle)VirtualView).Font);
            PlatformView.FontSize = fontManager.GetFontSize(((ITextStyle)VirtualView).Font);
            //PlatformView.FontWeight = fontManager.GetFontWeight(((ITextStyle)VirtualView).Font);
            //PlatformView.FontStyle = fontManager.GetFontStyle(((ITextStyle)VirtualView).Font);
        }

        // ---------------------------------------------------------
        // Alignment
        // ---------------------------------------------------------

        private static void MapAlignment(SelectableLabelHandler handler, SelectableLabel view)
        {
            handler.UpdateAlignment();
        }

        private void UpdateAlignment()
        {
            if (PlatformView == null)
                return;

            PlatformView.HorizontalTextAlignment = VirtualView.HorizontalTextAlignment switch
            {
                TextAlignment.Center => Microsoft.UI.Xaml.TextAlignment.Center,

                TextAlignment.End => Microsoft.UI.Xaml.TextAlignment.Right,

                _ => Microsoft.UI.Xaml.TextAlignment.Left
            };

            PlatformView.HorizontalAlignment = VirtualView.VerticalTextAlignment switch
            {
                TextAlignment.Center => Microsoft.UI.Xaml.HorizontalAlignment.Center,

                TextAlignment.End => Microsoft.UI.Xaml.HorizontalAlignment.Right,

                _ => Microsoft.UI.Xaml.HorizontalAlignment.Left
            };

            PlatformView.VerticalAlignment = VirtualView.VerticalTextAlignment switch
            {
                TextAlignment.Center => Microsoft.UI.Xaml.VerticalAlignment.Center,

                TextAlignment.End => Microsoft.UI.Xaml.VerticalAlignment.Bottom,

                _ => Microsoft.UI.Xaml.VerticalAlignment.Top
            };
        }

        // ---------------------------------------------------------
        // Padding
        // ---------------------------------------------------------

        private static void MapPadding(SelectableLabelHandler handler, SelectableLabel view)
        {
            handler.UpdatePadding();
        }

        private void UpdatePadding()
        {
            if (PlatformView == null)
                return;

            var padding = VirtualView.Padding;

            PlatformView.Padding = new Thickness(
                padding.Left,
                padding.Top,
                padding.Right,
                padding.Bottom);
        }

    }
}
