using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business
{
    public class LockableOperationProcessor<TDto>
        where TDto : class, new()
    {
        private readonly Type entityMessageType;
        private readonly Type lockRequestType;
        private readonly Type unlockRequestType;

        public LockableOperationProcessor(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            ILogger<LockableOperationProcessor<TDto>> logger)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Logger = logger;

            Assembly messagesAssembly = typeof(OrderMessage).Assembly;
            Assembly dataAssembly = typeof(LockRequestBase<>).Assembly;
            Type dtoType = typeof(TDto);

            entityMessageType = messagesAssembly.GetTypesNestedFromGenericType(typeof(EntityMessage<>), dtoType).Single();
            lockRequestType = dataAssembly.GetTypesNestedFromGenericType(typeof(LockRequestBase<>), dtoType).Single();
            unlockRequestType = dataAssembly.GetTypesNestedFromGenericType(typeof(UnlockRequestBase<>), dtoType).Single();
        }

        protected IMessenger Messenger { get; }

        protected IWebClient WebClient { get; }

        protected IMessageFacadeService MessageFacadeService { get; }

        protected ILogger Logger { get; }

        public Task DoActionAsync(
            int entityId,
            Action<TDto> action,
            bool allowLockedByMe = true,
            bool checkPermissions = false)
        {
            return DoActionAsync(entityId, action, null, allowLockedByMe, checkPermissions);
        }

        public async Task DoActionAsync(
            int entityId,
            Action<TDto> action,
            Func<Task<bool>> check,
            bool allowLockedByMe = true,
            bool checkPermissions = false)
        {
            LockResponse<TDto> response = await TryLockAsync(entityId, allowLockedByMe, checkPermissions);

            if (response == null || !response.Success)
            {
                return;
            }

            bool ok = true;

            if (check != null)
            {
                ok = await check();
            }

            if (ok)
            {
                try
                {
                    action(response.Dto);
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to execute lockable operation");
                    MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                }
            }

            await UnlockAsync(entityId);
        }

        public async Task DoOperationAsync(int entityId, Func<TDto, Task> action, bool allowLockedByMe = true, bool checkPermissions = false)
        {
            LockResponse<TDto> response = await TryLockAsync(entityId, allowLockedByMe, checkPermissions);

            if (response?.Success == true)
            {
                try
                {
                    await action(response.Dto);
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to execute lockable operation");
                    MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                }

                await UnlockAsync(entityId);
            }
        }

        public async Task<LockResponse<TDto>> TryLockAsync(int entityId, bool allowLockedByMe, bool checkPermissions)
        {
            LockResponse<TDto> response = null;

            try
            {
                response = await WebClient.ExecuteApiRequestAsync(CreateLockRequest(entityId, allowLockedByMe, checkPermissions));

                SendMessage(CreateMessage(response.Dto, MessageType.Changed));

                if (!response.Success)
                {
                    MessageFacadeService.ShowNotificationWarning("Документ заблокирован");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to lock entity");
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
            }

            return response;
        }

        public async Task<LockResponse<TDto>> UnlockAsync(int entityId, bool force = false)
        {
            LockResponse<TDto> response = null;

            try
            {
                response = await WebClient.ExecuteApiRequestAsync(CreateUnlockRequest(entityId, force));

                SendMessage(CreateMessage(response.Dto, MessageType.Changed));

                if (!response.Success)
                {
                    MessageFacadeService.ShowNotificationError("Не удалось разблокировать документ");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to unlock entity");
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
            }

            return response;
        }

        private void SendMessage(object message)
        {
            MethodInfo methodInfo = typeof(IMessenger).GetMethod("Send", BindingFlags.Public | BindingFlags.Instance)
                .GetGenericMethodDefinition()
                .MakeGenericMethod(entityMessageType);

            methodInfo.Invoke(Messenger, new[] { message, null, null });
        }

        private object CreateMessage(TDto dto, MessageType messageType)
        {
            return entityMessageType.CreateObject(
                new[] { typeof(TDto), typeof(MessageType) },
                new object[] { dto, messageType });
        }

        private LockRequestBase<TDto> CreateLockRequest(int entityId, bool allowLockedByMe, bool checkPermissions)
        {
            return (LockRequestBase<TDto>)lockRequestType.CreateObject(
                new[] { typeof(int), typeof(bool), typeof(bool) },
                new object[] { entityId, allowLockedByMe, checkPermissions });
        }

        private UnlockRequestBase<TDto> CreateUnlockRequest(int entityId, bool force)
        {
            return (UnlockRequestBase<TDto>)unlockRequestType.CreateObject(
                new[] { typeof(int), typeof(bool) },
                new object[] { entityId, force });
        }
    }
}