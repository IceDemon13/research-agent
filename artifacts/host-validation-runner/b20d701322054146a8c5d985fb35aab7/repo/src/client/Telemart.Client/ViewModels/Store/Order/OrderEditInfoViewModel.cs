using System;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderEditInfoViewModel : TelemartDialogViewModelBase
    {
        private int orderId;

        public OrderEditInfoViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public OrderEditInfoViewModel()
        {
        }

        #region INPC

        public DateTime? ReceiveTime
        {
            get { return GetProperty(() => ReceiveTime); }
            set { SetProperty(() => ReceiveTime, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public DateTime ReceiveTimeMinValue
        {
            get { return GetProperty(() => ReceiveTimeMinValue); }
            private set { SetProperty(() => ReceiveTimeMinValue, value); }
        }

        #endregion

        private IMessenger Messenger { get; }

        protected override Task HandleLoadedAsync()
        {
            return Task.Factory.StartNew(
                () =>
                {
                    OrderDto order = (OrderDto)Parameter;

                    orderId = order.Id;

                    ReceiveTime = order.ReceiveTime;
                    Comment = order.EmployeeComment;

                    ReceiveTimeMinValue = order.CreatedOn;

                    Title = $"Изменение заказа №{order.Id}";
                },
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateOrderInfo(orderId, ReceiveTime, Comment));

                MessageFacadeService.ShowNotificationInfo($"Инфо для заказа №{result.Data.Id} успешно сохранено");
                Messenger.Send(new OrderMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving orders инфо");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
            }
        }
    }
}