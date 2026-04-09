using HY.MAUI.Dtos;
using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace HY.MAUI.Services.Interfaces
{
    public interface ILoginService
    {
        bool IsLoggedIn { get; }


        Task Login(UserVM user);

        void Logout();

        Task SetCurrentUser(UserVM user);

        Task<UserVM?> GetCurrentUser();

        Task<string> GetDeviceId();

        Task<string> GetDeviceName();

        Task<int> GetDevicePlatform();

        Task<int> GetDeviceType();

    }
}
