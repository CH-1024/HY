using HY.MAUI.Communication.Requests;
using HY.MAUI.Dtos;
using HY.MAUI.Enums;
using HY.MAUI.Mapping;
using HY.MAUI.Models;
using HY.MAUI.Services;
using HY.MAUI.Services.Interfaces;
using HY.MAUI.Stores;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace HY.MAUI.Communication.Http
{
    public class ContactApi : BaseApi
    {
        private readonly IGlobalCache _globalCache;

        private readonly ContactRequestStore _contactRequestStore;
        private readonly ContactStore _contactStore;
        private readonly ChatStore _chatStore;
        private readonly MessageStore _messageStore;

        public ContactApi(HttpClient http, IGlobalCache globalCache, ContactRequestStore contactRequestStore, ContactStore contactStore, ChatStore chatStore, MessageStore messageStore) : base(http)
        {
            _globalCache = globalCache;

            _contactRequestStore = contactRequestStore;
            _contactStore = contactStore;
            _chatStore = chatStore;
            _messageStore = messageStore;
        }


        public async Task<Response?> GetContactRequests()
        {
            return await GetAsync(ApiUrl.GetContactRequests);
        }

        public async Task<Response?> GetContacts()
        {
            return await GetAsync(ApiUrl.GetContacts);
        }

        public async Task<Response?> GetContact(long targetId)
        {
            return await GetAsync($"{ApiUrl.GetContact}?targetId={targetId}");
        }

        public async Task<Response?> SearchContact(string identity)
        {
            return await GetAsync($"{ApiUrl.SearchContact}?identity={identity}");
        }

        public async Task RequestContact(long contactId, int source, string msg = "")
        {
            var resp = await PostAsync($"{ApiUrl.RequestContact}?contactId={contactId}&source={source}&message={msg}");
            if (resp?.IsSucc != true) return;

            var currentUser = _globalCache.GetCurrentUser();
            var contactRequestDto = resp.GetValue<ContactRequestDto>("ContactRequest")!;
            var contactDto = resp.GetValue<ContactDto>("Contact");
            var chatDto = resp.GetValue<ChatDto>("Chat");
            var messageDto = resp.GetValue<MessageDto>("Message");

            _contactRequestStore.Upsert(contactRequestDto.ToVM(currentUser.Id));

            if (contactRequestDto.Relation_Request_Status == RelationRequestStatus.Accepted)
            {
                _contactStore.Upsert(contactDto!.ToVM());

                _chatStore.UpsertAndSetTop(chatDto!.ToVM());

                _messageStore.Add(chatDto!.Id, messageDto!.ToVM(currentUser.Id));
            }
        }

        public async Task RespondContact(long contactRequestId, RespondContactHandle handle, string msg = "")
        {
            var resp = await PostAsync($"{ApiUrl.RespondContact}?contactRequestId={contactRequestId}&handle={handle}&message={msg}");
            if (resp?.IsSucc != true) return;

            var currentUser = _globalCache.GetCurrentUser();
            var contactRequestDto = resp.GetValue<ContactRequestDto>("ContactRequest")!;
            var contactDto = resp.GetValue<ContactDto>("Contact");
            var chatDto = resp.GetValue<ChatDto>("Chat");
            var messageDto = resp.GetValue<MessageDto>("Message");

            _contactRequestStore.Upsert(contactRequestDto.ToVM(currentUser.Id));

            if (contactRequestDto.Relation_Request_Status == RelationRequestStatus.Accepted)
            {
                _contactStore.Upsert(contactDto!.ToVM());

                _chatStore.UpsertAndSetTop(chatDto!.ToVM());

                _messageStore.Add(chatDto!.Id, messageDto!.ToVM(currentUser.Id));
            }
        }

        public async Task<Response?> DeleteContact(long targetId)
        {
            return await DeleteAsync($"{ApiUrl.DeleteContact}?targetId={targetId}");
        }
    }

}
