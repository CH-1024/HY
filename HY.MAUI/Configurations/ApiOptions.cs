using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Configurations
{
    public class ApiOptions
    {
        //准备工作：
        //  1、Platforms > Windows > ffmpeg：放入 ffmpeg 8.1 静态库



        // 开发环境
#if WINDOWS || MACCATALYST || IOS
                public const string Address = "localhost";
                public const string Port = "8003";
#elif ANDROID
        public const string Address = "10.0.2.2";
        public const string Port = "8003";
#endif

        //// 生产环境
        //public const string Address = "hoyi.net.cn";
        //public const string Port = "8003";


        // SignalR
        public const double KeepAlive = 15;
        public const double ServerTimeout = 30;


        // Http
        public const double Timeout = 30;

    }
}
