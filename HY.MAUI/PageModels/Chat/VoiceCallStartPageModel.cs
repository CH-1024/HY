using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Chat
{
    public partial class VoiceCallStartPageModel : ObservableObject, IQueryAttributable
    {
        private string targetAvatar;
        public string TargetAvatar
        {
            get { return targetAvatar; }
            set { SetProperty(ref targetAvatar, value); }
        }

        private string targetName;
        public string TargetName
        {
            get { return targetName; }
            set { SetProperty(ref targetName, value); }
        }

        ChatType _chatType;
        long _calleeId;
        DateTime _expiry;


        public VoiceCallStartPageModel()
        {

        }


        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _chatType = (ChatType)query["ChatType"];
            _calleeId = Convert.ToInt64(query["CalleeId"]);
            TargetAvatar = query["TargetAvatar"]?.ToString();
            TargetName = query["TargetName"]?.ToString();
        }




        [RelayCommand]
        async Task HangUp()
        {

        }



    }
}
