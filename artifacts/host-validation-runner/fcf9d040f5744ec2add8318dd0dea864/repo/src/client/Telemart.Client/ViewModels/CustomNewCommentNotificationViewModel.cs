using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Comment;

namespace Telemart.Client.ViewModels
{
    [POCOViewModel]
    public class CustomNewCommentNotificationViewModel
    {
        private int commentId;
        private ISupportServices parent;
        private INotification notification;

        protected CustomNewCommentNotificationViewModel()
        {
             AnswerCommand = new DelegateCommand(Answer);
        }

        public IDelegateCommand AnswerCommand { get; }

        public virtual string Caption { get; protected set; }

        public virtual string Content { get; protected set; }

        public static CustomNewCommentNotificationViewModel Create(string caption, string content, int commentId, ISupportServices parent)
        {
            CustomNewCommentNotificationViewModel viewModel = ViewModelSource<CustomNewCommentNotificationViewModel>.Create();

            viewModel.Caption = caption;
            viewModel.Content = content;
            viewModel.parent = parent;
            viewModel.commentId = commentId;

            return viewModel;
        }

        public void SetNotification(INotification notification)
        {
            this.notification = notification;
        }

        private void Answer()
        {
            IDocumentManagerService sizeableDialogDocumentManagerService = parent.ServiceContainer.GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

            notification.Hide();

            sizeableDialogDocumentManagerService.ShowView<CommentAnswersViewModel>(new CommentAnswersParameter(commentId), parent);
        }
    }
}