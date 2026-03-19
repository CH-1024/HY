using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Contact
{
    public partial class StrangerDetailPageModel : ObservableObject, IQueryAttributable
    {
        private readonly IServiceProvider _serviceProvider;



        private StrangerVM strangerInfo = null;
        public StrangerVM StrangerInfo
        {
            get { return strangerInfo; }
            set { SetProperty(ref strangerInfo, value); }
        }



        public StrangerDetailPageModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            StrangerInfo = (StrangerVM)query["StrangerInfo"];
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
