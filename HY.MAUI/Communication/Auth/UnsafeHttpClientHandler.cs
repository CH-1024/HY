using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Communication.Auth
{
    public class UnsafeHttpClientHandler : HttpClientHandler
    {
        public UnsafeHttpClientHandler()
        {
            //#if DEBUG
            //            // 仅开发环境忽略证书
            //            ServerCertificateCustomValidationCallback = DangerousAcceptAnyServerCertificateValidator;
            //#endif

            ServerCertificateCustomValidationCallback = DangerousAcceptAnyServerCertificateValidator;

        }
    }
}
