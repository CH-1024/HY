using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace HY.MAUI.Models.MsgVM
{
    public class VoiceMessageVM : MessageVM
    {
        public TimeSpan Duration { get; set; }
        public ICommand PlayCommand { get; set; } = default!;
    }
}
