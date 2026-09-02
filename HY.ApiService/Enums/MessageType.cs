namespace HY.ApiService.Enums
{
    public enum MessageType
    {
        Text = 1,
        Image = 2,
        File = 3,
        Voice = 4,
        Video = 5,
        System = 6,     // 系统消息(发送人是自己，接收人是目标对象)
        VoiceCall = 7,
        VideoCall = 8
    }
}
