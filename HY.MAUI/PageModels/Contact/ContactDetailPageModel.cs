using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Contact
{
    public partial class ContactDetailPageModel : ObservableObject, IQueryAttributable
    {
        private readonly IServiceProvider _serviceProvider;



        private ContactVM contactInfo = null;
        public ContactVM ContactInfo
        {
            get { return contactInfo; }
            set { SetProperty(ref contactInfo, value); }
        }



        public ContactDetailPageModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
        
        }

    }
}
