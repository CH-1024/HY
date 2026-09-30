using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace HY.MAUI.Controls
{
    public class ChatEditor : Editor
    {
        public static readonly BindableProperty SendCommandProperty = BindableProperty.Create(nameof(SendCommand), typeof(ICommand), typeof(ChatEditor));

        public ICommand? SendCommand
        {
            get => (ICommand?)GetValue(SendCommandProperty);
            set => SetValue(SendCommandProperty, value);
        }

        public static readonly BindableProperty SendCommandParameterProperty = BindableProperty.Create(nameof(SendCommandParameter), typeof(object), typeof(ChatEditor));

        public object? SendCommandParameter
        {
            get => GetValue(SendCommandParameterProperty);
            set => SetValue(SendCommandParameterProperty, value);
        }



        public ChatEditor()
        {
            #region andorid
            //// 拦截回车按键
            //platformView.KeyPress += (s, e) =>
            //{
            //    if (e.KeyCode == Android.Views.Keycode.Enter)
            //        e.Handled = true;
            //};
            #endregion
        }



        /// <summary>
        /// 执行发送命令
        /// </summary>
        public void ExecuteSendCommand()
        {
            if (string.IsNullOrWhiteSpace(Text))
                return;

            var parameter = SendCommandParameter ?? Text;

            if (SendCommand?.CanExecute(parameter) == true)
            {
                SendCommand.Execute(parameter);
            }
        }
        
    }
}