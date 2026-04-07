using Telemart.Client.TransferObjects.Notification;

namespace Telemart.Client.ViewModels.Notification
{
    public interface INotificationConfigViewModel
    {
        public NotificationSubscribeConfigDto Config { get; set; }
    }
}