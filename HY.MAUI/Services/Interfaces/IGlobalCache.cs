using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Services.Interfaces
{
    public interface IGlobalCache
    {
        void SetCache(string key, object value);
        T? GetCache<T>(string key);


        UserVM GetCurrentUser();
        void SetCurrentUser(UserVM userVM);
    }
}
