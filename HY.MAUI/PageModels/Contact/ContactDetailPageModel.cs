using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication.Http;
using HY.MAUI.Dtos;
using HY.MAUI.Enums;
using HY.MAUI.Models;
using HY.MAUI.Pages.Chat;
using HY.MAUI.Stores;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Contact
{
    public partial class ContactDetailPageModel : ObservableObject, IQueryAttributable
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly ChatStore _chatStore;
        private readonly ContactStore _contactStore;
        private readonly ContactApi _contactApi;



        private ContactVM contactInfo = null;
        public ContactVM ContactInfo
        {
            get { return contactInfo; }
            set { SetProperty(ref contactInfo, value); }
        }



        public ContactDetailPageModel(IServiceProvider serviceProvider, ChatStore chatStore, ContactStore contactStore, ContactApi contactApi)
        {
            _serviceProvider = serviceProvider;
            _chatStore = chatStore;
            _contactStore = contactStore;
            _contactApi = contactApi;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            ContactInfo = (ContactVM)query["ContactInfo"];
        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("..", true);
        }


        [RelayCommand]
        async Task SendMessage()
        {
            var chat = _chatStore.Chats.FirstOrDefault(v => v.Type == ChatType.Private && v.Target_Id == ContactInfo.Contact_Id);
            if (chat == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "ChatInfo", chat }
            };
            await Shell.Current.GoToAsync(nameof(MessagePage), true, parameters);
        }

        [RelayCommand]
        async Task DeleteContact()
        {
            var confirm = await Application.Current!.Windows[0].Page!.DisplayAlertAsync("删除联系人", "是否删除联系人?", "是", "否");
            if (!confirm) return;

            var resp = await _contactApi.DeleteContact(ContactInfo.Contact_Id);
            if (resp?.IsSucc == true)
            {
                _chatStore.Remove(ChatType.Private, ContactInfo.Contact_Id);
                _contactStore.Remove(ContactInfo.Contact_Id);
            }
        }
    }
}
