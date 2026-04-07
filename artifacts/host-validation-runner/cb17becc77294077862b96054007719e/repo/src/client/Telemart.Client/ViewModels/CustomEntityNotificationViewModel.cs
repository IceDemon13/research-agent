using System.Windows.Media;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels
{
    [POCOViewModel]
    public class CustomEntityNotificationViewModel<TMessage>
    where TMessage : class
    {
        private IMessenger _messenger;
        private INotification _notification;
        private TMessage _message;

        protected CustomEntityNotificationViewModel()
        {
            IsOpenButtonVisible = true;
            OpenCommand = new DelegateCommand(Open, IsOpenButtonVisible);
        }

        public IDelegateCommand OpenCommand { get; }

        public bool IsOpenButtonVisible { get; private set; }

        public virtual string Caption { get; protected set; }

        public virtual string Content { get; protected set; }

        public virtual ImageSource Image { get; set; }

        public static CustomEntityNotificationViewModel<TMessage> Create(
            string caption,
            string content,
            TMessage message,
            ImageSource image,
            IMessenger messenger)
        {
            CustomEntityNotificationViewModel<TMessage> viewModel = ViewModelSource<CustomEntityNotificationViewModel<TMessage>>.Create();

            viewModel.Image = image;
            viewModel.Caption = caption;
            viewModel.Content = content;
            viewModel._messenger = messenger;
            viewModel._message = message;

            if (message is EditorParameter parameter && parameter.Id <= 0)
            {
                viewModel.IsOpenButtonVisible = false;
            }

            return viewModel;
        }

        public void SetNotification(INotification notificationToSet)
        {
            _notification = notificationToSet;
        }

        private void Open()
        {
            _messenger.Send(_message);

            _notification.Hide();
        }
    }
}