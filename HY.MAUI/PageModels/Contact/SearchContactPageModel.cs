using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication;
using HY.MAUI.Communication.Http;
using HY.MAUI.Dtos;
using HY.MAUI.Enums;
using HY.MAUI.Mapping;
using HY.MAUI.Models;
using HY.MAUI.Pages.Contact;
using HY.MAUI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace HY.MAUI.PageModels.Contact
{
    public partial class SearchContactPageModel : ObservableObject
    {
        private bool _isNavigatedTo;

        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalCache _globalCache;
        private readonly ContactApi _contactApi;


        private bool isBusy;
        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        private ObservableCollection<ContactVM> contactCollection = null;
        public ObservableCollection<ContactVM> ContactCollection
        {
            get { return contactCollection; }
            set { SetProperty(ref contactCollection, value); }
        }

        private ContactVM? selectedContact = null;
        public ContactVM? SelectedContact
        {
            get { return selectedContact; }
            set { SetProperty(ref selectedContact, value); }
        }


        public SearchContactPageModel(IServiceProvider serviceProvider, IGlobalCache globalCache, ContactApi contactApi)
        {
            _serviceProvider = serviceProvider;
            _globalCache = globalCache;
            _contactApi = contactApi;

        }


        [RelayCommand]
        void NavigatedTo()
        {
            _isNavigatedTo = true;
            SelectedContact = null;
        }

        [RelayCommand]
        void NavigatedFrom() => _isNavigatedTo = false;

        [RelayCommand]
        async Task SearchContact(string identity)
        {
            var resp = await _contactApi.SearchContact(identity);
            if (resp?.IsSucc == true)
            {
                var contacts = resp.GetValue<List<ContactDto>>("Contacts");

                ContactCollection = new ObservableCollection<ContactVM>(contacts.Select(c => c.ToVM()));
            }
        }

        [RelayCommand]
        async Task SelectionChanged()
        {
            if (SelectedContact == null) return;

            if (SelectedContact.Relation_Status == RelationStatus.Accepted)
            {
                // 是联系人
                var parameters = new Dictionary<string, object>
                {
                    { "ContactInfo", SelectedContact }
                };

                await Shell.Current.GoToAsync(nameof(ContactDetailPage), true, parameters);
            }
            else if (SelectedContact.Relation_Status == RelationStatus.None)
            {
                // 是陌生人
                var parameters = new Dictionary<string, object>
                {
                    { "ContactInfo", SelectedContact }
                };

                await Shell.Current.GoToAsync(nameof(StrangerDetailPage), true, parameters);
            }
        }


    }
}
