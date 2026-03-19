using HY.MAUI.Models;
using HY.MAUI.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Services
{
    public class GlobalCache : IGlobalCache
    {
        private static readonly ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();
        private static readonly string CurrentUserKey = "CurrentUser";

        public void SetCache(string key, object value)
        {
            _cache[key] = value;
        }

        public T? GetCache<T>(string key)
        {
            return _cache.TryGetValue(key, out var value) ? (T)value : default;
        }


        public UserVM GetCurrentUser()
        {
            return GetCache<UserVM>(CurrentUserKey)!;
        }

        public void SetCurrentUser(UserVM userVM)
        {
            SetCache(CurrentUserKey, userVM);
        }

    }
}
