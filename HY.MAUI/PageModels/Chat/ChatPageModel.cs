using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication.Http;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Dtos;
using HY.MAUI.Models;
using HY.MAUI.Pages.Chat;
using HY.MAUI.Services.Interfaces;
using HY.MAUI.Stores;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace HY.MAUI.PageModels.Chat
{
    public partial class ChatPageModel : ObservableObject
    {
        private bool _isNavigatedTo;
        private bool _dataLoaded;
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalCache _globalCache;
        private readonly ChatApi _chatApi;
        private readonly ChatStore _chatStore;


        private bool isBusy;
        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        private ObservableCollection<ChatVM> chatCollection = null;
        public ObservableCollection<ChatVM> ChatCollection
        {
            get { return chatCollection; }
            set { SetProperty(ref chatCollection, value); }
        }

        private ChatVM? selectedChat = null;
        public ChatVM? SelectedChat
        {
            get { return selectedChat; }
            set { SetProperty(ref selectedChat, value); }
        }

        public ChatPageModel(IServiceProvider serviceProvider, IGlobalCache globalCache, ChatApi chatApi, ChatStore chatStore)
        {
            _serviceProvider = serviceProvider;
            _globalCache = globalCache;
            _chatApi = chatApi;
            _chatStore = chatStore;
        }

        private async Task LoadData()
        {
            try
            {
                IsBusy = true;

                ChatCollection = _chatStore.Chats;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        void NavigatedTo()
        {
            _isNavigatedTo = true;
            SelectedChat = null;
        }

        [RelayCommand]
        void NavigatedFrom() => _isNavigatedTo = false;

        [RelayCommand]
        async Task Appearing()
        {
            if (!_dataLoaded)
            {
                ChatCollection = _chatStore.Chats;
                _dataLoaded = true;
            }
            //else if (!_isNavigatedTo)
            //{
            //    await Refresh();
            //}
        }


        [RelayCommand]
        async Task Refresh()
        {
            if (_isNavigatedTo)
            {
                IsBusy = true;

                await Task.Delay(2000);

                IsBusy = false;
            }
        }


        [RelayCommand]
        async Task SelectionChanged()
        {
            if (SelectedChat == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "ChatInfo", SelectedChat }
            };
            await Shell.Current.GoToAsync(nameof(MessagePage), true, parameters);
        }


    }
}
