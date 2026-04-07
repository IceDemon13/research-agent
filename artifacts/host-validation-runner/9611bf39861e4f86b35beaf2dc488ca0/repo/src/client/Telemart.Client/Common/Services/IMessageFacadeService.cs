using System.Collections.Generic;
using System.Windows;
using DevExpress.Mvvm;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.Common.Services
{
    public interface IMessageFacadeService
    {
        bool Confirm(string text, string caption = "Подтверждение");

        MessageBoxResult ShowMessageBox(
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon,
            MessageBoxResult defaultResult = MessageBoxResult.None,
            FrameworkElement owner = null);

        MessageBoxResult ShowCustomMessageBox(
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon,
            bool suspendKeyboard = false,
            Window owner = null);

        void ShowNewCommentNotification(string caption, string content, int commentId, ISupportServices parent);

        void ShowEntityNotification<TMessage>(
            int? notificationId,
            string caption,
            string content,
            TMessage viewModelParameter,
            MessageBoxImage icon)
            where TMessage : class;

        void ShowMessageBoxError(string text, string caption = "", FrameworkElement owner = null);

        void ShowMessageBoxInfo(string text, string caption = "", FrameworkElement owner = null);

        void ShowMessageBoxWarning(string text, string caption = "", FrameworkElement owner = null);

        void ShowNotificationError(string text, bool playSound = false);

        void ShowNotificationInfo(string text);

        void ShowUpdateNotificationInfo(string text);

        void ShowNotificationWarning(string text, bool playSound = false);

        void ShowValidationResultView(string title, IEnumerable<ValidationResultItem> validationItems, ISupportServices parent);

        void ShowNotification(string text, MessageBoxImage icon);
    }
}