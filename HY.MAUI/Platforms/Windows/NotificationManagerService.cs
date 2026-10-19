using HY.MAUI.Services.Interfaces;

namespace HY.MAUI.Platforms.Windows;

public class NotificationManagerService : INotificationManagerService
{
    
    public event EventHandler? NotificationReceived;

    public NotificationManagerService()
    {
    }
    
    public void SendNotification(string title, string message, DateTime? notifyTime = null)
    {        
    }

    public void ReceiveNotification(string title, string message)
    {
    }
}