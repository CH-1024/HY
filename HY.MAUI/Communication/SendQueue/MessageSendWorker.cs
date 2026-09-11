using HY.MAUI.Communication.Http;
using HY.MAUI.Enums;
using HY.MAUI.Models.MsgVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Communication.SendQueue
{
    public class MessageSendWorker
    {
        private readonly MessageSendQueue _queue;
        private readonly FileApi _fileApi;
        private readonly MessageApi _messageApi;

        public MessageSendWorker(MessageSendQueue queue, FileApi fileApi, MessageApi messageApi)
        {
            _queue = queue;
            _fileApi = fileApi;
            _messageApi = messageApi;
        }

        public async Task RunAsync(CancellationToken token)
        {
            await foreach (var task in _queue.Reader.ReadAllAsync(token))
            {
                await ProcessAsync(task, token);
            }
        }

        private async Task ProcessAsync(SendMessageTask task, CancellationToken token)
        {
            try
            {
                task.Status = SendTaskStatus.Uploading;

                if (task.Message is ImageMessageVM imageVM)
                {
                    // 上传文件
                    var resp = await _fileApi.UploadImage(task.file, token);

                    imageVM.File_Id = resp!.GetValue<string>("File_Id");
                }
                else if (task.Message is VideoMessageVM videoVM)
                {
                    // 上传文件
                    var resp = await _fileApi.UploadVideo(task.file, token);

                    videoVM.File_Id = resp!.GetValue<string>("File_Id");
                }

                task.Status = SendTaskStatus.Sending;

                // 发送消息
                await _messageApi.SendMessage(task.Message);

                task.Status = SendTaskStatus.Sent;
            }
            catch (Exception ex)
            {
                task.Status = SendTaskStatus.Failed;
                task.Error = ex;
            }
        }
    }
}
