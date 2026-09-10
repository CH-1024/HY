using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Communication.SendQueue
{
    public class MessageSendService
    {
        private readonly MessageSendQueue _sendQueue;

        public MessageSendService(MessageSendQueue sendQueue)
        {
            _sendQueue = sendQueue;
        }

        public async Task EnqueueAsync(SendMessageTask task)
        {
            await _sendQueue.Writer.WriteAsync(task);
        }
    }
}
