using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace HY.MAUI.Communication.SendQueue
{
    public class MessageSendQueue
    {
        private readonly Channel<SendMessageTask> _channel = Channel.CreateUnbounded<SendMessageTask>();

        public ChannelWriter<SendMessageTask> Writer => _channel.Writer;

        public ChannelReader<SendMessageTask> Reader => _channel.Reader;
    }
}
