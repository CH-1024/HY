using CommunityToolkit.Maui.Core.Extensions;
using HY.MAUI.Enums;
using HY.MAUI.Interfaces;
using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace HY.MAUI.Stores
{
    public class ContactRequestStore
    {
        readonly IGlobalCache _globalCache;

        public ContactRequestStore(IGlobalCache globalCache)
        {
            _globalCache = globalCache;
        }

        public ObservableCollection<ContactRequestVM> ContactRequests { get; } = new();

        public void Upsert(ContactRequestVM contactRequestVM)
        {
            var existing = ContactRequests.FirstOrDefault(cr => cr.Sender_Id == contactRequestVM.Sender_Id && cr.Receiver_Id == contactRequestVM.Receiver_Id);
            if (existing != null)
            {
                // Update existing
                existing.Id = contactRequestVM.Id;
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



        public int GetRequestCount()
        {
            var currentUser = _globalCache.GetCurrentUser();
            return ContactRequests.Where(cr => cr.Relation_Request_Status == RelationRequestStatus.Pending && cr.Sender_Id == currentUser.Id).Count();
        }

        public int GetRespondCount()
        {
            var currentUser = _globalCache.GetCurrentUser();
            return ContactRequests.Where(cr => cr.Relation_Request_Status == RelationRequestStatus.Pending  && cr.Receiver_Id == currentUser.Id).Count();
        }

        public ObservableCollection<ContactRequestVM> GetRequestCollection()
        {
            var currentUser = _globalCache.GetCurrentUser();
            return ContactRequests.Where(cr => /*cr.Relation_Request_Status == RelationRequestStatus.Pending &&*/ cr.Sender_Id == currentUser.Id).OrderByDescending(v => v.Created_At).ToObservableCollection();
        }

        public ObservableCollection<ContactRequestVM> GetRespondCollection()
        {
            var currentUser = _globalCache.GetCurrentUser();
            return ContactRequests.Where(cr => /*cr.Relation_Request_Status == RelationRequestStatus.Pending &&*/ cr.Receiver_Id == currentUser.Id).ToObservableCollection();
        }
    }
}
