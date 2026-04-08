using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication;
using HY.MAUI.Communication.Http;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Dtos;
using HY.MAUI.Enums;
using HY.MAUI.Mapping;
using HY.MAUI.Models;
using HY.MAUI.Pages.Contact;
using HY.MAUI.Services.Interfaces;
using HY.MAUI.Stores;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace HY.MAUI.PageModels.Contact
{
    public partial class SearchContactPageModel : ObservableObject
    {
        private bool _isNavigatedTo;
        private bool _dataLoaded;

        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalCache _globalCache;
        private readonly ContactApi _contactApi;
        private readonly ContactStore _contactStore;
        private readonly ContactRequestStore _contactRequestStore;

        private readonly ChatHubSignalR _chatHub;

        private bool isBusy;
        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        private ObservableCollection<ContactRequestVM> contactRequestCollection = null;
        public ObservableCollection<ContactRequestVM> ContactRequestCollection
        {
            get { return contactRequestCollection; }
            set { SetProperty(ref contactRequestCollection, value); }
        }


        public SearchContactPageModel(IServiceProvider serviceProvider, IGlobalCache globalCache, ContactApi contactApi, ContactStore contactStore, ContactRequestStore contactRequestStore, ChatHubSignalR chatHub)
        {
            _serviceProvider = serviceProvider;
            _globalCache = globalCache;
            _contactApi = contactApi;
            _contactStore = contactStore;
            _contactRequestStore = contactRequestStore;

            _chatHub = chatHub;
        }


        private async Task LoadData()
        {
            try
            {
                IsBusy = true;
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
        }

        [RelayCommand]
        void NavigatedFrom() => _isNavigatedTo = false;

        [RelayCommand]
        async Task Appearing()
        {
            if (!_dataLoaded)
            {
                ContactRequestCollection = _contactRequestStore.ContactRequests;
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
        async Task SearchContact(string identity)
        {
            var resp = await _contactApi.SearchContact(identity);
            if (resp?.IsSucc == true)
            {
                var contact = resp.GetValue<ContactDto>("Contact");

                if (contact.Relation_Status == RelationStatus.Friend)
                {
                    // 是联系人
                    var parameters = new Dictionary<string, object>
                    {
                        { "ContactInfo", contact.ToVM() }
                    };

                    await Shell.Current.GoToAsync(nameof(ContactDetailPage), true, parameters);
                }
                else
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "Source", 2 },
                        { "ContactInfo", contact.ToVM() },
                    };
                    await Shell.Current.GoToAsync(nameof(StrangerDetailPage), true, parameters);
                }
            }
            else
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", resp?.Msg, "OK");
            }
        }


        [RelayCommand]
        async Task RespondContact_Accepted(ContactRequestVM contactRequest)
        {
            await _chatHub.RespondContact(contactRequest.Id, RespondContactHandle.Accepted);
        }

        [RelayCommand]
        async Task RespondContact_Declined(ContactRequestVM contactRequest)
        {
            await _chatHub.RespondContact(contactRequest.Id, RespondContactHandle.Declined);
        }

        [RelayCommand]
        async Task RespondContact_Revoked(ContactRequestVM contactRequest)
        {
            await _chatHub.RespondContact(contactRequest.Id, RespondContactHandle.Revoked);
        }

        [RelayCommand]
        async Task ContactDetail(ContactRequestVM contactRequest)
        {
            long contactId;
            if (contactRequest.IsSelf)
            {
                contactId = contactRequest.Receiver_Id;
            }
            else
            {
                contactId = contactRequest.Sender_Id;
            }

            var resp = await _contactApi.GetContact(contactId);
            if (resp?.IsSucc == true)
            {
                var parameters = new Dictionary<string, object>();
                var contactDto = resp.GetValue<ContactDto>("Contact")!;

                if (contactDto.Relation_Status == RelationStatus.Friend)
                {
                    // 是联系人
                    parameters.Add("ContactInfo", contactDto.ToVM());
                    await Shell.Current.GoToAsync(nameof(ContactDetailPage), true, parameters);
                }
                else
                {
                    // 是陌生人
                    parameters.Add("Source", 1);
                    parameters.Add("ContactInfo", contactDto.ToVM());
                    await Shell.Current.GoToAsync(nameof(StrangerDetailPage), true, parameters);
                }
            }

        }
    }
}
