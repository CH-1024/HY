using HY.MAUI.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace HY.MAUI.Platforms.MacCatalyst;

public class SelectableLabelHandler : ViewHandler<SelectableLabel, UITextView>
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

    protected override UITextView CreatePlatformView()
    {
        return new SelectableTextView
        {
            Editable = false,

            // 核心：允许选择文字
            Selectable = true,

            // 不允许 UITextView 自己滚动
            ScrollEnabled = false,

            // 允许鼠标交互
            UserInteractionEnabled = true,

            BackgroundColor = UIColor.Clear,

            ShowsVerticalScrollIndicator = false,
            ShowsHorizontalScrollIndicator = false,

            TextContainerInset = UIEdgeInsets.Zero,
        };
    }

    protected override void ConnectHandler(UITextView platformView)
    {
        base.ConnectHandler(platformView);

        // 去掉 UITextView 默认左右空白
        platformView.TextContainer.LineFragmentPadding = 0;

        // 选择文字时使用系统 Tint
        platformView.TintColor = UIColor.Red;

        UpdateText();
        UpdateTextColor();
        UpdateFont();
        UpdateAlignment();
        UpdatePadding();
    }

    protected override void DisconnectHandler(UITextView platformView)
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

        PlatformView.TextColor = VirtualView.TextColor.ToPlatform();
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

        PlatformView.Font = fontManager.GetFont(((ITextStyle)VirtualView).Font, VirtualView.FontSize);
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

        PlatformView.TextAlignment = VirtualView.HorizontalTextAlignment switch
        {
            TextAlignment.Center => UITextAlignment.Center,

            TextAlignment.End => UITextAlignment.Right,

            _ => UITextAlignment.Left
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

        PlatformView.TextContainerInset = new UIEdgeInsets(
                (nfloat)padding.Top,
                (nfloat)padding.Left,
                (nfloat)padding.Bottom,
                (nfloat)padding.Right);

        // 防止 UITextView 自带的左右 padding
        PlatformView.TextContainer.LineFragmentPadding = 0;
    }
}
