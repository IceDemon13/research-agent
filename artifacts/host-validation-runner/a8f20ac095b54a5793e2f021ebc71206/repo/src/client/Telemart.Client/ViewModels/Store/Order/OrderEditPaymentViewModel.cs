using System;
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
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderEditPaymentViewModel : OrderEditPaymentViewModelBase
    {
        public OrderEditPaymentViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService, IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        private IMessenger Messenger { get; }

        protected override async Task HandleOkAsync()
        {
            OrderEditPaymentParameter parameter = (OrderEditPaymentParameter)Parameter;

            if (parameter.PaymentId == SelectedPaymentId)
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            try
            {
                UpdateOrderPaymentDto updateDto = new UpdateOrderPaymentDto(parameter.OrderId, SelectedPaymentId);

                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateOrderPayment(parameter.OrderId, updateDto));

                MessageFacadeService.ShowNotificationInfo($"Данные об оплате для заказа №{result.Data.Id} успешно сохранены");

                Messenger.Send(new OrderMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки при изменении оплаты заказа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при изменении оплаты заказа", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving orders client");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
            }
        }
    }
}