using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication;
using HY.MAUI.Communication.Http;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Dtos;
using HY.MAUI.Enums;
using HY.MAUI.Mapping;
using HY.MAUI.Models;
using HY.MAUI.Models.MsgVM;
using HY.MAUI.PageModels.Chat.MessageCommands;
using HY.MAUI.Pages.Chat;
using HY.MAUI.Pages.Contact;
using HY.MAUI.Services.Interfaces;
using HY.MAUI.Stores;
using HY.MAUI.Tools;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace HY.MAUI.PageModels.Chat
{
    public partial class MessagePageModel : ObservableObject, IQueryAttributable
    {
        private bool _isNavigatedTo;
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalCache _globalCache;
        private readonly IDispatcher _dispatcher;

        private readonly ChatHubSignalR _chatHub;

        private readonly ChatStore _chatStore;
        private readonly MessageStore _messageStore;
        private readonly ContactStore _contactStore;

        private readonly ChatApi _chatApi;
        private readonly MessageApi _messageApi;
        private readonly ContactApi _contactApi;
        private readonly FileApi _fileApi;

        bool _isTop = false;
        CollectionView _collectionView = null;
        ChatVM _currentChat = null;
        UserVM _currentUser = null;

        private bool isLoading;
        public bool Isloading
        {
            get { return isLoading; }
            set { SetProperty(ref isLoading, value); }
        }

        private bool showUnread;
        public bool ShowUnread
        {
            get { return showUnread; }
            set { SetProperty(ref showUnread, value); }
        }

        private int unreadCount;
        public int UnreadCount
        {
            get { return unreadCount; }
            set { SetProperty(ref unreadCount, value); }
        }


        private string? title;
        public string? Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        private string inputText = "";
        public string InputText
        {
            get { return inputText; } 
            set { SetProperty(ref inputText, value); }
        }

        private ObservableCollection<MessageVM> messageCollection = null;
        public ObservableCollection<MessageVM> MessageCollection
        {
            get { return messageCollection; }
            set { SetProperty(ref messageCollection, value); }
        }

        public MessagePageModel(IServiceProvider serviceProvider, IGlobalCache globalCache, IDispatcher dispatcher, ChatHubSignalR chatHub, 
                                ChatStore chatStore, MessageStore messageStore, ContactStore contactStore, ChatApi chatApi, MessageApi messageApi, ContactApi contactApi, FileApi fileApi)
        {
            _serviceProvider = serviceProvider;
            _globalCache = globalCache;
            _dispatcher = dispatcher;

            _chatHub = chatHub;

            _chatStore = chatStore;
            _messageStore = messageStore;
            _contactStore = contactStore;

            _chatApi = chatApi;
            _messageApi = messageApi;
            _contactApi = contactApi;
            _fileApi = fileApi;
        }

        void MessageCollection_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                if (_collectionView != null && e.NewItems != null && e.NewItems.Count > 0 && e.NewStartingIndex > 0)
                {
                    var newMessage = e.NewItems[0] as MessageVM;

                    if (newMessage is TextMessageVM textMessageVM)
                    {
                        _currentChat.Last_Msg_Time = textMessageVM.Created_At;
                        _currentChat.Last_Msg_Brief = textMessageVM.Content?.Length > 20 ? textMessageVM.Content.Substring(0, 20) + "..." : textMessageVM.Content;
                        _currentChat.Last_Msg_Status = textMessageVM.Message_Status;
                        _currentChat.Is_Deleted = false;
                    }
                    else if (newMessage is ImageMessageVM imageMessageVM)
                    {
                        _currentChat.Last_Msg_Time = imageMessageVM.Created_At;
                        //_currentChat.Last_Msg_Brief = "[图片]";
                        _currentChat.Last_Msg_Status = imageMessageVM.Message_Status;
                        _currentChat.Is_Deleted = false;
                    }

                    UI.Run(async() =>
                    {
                        await Task.Delay(100);
                        _collectionView.ScrollTo(newMessage, position: ScrollToPosition.End, animate: true);
                    });

                }
            }
        }

        bool ChatHub_OnReceiveMessage_ChatHub(MessageDto messageDto)
        {
            if (messageDto.Chat_Type != _currentChat.Type) return false;
            else if (messageDto.Chat_Type == ChatType.Private && _currentChat.Target_Id != messageDto.Sender_Id) return false;
            else if (messageDto.Chat_Type == ChatType.Group && _currentChat.Target_Id != messageDto.Target_Id) return false;

            UI.Run(async() =>
            {
                _currentChat.Unread_Count = 0;

                if (_lastVisibleItemIndex <= MessageCollection.Count - 2)
                {
                    UnreadCount = 0;
                    ShowUnread = false;

                    await Task.Delay(100);
                    _collectionView.ScrollTo(MessageCollection.LastOrDefault(), position: ScrollToPosition.End, animate: true);
                }
                else
                {
                    UnreadCount++;
                    ShowUnread = true;
                }
            });

            return true;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _currentChat = (ChatVM)query["ChatInfo"];
            _currentUser = _globalCache.GetCurrentUser();
            Title = _currentChat?.Target_Name;

        }


        [RelayCommand]
        void NavigatedTo(NavigatedToEventArgs e)
        {
            _isNavigatedTo = true;
        }

        [RelayCommand]
        void NavigatedFrom(NavigatedFromEventArgs e)
        {
            if (e.DestinationPage != null && e.DestinationPage.GetType().FullName == "HY.MAUI.Pages.Chat.ChatPage")
            {
                _isNavigatedTo = false;
            }
        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("..", true);
        }

        [RelayCommand]
        void Appearing()
        {
            if (_isNavigatedTo) return;

            MessageCollection = _messageStore.GetMessages(_currentChat.Id);

            MessageCollection.CollectionChanged += MessageCollection_CollectionChanged;
        }

        [RelayCommand]
        void Disappearing()
        {
            if (_isNavigatedTo) return;

            MessageCollection.CollectionChanged -= MessageCollection_CollectionChanged;
        }

        [RelayCommand]
        async Task Loaded()
        {
            if (_isNavigatedTo) return;

            _chatHub.OnReceiveMessage_ChatHub += ChatHub_OnReceiveMessage_ChatHub;

            if (_currentChat.Unread_Count != 0)
            {
                var resp = await _chatApi.ReadAll(_currentChat.Id);
                if (resp?.IsSucc == true) _currentChat.Unread_Count = 0;
            }
        }

        [RelayCommand]
        void Unloaded()
        {
            if (_isNavigatedTo) return;

            _chatHub.OnReceiveMessage_ChatHub -= ChatHub_OnReceiveMessage_ChatHub;
        }

        [RelayCommand]
        async Task Refresh()
        {
            var takeCount = 50;
            var resp = await _messageApi.GetMessages(_currentChat.Id, MessageCollection.First().Id, takeCount);
            if (resp?.IsSucc == true)
            {
                var messageDtos = resp.GetValue<List<MessageDto>>("Messages") ?? [];

                if (messageDtos.Count < takeCount) _isTop = true;

                foreach (var messageDto in messageDtos)
                {
                    var messageVM = messageDto.ToVM(_currentUser.Id);
                    MessageCollection.Insert(0, messageVM);
                }
            }
        }

        [RelayCommand]
        void CollectionViewLoaded(CollectionView collectionView)
        {
            if (_isNavigatedTo) return;

            _collectionView = collectionView;
            _collectionView?.ScrollTo(MessageCollection.LastOrDefault(), animate: false);
        }

        int _lastVisibleItemIndex;
        [RelayCommand]
        async Task CollectionViewScrolled(ItemsViewScrolledEventArgs args)
        {
            InputText = $"{args.FirstVisibleItemIndex}";
            _lastVisibleItemIndex = args.LastVisibleItemIndex;

            if (args.VerticalDelta < 0 && _lastVisibleItemIndex <= MessageCollection.Count - 1)
            {
                UnreadCount = 0;
                ShowUnread = false;
            }

            if (args.VerticalDelta < 0 && args.FirstVisibleItemIndex <= 5 && !RefreshCommand.IsRunning && !_isTop)
            {
                await RefreshCommand.ExecuteAsync(null);
            }
        }

        [RelayCommand]
        void SelectionChanged(MessageVM message)
        {
            ;
        }



        [RelayCommand]
        async Task Invocation(MessageCommandInvocation cmd)
        {
            if (cmd?.Command == CommandNames.ContactDetail)
            {
                await ContactDetailCommand.ExecuteAsync(cmd.Message);
            }
            else if (cmd?.Command == CommandNames.DeleteMessage)
            {
                await DeleteMessageCommand.ExecuteAsync(cmd.Message);
            }
            else if (cmd?.Command == CommandNames.RecallMessage)
            {
                await RecallMessageCommand.ExecuteAsync(cmd.Message);
            }
            else if (cmd?.Command == CommandNames.TapImageMessage)
            {
                await TapImageMessageCommand.ExecuteAsync(cmd.Message);
            }
        }

        [RelayCommand]
        async Task ContactDetail(MessageVM message)
        {
            var parameters = new Dictionary<string, object>();
            if (message.IsSelf)
            {
                //// 查看自己信息
                //parameters.Add("UserInfo", _currentUser);
                //await Shell.Current.GoToAsync(nameof(UserDetailPage), true, parameters);
                return;
            }
            else
            {
                var resp = await _contactApi.GetContact(message.Sender_Id);
                if (resp?.IsSucc == true)
                {
                    var contactDto = resp.GetValue<ContactDto>("Contact");

                    if (contactDto.Relation_Status == RelationStatus.Friend)
                    {
                        // 是联系人
                        parameters.Add("ContactInfo", contactDto.ToVM());
                        await Shell.Current.GoToAsync(nameof(ContactDetailPage), true, parameters);
                    }
                    else
                    {
                        // 是陌生人
                        parameters.Add("ContactInfo", contactDto.ToVM());
                        await Shell.Current.GoToAsync(nameof(StrangerDetailPage), true, parameters);
                    }
                }
            }
        }

        [RelayCommand]
        async Task DeleteMessage(MessageVM message)
        {
            await _chatHub.DeleteMessage(_currentChat, message);
        }

        [RelayCommand]
        async Task RecallMessage(MessageVM message)
        {
            await _chatHub.RecallMessage(_currentChat, message);
        }

        [RelayCommand]
        async Task TapImageMessage(MessageVM message)
        {
            if (message is ImageMessageVM imageMessage)
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Compressed_Image_Url", imageMessage.Compressed_Image_Url }
                };
                await Shell.Current.GoToAsync(nameof(ImagePreviewPage), false, parameters);
            }
        }

        [RelayCommand]
        async Task SendText()
        {
            if (string.IsNullOrWhiteSpace(InputText)) return;

            var textMessageVM = CreateTextMessageVM();

            MessageCollection.Add(textMessageVM);

            await _chatHub.SendMessage(_currentChat, textMessageVM);

            // 清空输入框
            InputText = "";

        }

        [RelayCommand]
        async Task SendImage()
        {
            #region
            //var photoResults = await FilePicker.Default.PickMultipleAsync(new PickOptions
            //{
            //    PickerTitle = "选择图片",
            //    FileTypes = FilePickerFileType.Images
            //});

            //if (photoResults.Count() == 0) return;

            //foreach (var photoResult in photoResults)
            //{
            //    var progress = new Progress<double>(p => _ = p);

            //    var resp = await _fileApi.Upload(photoResult, progress);
            //    if (resp?.IsSucc == true)
            //    {
            //        var originalUrl = resp.GetValue<string>("OriginalUrl");
            //        var thumbnailUrl = resp.GetValue<string>("ThumbnailUrl");

            //        var imageMessageVM = new ImageMessageVM
            //        {
            //            Chat_Type = _currentChat.Type,
            //            Sender_Id = _currentUser.Id,
            //            Sender_Avatar = _currentUser.Avatar,
            //            Target_Id = _currentChat.Target_Id,
            //            Created_At = DateTime.UtcNow,
            //            IsSelf = true,
            //            Message_Status = MessageStatus.Sending,

            //            Original_Image_Url = originalUrl,
            //            Thumbnail_Image_Url = thumbnailUrl,
            //        };

            //        _ = _dispatcher.DispatchAsync(async () =>
            //        {
            //            MessageCollection.Add(imageMessageVM);

            //            await Task.Delay(50);

            //            _collectionView.ScrollTo(MessageCollection.LastOrDefault(), position: ScrollToPosition.End, animate: true);
            //        });

            //        _currentChat.Last_Msg_Time = imageMessageVM.Created_At;
            //        //_currentChat.Last_Msg_Brief = "[图片]";
            //        _currentChat.Last_Msg_Status = imageMessageVM.Message_Status;
            //        _currentChat.Is_Deleted = false;

            //        // 清空输入框
            //        InputText = "";

            //        await _chatHub.SendMessage(_currentChat, imageMessageVM);

            //    }
            //}



            //var photoResults = await FilePicker.Default.PickMultipleAsync(new PickOptions
            //{
            //    PickerTitle = "选择图片",
            //    FileTypes = FilePickerFileType.Images
            //});

            //if (photoResults == null || !photoResults.Any()) return;

            //// 最大并发数
            //var semaphore = new SemaphoreSlim(3);

            //var tasks = photoResults.Select(async photoResult =>
            //{
            //    await semaphore.WaitAsync();

            //    try
            //    {
            //        var progress = new Progress<double>(p =>
            //        {
            //            UploadProgress = p;
            //        });

            //        var resp = await _fileApi.Upload(photoResult, progress);

            //        if (resp?.IsSucc == true)
            //        {
            //            // success logic
            //        }
            //    }
            //    finally
            //    {
            //        semaphore.Release();
            //    }
            //});

            //await Task.WhenAll(tasks);
            #endregion

            #region MyRegion


            //var photoResults = await FilePicker.Default.PickMultipleAsync(new PickOptions
            //{
            //    PickerTitle = "选择图片",
            //    FileTypes = FilePickerFileType.Images
            //});

            //if (photoResults == null || !photoResults.Any()) return;

            //// 最大并发数
            //var semaphore = new SemaphoreSlim(3);

            //photoResults.AsParallel()
            ////.WithDegreeOfParallelism(3) // 设置最大并行数
            //.ForAll(async photoResult =>
            //{
            //    var imageMessageVM = new ImageMessageVM
            //    {
            //        Chat_Type = _currentChat.Type,
            //        Sender_Id = _currentUser.Id,
            //        Sender_Avatar = _currentUser.Avatar,
            //        Target_Id = _currentChat.Target_Id,
            //        Created_At = DateTime.UtcNow,
            //        IsSelf = true,
            //        Message_Status = MessageStatus.Sending,

            //        UploadProgress = 0,
            //        //Original_Image_Url = originalUrl,
            //        //Thumbnail_Image_Url = thumbnailUrl,
            //    };

            //    MainThread.BeginInvokeOnMainThread(() =>
            //    {
            //        MessageCollection.Add(imageMessageVM);
            //    });

            //    await semaphore.WaitAsync();

            //    var progress = new Progress<double>(p =>
            //    {
            //        MainThread.BeginInvokeOnMainThread(() => imageMessageVM.UploadProgress = p);
            //    });

            //    var resp = await _fileApi.Upload(photoResult, progress);
            //    if (resp?.IsSucc == true)
            //    {
            //        MainThread.BeginInvokeOnMainThread(() =>
            //        {
            //            var originalUrl = resp.GetValue<string>("OriginalUrl");
            //            var thumbnailUrl = resp.GetValue<string>("ThumbnailUrl");
            //            imageMessageVM.Original_Image_Url = originalUrl;
            //            imageMessageVM.Thumbnail_Image_Url = thumbnailUrl;
            //            imageMessageVM.Message_Status = MessageStatus.Sented;
            //        });
            //    }
            //    else
            //    {
            //        MainThread.BeginInvokeOnMainThread(() =>
            //        {
            //            imageMessageVM.Message_Status = MessageStatus.Failed;
            //        });
            //    }

            //    await _chatHub.SendMessage(_currentChat, imageMessageVM);

            //    semaphore.Release();

            //});

            //// 清空输入框
            //InputText = "";



            ////await Parallel.ForEachAsync(photoResults,
            ////new ParallelOptions
            ////{
            ////    MaxDegreeOfParallelism = 3
            ////},
            ////async (photoResult, ct) =>
            ////{
            ////    var progress = new Progress<double>(p =>
            ////    {
            ////        //UploadProgress = p;
            ////    });

            ////    var resp = await _fileApi.Upload(photoResult, progress);
            ////});




            ////await Parallel.ForEachAsync(photoResults,
            ////new ParallelOptions
            ////{
            ////    MaxDegreeOfParallelism = 3
            ////},
            ////async (photoResult, ct) =>
            ////{
            ////    var progress = new Progress<double>(p =>
            ////    {
            ////        //UploadProgress = p;
            ////    });

            ////    var resp = await _fileApi.Upload(photoResult, progress);
            ////});
            #endregion


            var photoResults = await PickImages();
            if (photoResults.Count == 0) return;

            InputText = "";

            using var semaphore = new SemaphoreSlim(3);

            var tasks = new List<Task>();

            foreach (var photoResult in photoResults)
            {
                var imageMessageVM = CreateImageMessageVM();

                MessageCollection.Add(imageMessageVM);

                tasks.Add(UploadImageAsync(photoResult, imageMessageVM, semaphore));
            }

            await Task.WhenAll(tasks);
        }

        [RelayCommand]
        async Task SendVideo()
        {
            //(string videoName, string videoExtension, string videoContentType, byte[] iconBytes, byte[] videoData)? video = await PickVideoAsync();

            //if (video == null) return;
        }

        [RelayCommand]
        void ShowBottom()
        {
            ShowUnread = false;
            UnreadCount = 0;
            _collectionView.ScrollTo(MessageCollection.LastOrDefault(), position: ScrollToPosition.End, animate: true);
        }







        async Task<IReadOnlyList<FileResult>> PickImages()
        {
            var result = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "选择图片",
                FileTypes = FilePickerFileType.Images
            });

            return result?.ToList() ?? [];
        }

        TextMessageVM CreateTextMessageVM()
        {
            return new TextMessageVM
            {
                Chat_Type = _currentChat.Type,
                Sender_Id = _currentUser.Id,
                Sender_Avatar = _currentUser.Avatar,
                Target_Id = _currentChat.Target_Id,
                Created_At = DateTime.UtcNow,
                IsSelf = true,
                Message_Status = MessageStatus.Sending,

                Content = InputText.Trim(),
            };
        }

        ImageMessageVM CreateImageMessageVM()
        {
            return new ImageMessageVM
            {
                Chat_Type = _currentChat.Type,
                Sender_Id = _currentUser.Id,
                Sender_Avatar = _currentUser.Avatar,
                Target_Id = _currentChat.Target_Id,
                Created_At = DateTime.UtcNow,
                IsSelf = true,
                Message_Status = MessageStatus.Sending,

                UploadProgress = 0,
                //Original_Image_Url = originalUrl,
                //Thumbnail_Image_Url = thumbnailUrl,
            };
        }

        async Task UploadImageAsync(FileResult photoResult, ImageMessageVM vm, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();

            try
            {
                var progress = new Progress<double>(p => UI.Run(() => vm.UploadProgress = p));

                var resp = await _fileApi.UploadImage(photoResult, progress);
                if (resp?.IsSucc == true)
                {
                    UI.Run(() =>
                    {
                        vm.File_Id = resp.GetValue<string>("File_Id");
                        vm.Message_Status = MessageStatus.Sented;
                    });

                    await _chatHub.SendMessage(_currentChat, vm);
                }
                else
                {
                    UI.Run(() => vm.Message_Status = MessageStatus.Failed);
                }
            }
            catch
            {
                UI.Run(() => vm.Message_Status = MessageStatus.Failed);
            }
            finally
            {
                semaphore.Release();
            }
        }

    }
}
