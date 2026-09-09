using HY.MAUI.Models.MsgVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Controls
{
    public class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? TextTemplate { get; set; }
        public DataTemplate? ImageTemplate { get; set; }
        public DataTemplate? FileTemplate { get; set; }
        public DataTemplate? VoiceTemplate { get; set; }
        public DataTemplate? VideoTemplate { get; set; }
        public DataTemplate? SystemTemplate { get; set; }
        public DataTemplate? CallVoiceTemplate { get; set; }
        public DataTemplate? CallVideoTemplate { get; set; }

        protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
        {
            return item switch
            {
                TextMessageVM => TextTemplate,
                ImageMessageVM => ImageTemplate,
                FileMessageVM => FileTemplate,
                VoiceMessageVM => VoiceTemplate,
                VideoMessageVM => VideoTemplate,
                SystemMessageVM => SystemTemplate,
                CallVoiceMessageVM => CallVoiceTemplate,
                CallVideoMessageVM => CallVideoTemplate,
                _ => throw new InvalidOperationException("Unsupported message type.")
            };
        }
    }
}
