using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HY.MAUI.Models;

namespace HY.MAUI.PageModels.Contact;

public class SearchLocalContactPageModel : ObservableObject
{
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


    public SearchLocalContactPageModel()
    {

    }

}