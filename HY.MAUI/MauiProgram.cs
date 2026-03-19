using CommunityToolkit.Maui;
using HY.MAUI.Communication;
using HY.MAUI.Communication.Auth;
using HY.MAUI.Communication.Http;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.PageModels.Chat;
using HY.MAUI.PageModels.Contact;
using HY.MAUI.PageModels.Login;
using HY.MAUI.PageModels.Mine;
using HY.MAUI.Pages.Chat;
using HY.MAUI.Pages.Contact;
using HY.MAUI.Pages.Login;
using HY.MAUI.Pages.Mine;
using HY.MAUI.Services;
using HY.MAUI.Services.Interfaces;
using HY.MAUI.Setups;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace HY.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
#if WINDOWS
                    Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping("KeyboardAccessibleCollectionView", (handler, view) =>
				    {
					    handler.PlatformView.SingleSelectionFollowsFocus = false;
				    });

				    //Microsoft.Maui.Handlers.ContentViewHandler.Mapper.AppendToMapping(nameof(Pages.Controls.CategoryChart), (handler, view) =>
				    //{
					   // if (view is Pages.Controls.CategoryChart && handler.PlatformView is Microsoft.Maui.Platform.ContentPanel contentPanel)
					   // {
						  //  contentPanel.IsTabStop = true;
					   // }
				    //});
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });


            builder.Services.AddLocalServiceSetup();
            builder.Services.AddCommunicationSetup();
            builder.Services.AddPageAndPageModelSetup();

            #region MyRegion

            //            EditorHandler.Mapper.AppendToMapping("RemoveFocusLine", (handler, view) =>
            //            {
            //#if ANDROID
            //                var editText = handler.PlatformView;

            //                // 1️⃣ 去掉背景（包含 Focused 状态）
            //                editText.Background = null;

            //                // 2️⃣ 禁用焦点高亮色
            //                editText.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Transparent);

            //                // 3️⃣ 防止某些机型恢复下划线
            //                editText.SetPadding(
            //                    editText.PaddingLeft,
            //                    editText.PaddingTop,
            //                    editText.PaddingRight,
            //                    editText.PaddingBottom);
            //#endif
            //            });


            //            EditorHandler.Mapper.AppendToMapping("NoFocusLine", (handler, view) =>
            //            {
            //#if ANDROID
            //                handler.PlatformView.Background = null;
            //                handler.PlatformView.BackgroundTintList =
            //                    ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
            //#endif
            //            });



            //            EditorHandler.Mapper.AppendToMapping("RemoveFocusLine", (handler, view) =>
            //            {
            //#if WINDOWS
            //                var editor = handler.PlatformView;

            //                // 1️⃣ 去掉焦点可视化（重点）
            //                editor.UseSystemFocusVisuals = false;

            //                // 2️⃣ 去掉边框（防止部分主题显示底线）
            //                editor.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);

            //                // 3️⃣ 可选：背景透明
            //                editor.Background = null;
            //#endif
            //            });




            //            EditorHandler.Mapper.AppendToMapping("RemoveFocusLineOnly", (handler, view) =>
            //            {
            //#if WINDOWS
            //                handler.PlatformView.UseSystemFocusVisuals = false;
            //#endif
            //            });

            #endregion

            return builder.Build();
        }
    }
}
