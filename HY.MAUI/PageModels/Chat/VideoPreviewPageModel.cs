using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Chat
{
    public partial class VideoPreviewPageModel : ObservableObject, IQueryAttributable
    {


        private string? original_Video_Url;
        public string? Original_Video_Url
        {
            get { return original_Video_Url; }
            set { SetProperty(ref original_Video_Url, value); }
        }


        public VideoPreviewPageModel()
        {

        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            Original_Video_Url = (string)query["Original_Video_Url"];
        }



    }
}
