using HY.MAUI.Enums;
using HY.MAUI.Mapping;
using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace HY.MAUI.Communication.Http
{
    public class MessageApi : BaseApi
    {
        public MessageApi(HttpClient http) : base(http)
        {

        }


        public async Task<Response?> GetMessages(long chatId, long skipMessageId = 0, int take = 50)
        {
            return await GetAsync($"{ApiUrl.GetMessages}?chatId={chatId}&skipMessageId={skipMessageId}&take={take}");
        }

        public async Task SendMessage(ChatVM chatVM, MessageVM messageVM)
        {
            try
            {
                var resp = await PostAsJsonAsync(ApiUrl.SendMessage, messageVM.ToDto());
                if (resp?.IsSucc == true)
                {
                    var msgId = resp.GetValue<long>("MessageId");
                    var createdAt = resp.GetValue<DateTime>("CreatedAt");

                    chatVM.Last_Msg_Id = msgId;
                    chatVM.Last_Msg_Time = createdAt;

                    messageVM.Id = msgId;
                    messageVM.Created_At = createdAt;
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

                var resp = await PostAsync($"{ApiUrl.RecallMessage}?messageId={messageVM.Id}");
                if (resp?.IsSucc == true)
                {
                    messageVM.Message_Status = MessageStatus.Recalled;
                }
                else
                {
                    // 撤回失败，还原消息状态
                    messageVM.Message_Status = statusOld;
                }
            }
            catch (Exception)
            {
                // 撤回失败，还原消息状态
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

                var resp = await PostAsync($"{ApiUrl.DeleteMessage}?messageId={messageVM.Id}");
                if (resp?.IsSucc == true)
                {
                    messageVM.Message_Status = MessageStatus.Deleted;
                }
                else
                {
                    // 删除失败，还原消息状态
                    messageVM.Message_Status = statusOld;
                }
            }
            catch (Exception)
            {
                // 删除失败，还原消息状态
                messageVM.Message_Status = statusOld;
            }
            finally
            {
                if (chatVM.Last_Msg_Id == messageVM.Id) chatVM.Last_Msg_Status = messageVM.Message_Status;
            }
        }

    }
}
