using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Validation;
using Telemart.Client.WebClient;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Base
{
    public sealed class TelemartCommentEditorViewModel<TDto> : TelemartDialogViewModelBase
        where TDto : class
    {
        private int entityId;
        private Type requestType;
        private Type entityMessageType;

        public TelemartCommentEditorViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public TelemartCommentEditorViewModel()
        {
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public string CommentOriginal
        {
            get { return GetProperty(() => CommentOriginal); }
            private set { SetProperty(() => CommentOriginal, value); }
        }

        public bool IsChanged => !string.Equals(Comment, CommentOriginal, StringComparison.Ordinal);

        public TDto UpdatedEntity { get; private set; }

        private IMessenger Messenger { get; }

        protected override Task HandleLoadedAsync()
        {
            CommentEditorParameter parameter = (CommentEditorParameter)Parameter;

            entityId = parameter.Id;
            CommentOriginal = parameter.Comment;
            Comment = parameter.Comment;

            Assembly messagesAssembly = typeof(OrderMessage).Assembly;
            Assembly dataAssembly = typeof(UpdateCommentBase<>).Assembly;
            Type dtoType = typeof(TDto);

            entityMessageType = messagesAssembly.GetTypesNestedFromGenericType(typeof(EntityMessage<>), dtoType).Single();
            requestType = dataAssembly.GetTypesNestedFromGenericType(typeof(UpdateCommentBase<>), dtoType).Single();

            Title = "Комментарий";

            return base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                if (IsChanged)
                {
                    Result<TDto> result = await WebClient.ExecuteApiRequestAsync(CreateRequest(entityId, Comment));

                    SendMessage(CreateMessage(result.Data, MessageType.Changed));

                    UpdatedEntity = result.Data;

                    if (result.Warnings.Any())
                    {
                        MessageFacadeService.ShowNotificationWarning("Комментарий сохранен с предупреждениями");
                        ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationInfo("Комментарий сохранен успешно");
                    }
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                ShowValidationResultView("Ошибки при выполнении операции", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save entity");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving entity");
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
            }
        }

        private void SendMessage(object message)
        {
            Type type = message.GetType();

            MethodInfo methodInfo = typeof(IMessenger).GetMethod("Send", BindingFlags.Public | BindingFlags.Instance)
                .GetGenericMethodDefinition()
                .MakeGenericMethod(type);

            methodInfo.Invoke(Messenger, new[] { message, null, null });
        }

        private object CreateMessage(TDto dto, MessageType messageType)
        {
            return entityMessageType.CreateObject(
                new[] { typeof(TDto), typeof(MessageType) },
                new object[] { dto, messageType });
        }

        private UpdateCommentBase<TDto> CreateRequest(int id, string comment)
        {
            return (UpdateCommentBase<TDto>)requestType.CreateObject(
                new[] { typeof(int), typeof(string) },
                new object[] { id, comment });
        }
    }
}