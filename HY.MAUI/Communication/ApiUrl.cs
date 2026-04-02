using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Communication
{
    public static class ApiUrl
    {
        // 开发环境
#if WINDOWS || MACCATALYST || IOS
        public static string HttpBaseUrl => "https://localhost:8003";
        public static string SignalRBaseUrl => "https://localhost:8003";
#elif ANDROID
        public static string HttpBaseUrl => "https://10.0.2.2:8003";
        public static string SignalRBaseUrl => "https://10.0.2.2:8003";
#endif

        // 生产环境
        //public static string HttpBaseUrl => "https://hoyi.net.cn:8003";
        //public static string SignalRBaseUrl => "https://hoyi.net.cn:8003";


        public static string ChatHub => $"{SignalRBaseUrl}/chatHub/";


        // Test
        public static string Ping1 => $"{HttpBaseUrl}/auth/ping1/";
        public static string Ping2 => $"{HttpBaseUrl}/auth/ping2/";


        // Login & Auth
        public static string Refresh => $"{HttpBaseUrl}/auth/refresh/";
        public static string Login => $"{HttpBaseUrl}/auth/login/";
        public static string Logout => $"{HttpBaseUrl}/auth/logout/";


        // Chat
        public static string GetChats => $"{HttpBaseUrl}/chat/get/chats/";
        public static string ReadAll => $"{HttpBaseUrl}/chat/read/all/";


        //Message
        public static string GetMessages => $"{HttpBaseUrl}/message/get/messages/";
        public static string GetSingleChatMessages => $"{HttpBaseUrl}/message/get/single/messages/";
        public static string GetGroupChatMessages => $"{HttpBaseUrl}/message/get/group/messages/";
        public static string UploadImage => $"{HttpBaseUrl}/file/upload/image/";
        public static string UploadHead => $"{HttpBaseUrl}/file/upload/head/";


        // Contact
        public static string GetContacts => $"{HttpBaseUrl}/contact/get/contacts/";
        public static string GetContactRequests => $"{HttpBaseUrl}/contact/get/contactrequests/";
        public static string GetContact => $"{HttpBaseUrl}/contact/get/contact/";
        public static string SearchContact => $"{HttpBaseUrl}/contact/search/contact/";


        // User
        public static string Register => $"{HttpBaseUrl}/user/register/";
        public static string UpdateHead => $"{HttpBaseUrl}/user/update/head/";


        public static string? Get_Origin_Image(string? file_Id)
        {
            return file_Id == null ? null :$"{HttpBaseUrl}/file/image/{file_Id}";
        }

        public static string? Get_Compress_Image(string? file_Id)
        {
            return file_Id == null ? null :$"{HttpBaseUrl}/file/compress/{file_Id}";
        }

        public static string? Get_Thumb_Image(string? file_Id)
        {
            return file_Id == null ? null :$"{HttpBaseUrl}/file/thumb/{file_Id}";
        }

        public static string? Get_Head_Image(string? file_Id)
        {
            return file_Id == null ? null :$"{HttpBaseUrl}/file/head/{file_Id}";
        }

    }
}