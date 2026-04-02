using HY.MAUI.Communication.Requests;
using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace HY.MAUI.Stores
{
    public class ContactRequestStore
    {
        public ObservableCollection<ContactRequestVM> ContactRequests { get; } = new();

        public void Upsert(ContactRequestVM contactRequestVM)
        {
            var existing = ContactRequests.FirstOrDefault(cr => cr.Id == contactRequestVM.Id);
            if (existing != null)
            {
                // Update existing
                existing.Sender_Id = contactRequestVM.Sender_Id;
                existing.Sender_Avatar = contactRequestVM.Sender_Avatar;
                existing.Sender_Nickname = contactRequestVM.Sender_Nickname;

                existing.Receiver_Id = contactRequestVM.Receiver_Id;
                existing.Receiver_Avatar = contactRequestVM.Receiver_Avatar;
                existing.Receiver_Nickname = contactRequestVM.Receiver_Nickname;

                existing.Message = contactRequestVM.Message;
                existing.Source = contactRequestVM.Source;
                existing.Relation_Request_Status = contactRequestVM.Relation_Request_Status;
                existing.Created_At = contactRequestVM.Created_At;
                existing.Handled_At = contactRequestVM.Handled_At;
            }
            else
            {
                // Add new
                ContactRequests.Add(contactRequestVM);
            }
        }

        public void Clear()
        {
            ContactRequests.Clear();
        }

    }
}
