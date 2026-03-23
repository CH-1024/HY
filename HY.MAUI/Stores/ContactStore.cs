using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace HY.MAUI.Stores
{
    public class ContactStore
    {
        public ObservableCollection<ContactVM> Contacts { get; } = new();

        public ContactVM? GetChat(long contactId)
        {
            return Contacts.FirstOrDefault(x => x.Contact_Id == contactId);
        }

        public void Upsert(ContactVM contact)
        {
            var old = Contacts.FirstOrDefault(x => x.Contact_Id == contact.Contact_Id);
            if (old == null) Contacts.Add(contact);
            else Contacts.Insert(Contacts.IndexOf(old), contact);
        }

        public void Clear()
        {
            Contacts.Clear();
        }
    }
}
