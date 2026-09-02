using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace HY.MAUI.Models.MsgVM
{
    public class FileMessageVM : MessageVM
    {
        private double uploadProgress;
        public double UploadProgress
        {
            get { return uploadProgress; }
            set { SetProperty(ref uploadProgress, value); }
        }

        public long FileSize { get; set; }
    }
}
