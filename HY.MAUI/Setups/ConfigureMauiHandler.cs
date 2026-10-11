using HY.MAUI.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Setups
{
    public static class ConfigureMauiHandler
    {
        public static void AddConfigureMauiHandlers(this MauiAppBuilder builder)
        {

#if ANDROID
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<ChatEditor, Platforms.Android.ChatEditorHandler>();
            });
#endif

#if IOS
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<ChatEditor, Platforms.iOS.ChatEditorHandler>();
            });
#endif

#if MACCATALYST
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<SelectableLabel, Platforms.MacCatalyst.SelectableLabelHandler>();
                handlers.AddHandler<ChatEditor, Platforms.MacCatalyst.ChatEditorHandler>();
            });
#endif

#if WINDOWS
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<SelectableLabel, Platforms.Windows.SelectableLabelHandler>();
                handlers.AddHandler<ChatEditor, Platforms.Windows.ChatEditorHandler>();
            });
#endif

        }
    }
}
