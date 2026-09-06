using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Communication
{
    public static class ApiUrl
    {
        // 开发环境
#if WINDOWS || MACCATALYST || IOS
        public static string Address => "localhost";
        public static string Port => "8003";
#elif ANDROID
        public static string Address => "10.0.2.2";
        public static string Port => "8003";
#endif

        // 生产环境
        //public static string Address => "hoyi.net.cn";
        //public static string Port => "8003";



        #region SignalR
        public static string SignalRUrl => $"https://{Address}:{Port}";

        public static string ChatHub => $"{SignalRUrl}/chatHub/";
        #endregion



        #region Http
        public static string HttpUrl => $"https://{Address}:{Port}";

        // Test
        public static string Ping1 => $"{HttpUrl}/auth/ping1/";
        public static string Ping2 => $"{HttpUrl}/auth/ping2/";


        // Login & Auth
        public static string Refresh => $"{HttpUrl}/auth/refresh/";
        public static string Login => $"{HttpUrl}/auth/login/";
        public static string Logout => $"{HttpUrl}/auth/logout/";


        // Chat
        public static string GetChats => $"{HttpUrl}/chat/get/chats/";
        public static string ReadAll => $"{HttpUrl}/chat/read/all/";


        //Message
        public static string GetMessages => $"{HttpUrl}/message/get/messages/";
        public static string SendMessage => $"{HttpUrl}/message/send/message/";
        public static string RecallMessage => $"{HttpUrl}/message/recall/message/";
        public static string DeleteMessage => $"{HttpUrl}/message/delete/message/";

        // File
        public static string UploadImage => $"{HttpUrl}/file/upload/image/";
        public static string UploadVideo => $"{HttpUrl}/file/upload/video/";
        public static string UploadHead => $"{HttpUrl}/file/upload/head/";


        // Contact
        public static string GetContactRequests => $"{HttpUrl}/contact/get/contactrequests/";
        public static string GetContacts => $"{HttpUrl}/contact/get/contacts/";
        public static string GetContact => $"{HttpUrl}/contact/get/contact/";
        public static string SearchContact => $"{HttpUrl}/contact/search/contact/";
        public static string RequestContact => $"{HttpUrl}/contact/request/contact/";
        public static string RespondContact => $"{HttpUrl}/contact/respond/contact/";
        public static string DeleteContact => $"{HttpUrl}/contact/delete/contact/";


        // User
        public static string Register => $"{HttpUrl}/user/register/";
        public static string UpdateHead => $"{HttpUrl}/user/update/head/";
        #endregion



        // ImageVM
        public static string? Get_Origin_Image(string? file_Id)
        {
            return file_Id == null ? null :$"{HttpUrl}/file/image/origin/{file_Id}";
        }

        public static string? Get_Compress_Image(string? file_Id)
        {
            return file_Id == null ? null :$"{HttpUrl}/file/image/compress/{file_Id}";
        }

        public static string? Get_Thumb_Image(string? file_Id)
        {
            return file_Id == null ? null :$"{HttpUrl}/file/image/thumb/{file_Id}";
        }




        // VideoVM
        public static string? Get_Origin_Video(string? file_Id)
        {
            return file_Id == null ? null : $"{HttpUrl}/file/video/origin/{file_Id}";
        }

        public static string? Get_Compress_Video(string? file_Id)
        {
            return file_Id == null ? null : $"{HttpUrl}/file/video/compress/{file_Id}";
        }

        public static string? Get_Cover_Video(string? file_Id)
        {
            return file_Id == null ? null : $"{HttpUrl}/file/video/cover/{file_Id}";
        }




        // Head
        public static string? Get_Head_Image(string? file_Id)
        {
            return file_Id == null ? null :$"{HttpUrl}/file/head/{file_Id}";
        }

    }
}