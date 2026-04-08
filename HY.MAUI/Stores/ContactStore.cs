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
            var existing = Contacts.FirstOrDefault(c => c.Contact_Id == contact.Contact_Id);
            if (existing != null)
            {
                // Update existing
                existing.Contact_Id = contact.Contact_Id;
                existing.Nickname = contact.Nickname;
                existing.Avatar = contact.Avatar;
                existing.Region = contact.Region;
                existing.Remark = contact.Remark;
                existing.Contact_Status = contact.Contact_Status;
                existing.Relation_Request_Status = contact.Relation_Request_Status;
                existing.Relation_Status = contact.Relation_Status;
            }
            else
            {
                // Add new
                Contacts.Add(contact);
            }
        }

        public bool Remove(long contactId)
        {
            var contact = GetChat(contactId);
            return Contacts.Remove(contact);
        }

        public void Clear()
        {
            Contacts.Clear();
        }
    }
}
