using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Comment;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Comment;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Comment
{
    public sealed class CommentAnswersViewModel : TelemartDialogViewModelBase
    {
        private CommentSimpleDto _comment;

        public CommentAnswersViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            RefreshCommand = new AsyncCommand(RefreshAsync);
            ApproveCommand = new AsyncCommand<CommentViewItem>(ApproveAsync, CanApprove);
            DismissCommand = new AsyncCommand<CommentViewItem>(DismissAsync, CanDismiss);
            HandleRowDoubleClickCommand = new DelegateCommand<RowDoubleClickInfo>(HandleRowDoubleClick, x => x != null);
        }

        public CommentAnswersViewModel()
        {
        }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand ApproveCommand { get; }

        public IAsyncCommand DismissCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        #region INPC

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string Text
        {
            get { return GetProperty(() => Text); }
            set { SetProperty(() => Text, value); }
        }

        public ObservableCollection<CommentViewItem> Comments
        {
            get { return GetProperty(() => Comments); }
            set { SetProperty(() => Comments, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Entities
        {
            get { return GetProperty(() => Entities); }
            private set { SetProperty(() => Entities, value); }
        }

        public ObservableCollection<CommentViewItem> FixedTopComments
        {
            get { return GetProperty(() => FixedTopComments); }
            set { SetProperty(() => FixedTopComments, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        #endregion

        public override int Width => 1000;

        public override int Height => 600;

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private CommentAnswersParameter CommentParameter { get; set; }

        public static void BuildMetadata(MetadataBuilder<CommentAnswersViewModel> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(100, () => "Значение поля должно быть короче 100 символов");
            builder.Property(x => x.Text)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(2000, () => "Значение поля должно быть короче 2000 символов");
        }

        protected override async Task HandleLoadedAsync()
        {
            CommentParameter = (CommentAnswersParameter)Parameter;

            if (CommentParameter.IsNew)
            {
                throw new NotSupportedException("Comment creation is not supported");
            }

            Name = "Интернет-магазин Telemart.ua";

            List<EmployeeDto> employeeList = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true)
                .GetPagedResultDataAsync();

            Employees = employeeList
                .Where(x => x.Active)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Entities = Dictionaries
                .GetItems<Entity>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories.Select(x => new ComboBoxItem(x.Id, x.Name)).ToObservableRangeCollection();

            RefreshCommand.Execute(null);

            await base.HandleLoadedAsync();

            Title = $"Ответы на комментарий №{CommentParameter.Id}";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            if (_comment.StateId == CommentState.NewId && !MessageFacadeService.Confirm("Ответ для вопроса в статусе 'Новый' не будет отображаться на сайте. Продолжть?"))
            {
                return;
            }

            try
            {
                CommentCreateAnswerDto dto = new CommentCreateAnswerDto
                {
                    Text = Text,
                    Name = Name,
                    Email = WebClient.AuthenticatedEmployee.Email
                };

                Result<CommentSimpleDto> result = await WebClient.ExecuteApiRequestAsync(new CreateCommentAnswer(CommentParameter.Id, dto));

                Comments.Add(Mapper.Map<CommentViewItem>(result.Data));

                Messenger.Send(new CommentMessage(result.Data, MessageType.Added));

                Text = string.Empty;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании ответа на коментарий");
                ShowValidationResultView("Ошибки при создании ответа на коментарий", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create comment answer");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании ответа на коментарий");
                Logger.LogError(exception, "Error while creating comment answer");
            }
        }

        private bool CanDismiss(CommentViewItem selectedComment)
        {
            return selectedComment != null && selectedComment.State.Id != CommentState.HideId;
        }

        private Task DismissAsync(CommentViewItem selectedComment)
        {
            return ChangeStateAsync(selectedComment, CommentState.HideId);
        }

        private bool CanApprove(CommentViewItem selectedComment)
        {
            return selectedComment != null && selectedComment.State.Id != CommentState.ActiveId;
        }

        private Task ApproveAsync(CommentViewItem selectedComment)
        {
            return ChangeStateAsync(selectedComment, CommentState.ActiveId);
        }

        private async Task ChangeStateAsync(CommentViewItem comment, int stateId)
        {
            try
            {
                comment.IsStateChanging = true;

                CommentChangeStateDto dto = new CommentChangeStateDto
                {
                    StateId = stateId
                };

                Result<CommentSimpleDto> result = await WebClient.ExecuteApiRequestAsync(new ChangeStateComment(comment.Id, dto));

                Messenger.Send(new CommentMessage(result.Data, MessageType.Changed));

                Comments.DoActionWithItem(x => x.Id == comment.Id, viewItem => Mapper.Map(result.Data, viewItem));

                MessageFacadeService.ShowNotificationInfo("Статус успешно изменен");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении статуса");
                ShowValidationResultView("Ошибка при изменении статуса", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to change comment state");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении статуса");
                Logger.LogError(exception, "Error while changing comment state");
            }
            finally
            {
                comment.IsStateChanging = false;
            }
        }

        private async Task RefreshAsync()
        {
            _comment = await WebClient.ExecuteApiRequestAsync(new QueryComment(CommentParameter.Id));

            CommentViewItem comment = Mapper.Map<CommentViewItem>(_comment);

            Comments = comment.Children.ToObservableCollection();

            Comments.Add(comment);

            FixedTopComments = new ObservableCollection<CommentViewItem> { comment };
        }

        private void HandleRowDoubleClick(RowDoubleClickInfo e)
        {
            switch (e.FieldName)
            {
                case nameof(CommentViewItem.Foto):

                    CommentViewItem current = (CommentViewItem)e.Data;

                    if (current.Foto)
                    {
                        CommentFotoParameter parameter = new CommentFotoParameter(current.Id);

                        NonModalDialogDocumentManagerService.ShowView<CommentFotoModel>(parameter, this);
                    }

                    break;
            }
        }
    }
}