using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication;
using HY.MAUI.Communication.Http;
using HY.MAUI.Dtos;
using HY.MAUI.Extensions;
using HY.MAUI.Interfaces;
using HY.MAUI.Models;
using HY.MAUI.Services;
using HY.MAUI.Stores;
using HY.MAUI.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Mine
{
    public partial class ProfilePageModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalCache _globalCache;
        private readonly ILoginService _loginService;

        private readonly FileApi _fileApi;
        private readonly UserApi _userApi;

        private readonly MessageStore _messageStore;

        private UserVM currentUser = null;
        public UserVM CurrentUser
        {
            get { return currentUser; }
            set { SetProperty(ref currentUser, value); }
        }

        public ProfilePageModel(IServiceProvider serviceProvider, IGlobalCache globalCache, ILoginService loginService, FileApi fileApi, UserApi userApi, MessageStore messageStore)
        {
            _serviceProvider = serviceProvider;
            _globalCache = globalCache;
            _loginService = loginService;

            _fileApi = fileApi;
            _userApi = userApi;

            _messageStore = messageStore;
        }


        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("..", true);
        }

        [RelayCommand]
        async Task Appearing()
        {
            CurrentUser = _globalCache.GetCurrentUser().DeepClone()!;
        }

        [RelayCommand]
        async Task ChangeAvatar()
        {
            var image = await PickImage();
            if (image == null) return;

            var resp = await _fileApi.UploadHead(image);
            if (resp?.IsSucc != true)
            {
                await Application.Current!.Windows[0].Page!.DisplayAlertAsync("失败", $"上传头像失败[{resp?.Msg}]！", "确定");
                return;
            }

            var fileId = resp.GetValue<string>("File_Id");

            var url = ApiUrl.Get_Head_Image(fileId);

            if (!RegexHelper.IsValidUrl(url))
            {
                await Application.Current!.Windows[0].Page!.DisplayAlertAsync("失败", $"头像 URL 无效[{url}]！", "确定");
                return;
            }

            var resp2 = await _userApi.UpdateHead(url);
            if (resp2?.IsSucc != true)
            {
                await Application.Current!.Windows[0].Page!.DisplayAlertAsync("失败", $"更新头像失败[{resp2?.Msg}]！", "确定");
                return;
            }

            CurrentUser.Avatar = url;

            _globalCache.UpdateCurrentUser(currentUser);

            await _loginService.SetCurrentUser(currentUser);

            _messageStore.UpdateUserHead(currentUser.Id, url);
        }


        [RelayCommand]
        async Task Save()
        {
            if (!RegexHelper.IsValidNickname(CurrentUser.Nickname))
            {
                await Application.Current!.Windows[0].Page!.DisplayAlertAsync("失败", "昵称格式不正确！", "确定");
                return;
            }
            else if (!RegexHelper.IsValidPhone(CurrentUser.Phone))
            {
                await Application.Current!.Windows[0].Page!.DisplayAlertAsync("失败", "手机号格式不正确！", "确定");
                return;
            }
            else if (!RegexHelper.IsValidEmail(CurrentUser.Email))
            {
                await Application.Current!.Windows[0].Page!.DisplayAlertAsync("失败", "邮箱格式不正确！", "确定");
                return;
            }

            var resp = await _userApi.UpdateProfile(CurrentUser.Nickname, CurrentUser.Email, CurrentUser.Phone);
            if (resp?.IsSucc != true)
            {
                await Application.Current!.Windows[0].Page!.DisplayAlertAsync("失败", $"修改失败[{resp?.Msg}]！", "确定");
                return;
            }

            _globalCache.UpdateCurrentUser(currentUser);

            await _loginService.SetCurrentUser(currentUser);

            await Shell.Current.GoToAsync("..");

            await Application.Current!.Windows[0].Page!.DisplayAlertAsync("成功", "修改成功！", "确定");

            return;
        }






        async Task<FileResult?> PickImage()
        {
            return await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "选择图片",
                FileTypes = FilePickerFileType.Images
            });
        }

    }
}
