using CommunityToolkit.Mvvm.ComponentModel;
using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Chat
{
    public partial class ImagePreviewPageModel : ObservableObject, IQueryAttributable
    {
        private string? compressed_Image_Url;
        public string? Compressed_Image_Url
        {
            get { return compressed_Image_Url; }
            set { SetProperty(ref compressed_Image_Url, value); }
        }


        public ImagePreviewPageModel()
        {
        
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            Compressed_Image_Url = (string)query["Compressed_Image_Url"];
        }



    }
}
