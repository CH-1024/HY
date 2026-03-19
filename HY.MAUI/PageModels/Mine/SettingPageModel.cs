using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Models;
using HY.MAUI.Pages.Contact;
using HY.MAUI.Pages.Mine;
using HY.MAUI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Mine
{
    public partial class SettingPageModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalCache _globalCache;



        public SettingPageModel(IServiceProvider serviceProvider, IGlobalCache globalCache)
        {
            _serviceProvider = serviceProvider;
            _globalCache = globalCache;
        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("..", true);
        }

        [RelayCommand]
        async Task Appearing()
        {

        }


        [RelayCommand]
        async Task Profile()
        {
            await Shell.Current.GoToAsync(nameof(ProfilePage), true);
        }


    }
}
