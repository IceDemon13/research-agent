using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Validation
{
    public sealed class ValidationResultItem
    {
        public ValidationResultItem(string message, bool isError)
        {
            Message = message;
            IsError = isError;
            NotificationImageId = isError ? NotificationImage.ErrorId : NotificationImage.WarningId;
        }

        public ValidationResultItem(string message, int notificationImageId)
        {
            Message = message;
            NotificationImageId = notificationImageId;
            IsError = notificationImageId == NotificationImage.ErrorId;
        }

        public string Message { get; }

        public bool IsError { get; }

        public int NotificationImageId { get; }
    }
}