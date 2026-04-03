using HY.MAUI.Communication.Auth;
using HY.MAUI.Communication.Http;
using HY.MAUI.Dtos;
using HY.MAUI.Enums;
using HY.MAUI.Mapping;
using HY.MAUI.Models;
using HY.MAUI.Models.MsgVM;
using HY.MAUI.Pages.Login;
using HY.MAUI.Services.Interfaces;
using HY.MAUI.Stores;
using HY.MAUI.Tools;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HY.MAUI.Communication.SignalR
{
    public class ChatHubSignalR : IAsyncDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalCache _globalCache;
        private readonly ILoginService _loginService;
        private readonly ITokenProvider _tokenProvider;

        private readonly ChatStore _chatStore;
        private readonly ContactStore _contactStore;
        private readonly ContactRequestStore _contactRequestStore;
        private readonly MessageStore _messageStore;

        private HubConnection? _connection = null;
        private readonly SemaphoreSlim _initLock = new(1, 1);


        public ChatHubSignalR(IServiceProvider serviceProvider, IGlobalCache globalCache, ILoginService loginService, ITokenProvider tokenProvider, ChatStore chatStore, ContactStore contactStore, ContactRequestStore contactRequestStore, MessageStore messageStore)
        {
            _serviceProvider = serviceProvider;
            _globalCache = globalCache;
            _loginService = loginService;
            _tokenProvider = tokenProvider;
            _chatStore = chatStore;
            _contactStore = contactStore;
            _contactRequestStore = contactRequestStore;
            _messageStore = messageStore;
        }


        public async Task<bool> InitializeAsync()
        {
            await _initLock.WaitAsync();
            try
            {
                if (_connection != null && _connection.State != HubConnectionState.Disconnected) return false;

                _connection = BuildConnection();

                RegisterLifecycleEvents();

                RegisterHubMethods();

                await StartAsync();

                return true;
            }
            catch
            {
                await DisposeAsync().AsTask();

                return false;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task StartAsync()
        {
            if (_connection != null && _connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync();
            }
        }

        public async Task StopAsync()
        {
            if (_connection != null && _connection.State != HubConnectionState.Disconnected)
            {
                await _connection.StopAsync();
            }
        }



        // InvokeAsync  等待服务器响应  有返回值  同步模式
        // SendAsync    不等待响应      无返回值  异步模式
        public async Task SendMessage(ChatVM chatVM, MessageVM messageVM)
        {
            try
            {
                var msgId = await _connection!.InvokeAsync<long>("SendMessage", messageVM.ToDto());
                if (msgId > 0)
                {
                    chatVM.Last_Msg_Id = msgId;

                    messageVM.Id = msgId;
                    messageVM.Message_Status = MessageStatus.Sented;
                }
                else
                {
                    messageVM.Message_Status = MessageStatus.Failed;
                }
            }
            catch (Exception)
            {
                // 发送失败
                messageVM.Message_Status = MessageStatus.Failed;
            }
            finally
            {
                if (chatVM.Last_Msg_Id == messageVM.Id) chatVM.Last_Msg_Status = messageVM.Message_Status;
            }
        }

        public async Task RecallMessage(ChatVM chatVM, MessageVM messageVM)
        {
            var statusOld = messageVM.Message_Status;
            try
            {
                messageVM.Message_Status = MessageStatus.Recalling;

                var isSucc = await _connection!.InvokeAsync<bool>("RecallMessage", messageVM.Id);
                if (isSucc)
                {
                    messageVM.Message_Status = MessageStatus.Recalled;
                }
                else
                {
                    // 发送失败，还原消息状态
                    messageVM.Message_Status = statusOld;
                }
            }
            catch (Exception)
            {
                // 发送失败，还原消息状态
                messageVM.Message_Status = statusOld;
            }
            finally
            {
                if (chatVM.Last_Msg_Id == messageVM.Id) chatVM.Last_Msg_Status = messageVM.Message_Status;
            }
        }

        public async Task DeleteMessage(ChatVM chatVM, MessageVM messageVM)
        {
            var statusOld = messageVM.Message_Status;
            try
            {
                messageVM.Message_Status = MessageStatus.Deleting;

                var isSucc = await _connection!.InvokeAsync<bool>("DeleteMessage", messageVM.Id);
                if (isSucc)
                {
                    messageVM.Message_Status = MessageStatus.Deleted;
                }
                else
                {
                    // 发送失败，还原消息状态
                    messageVM.Message_Status = statusOld;
                }
            }
            catch (Exception)
            {
                // 发送失败，还原消息状态
                messageVM.Message_Status = statusOld;
            }
            finally
            {
                if (chatVM.Last_Msg_Id == messageVM.Id) chatVM.Last_Msg_Status = messageVM.Message_Status;
            }
        }

        public async Task RequestContact(long contactId, string source, string message)
        {
            var contactRequestDto = await _connection!.InvokeAsync<ContactRequestDto>("RequestContact", contactId, source, message);
            if (contactRequestDto == null) return;

            var currentUser = _globalCache.GetCurrentUser();

            var contactRequestVM = contactRequestDto.ToVM(currentUser.Id);
            _contactRequestStore.Upsert(contactRequestVM);
        }

        public async Task RespondContact(long contactId, bool isAccept, string message)
        {
            await _connection!.SendAsync("RespondContact", contactId, isAccept, message);
        }


        public async ValueTask DisposeAsync()
        {
            UnregisterLifecycleEvents();
            UnregisterHubMethods();

            if (_connection != null)
            {
                await _connection.DisposeAsync().AsTask();
                _connection = null;
            }
        }





        //public void Recall(long chatId, long messageId)
        //{
        //    var messages = GetMessages(chatId);
        //    var message = messages.FirstOrDefault(m => m.Id == messageId);
        //    if (message != null)
        //    {
        //        message.Status = MessageStatus.Recalled;
        //    }
        //}

        //public void RemoveChat(long chatId)
        //{
        //    _messages.Remove(chatId);
        //}





        //public async Task Send(MessageVM message, long targetId)
        //{
        //    Add(message);

        //    var msgDto = message.ToDto(targetId);

        //    var msgId = DateTime.Now.Ticks; await Task.Delay(1000);
        //    //var msgId = await _chatHub.SendMessageAsync(msgDto);
        //    if (msgId > 0)
        //    {
        //        message.Id = msgId;
        //        message.Status = MessageStatus.Sent;
        //    }
        //    else
        //    {
        //        message.Status = MessageStatus.Failed;
        //    }
        //}







        private HubConnection BuildConnection()
        {
            var connection = new HubConnectionBuilder()
            .WithUrl(ApiUrl.ChatHub, options =>
            {
                options.CloseTimeout = TimeSpan.FromSeconds(3600);

                // 跳过 HTTPS 证书验证
                options.HttpMessageHandlerFactory = _ =>
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                // 使用 TokenProvider 获取访问令牌
                options.AccessTokenProvider = async () => await _tokenProvider.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect(
            [
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3),
            ])
            .Build();
            return connection;
        }

        private void RegisterLifecycleEvents()
        {
            _connection?.Reconnecting += OnReconnecting;
            _connection?.Reconnected += OnReconnected;
            _connection?.Closed += OnClosed;
        }

        private void RegisterHubMethods()
        {
            _connection?.On<MessageDto, bool>("ReceiveMessage", OnReceiveMessage);
            _connection?.On<MessageDto>("RecallMessage", OnRecallMessage);
            _connection?.On<ContactRequestDto>("RequestContact", OnRequestContact);
            _connection?.On<string, bool>("ForceLogout", OnForceLogout);
        }


        private void UnregisterLifecycleEvents()
        {
            _connection?.Reconnecting -= OnReconnecting;
            _connection?.Reconnected -= OnReconnected;
            _connection?.Closed -= OnClosed;
        }

        private void UnregisterHubMethods()
        {
            _connection?.Remove("ReceiveMessage");
            _connection?.Remove("RecallMessage");
            _connection?.Remove("RequestContact");
            _connection?.Remove("ForceLogout");
        }



        #region LifecycleEvents
        
        public event Action<Exception?> OnReconnecting_ChatHub;
        public event Action<string?> OnReconnected_ChatHub;
        public event Action<Exception?> OnClosed_ChatHub;

        private async Task OnReconnecting(Exception? exception)
        {
            UI.Run(() =>
            {
                // 正在重新连接中
                OnReconnecting_ChatHub?.Invoke(exception);
            });

            await Task.CompletedTask;
        }

        private async Task OnReconnected(string? connectionId)
        {
            UI.Run(() =>
            {
                // 已重新用新 Token 连上
                OnReconnected_ChatHub?.Invoke(connectionId);
            });

            await Task.CompletedTask;
        }

        private async Task OnClosed(Exception? exception)
        {
            await DisposeAsync().AsTask();

            UI.Run(() =>
            {
                OnClosed_ChatHub?.Invoke(exception);
            });
        }

        #endregion





        #region HubMethods

        public event Func<MessageDto, bool> OnReceiveMessage_ChatHub;

        private bool OnReceiveMessage(MessageDto messageDto)
        {
            var currentUser = _globalCache.GetCurrentUser();
            var chat = _chatStore.GetChat(currentUser.Id, messageDto);
            if (chat != null)
            {
                //MainThread.BeginInvokeOnMainThread(() =>
                //{
                //});

                var messageVM = messageDto.ToVM(currentUser.Id);
                _messageStore.Add(chat.Id, messageVM);

                chat.Last_Msg_Id = messageDto.Id;
                chat.Last_Msg_Time = messageDto.Created_At;
                chat.Last_Msg_Brief = messageDto.Content?.Length > 20 ? messageDto.Content.Substring(0, 20) + "..." : messageDto.Content;
                chat.Last_Msg_Status = messageDto.Message_Status;
                chat.Unread_Count += 1;
                chat.Is_Deleted = false;
            }
            else
            {
                return false;
            }

            if (OnReceiveMessage_ChatHub == null) return false;
            else return OnReceiveMessage_ChatHub.Invoke(messageDto);
        }

        private void OnRecallMessage(MessageDto messageDto)
        {
            var currentUser = _globalCache.GetCurrentUser();
            var chat = _chatStore.GetChat(currentUser.Id, messageDto);
            if (chat != null)
            {
                //MainThread.BeginInvokeOnMainThread(() =>
                //{
                //});

                var message = _messageStore.GetMessages(chat.Id).FirstOrDefault(m => m.Id == messageDto.Id);
                if (message != null)
                {
                    message.Message_Status = MessageStatus.Recalled;
                }
                if (chat.Last_Msg_Id == messageDto.Id)
                {
                    chat.Last_Msg_Status = MessageStatus.Recalled;
                    chat.Is_Deleted = false;
                }
            }
        }

        private void OnRequestContact(ContactRequestDto contactRequestDto)
        {
            var currentUser = _globalCache.GetCurrentUser();

            var contactRequestVM = contactRequestDto.ToVM(currentUser.Id);
            _contactRequestStore.Upsert(contactRequestVM);
        }

        private bool OnForceLogout(string message)
        {
            StopAsync().GetAwaiter().GetResult();

            return true;
        }

        #endregion


    }
}
