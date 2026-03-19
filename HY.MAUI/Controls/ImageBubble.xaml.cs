using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Enums;
using HY.MAUI.Models.MsgVM;
using HY.MAUI.PageModels.Chat;
using HY.MAUI.PageModels.Chat.MessageCommands;
using System.Windows.Input;

namespace HY.MAUI.Controls;

public partial class ImageBubble : ContentView
{
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(IAsyncRelayCommand), typeof(TextBubble), null);
    public IAsyncRelayCommand Command
    {
        get => (IAsyncRelayCommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }



    public ImageBubble()
	{
		InitializeComponent();
	}







    private void ContactDetail_Tapped(object sender, TappedEventArgs e)
    {
        var param = new MessageCommandInvocation
        {
            Command = CommandNames.ContactDetail,
            Message = this.BindingContext as ImageMessageVM,
        };

        if (Command != null && Command.CanExecute(param))
        {
            Command.Execute(param);
        }
    }

    private void Bubble_Secondary_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not Border border)
            return;

        if (border.BindingContext is not ImageMessageVM msg)
            return;

        if (msg.Message_Status == MessageStatus.Recalled)
        {
            // 已撤回消息不显示菜单
            FlyoutBase.SetContextFlyout(border, null);
        }
        else
        {
            var flyout = CreateFlyout(msg);
            FlyoutBase.SetContextFlyout(border, flyout);
        }
    }

    private async void MenuFlyoutItem_Copy_Clicked(ImageMessageVM msg)
    {
        //await Clipboard.SetTextAsync(msg.Content ?? string.Empty);
    }

    private async Task MenuFlyoutItem_Delete_Clicked(ImageMessageVM msg)
    {
        var param = new MessageCommandInvocation
        {
            Command = CommandNames.DeleteMessage,
            Message = msg,
        };

        if (Command != null && Command.CanExecute(param))
        {
            await Command.ExecuteAsync(param);
        }
    }

    private async Task MenuFlyoutItem_Recall_Clicked(ImageMessageVM msg)
    {
        if (msg.IsSelf)
        {
            var param = new MessageCommandInvocation
            {
                Command = CommandNames.RecallMessage,
                Message = msg,
            };

            if (Command != null && Command.CanExecute(param))
            {
                await Command.ExecuteAsync(param);
            }
        }
        else
        {
            _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "你只能撤回自己的消息", "确定");
        }
    }

    private async void Image_Tapped(object sender, TappedEventArgs e)
    {
        var param = new MessageCommandInvocation
        {
            Command = CommandNames.TapImageMessage,
            Message = sender is Border border ? (ImageMessageVM)border.BindingContext : null,
        };

        if (Command != null && Command.CanExecute(param))
        {
            await Command.ExecuteAsync(param);
        }
    }



    private MenuFlyout CreateFlyout(ImageMessageVM msg)
    {
        var flyout = new MenuFlyout();

        flyout.Add(new MenuFlyoutItem
        {
            Text = "复制",
            Command = new Command(() => MenuFlyoutItem_Copy_Clicked(msg))
        });

        var canDelete = msg.Message_Status == MessageStatus.Sented;
        if (canDelete)
        {
            flyout.Add(new MenuFlyoutItem
            {
                Text = "删除",
                Command = new Command(async () => await MenuFlyoutItem_Delete_Clicked(msg))
            });
        }

        var canRecall = msg.IsSelf && DateTime.UtcNow <= msg.Created_At.AddMinutes(5) && msg.Message_Status == MessageStatus.Sented;
        if (canRecall)
        {
            flyout.Add(new MenuFlyoutItem
            {
                Text = "撤回",
                Command = new Command(async () => await MenuFlyoutItem_Recall_Clicked(msg))
            });
        }

        return flyout;
    }


}