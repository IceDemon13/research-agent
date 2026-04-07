using System;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderEditPaymentControlViewModel : OrderEditPaymentViewModelBase
    {
        public OrderEditPaymentControlViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService, IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        private IMessenger Messenger { get; }

        protected override async Task HandleOkAsync()
        {
            OrderEditPaymentControlParameter parameter = (OrderEditPaymentControlParameter)Parameter;

            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(new ReturnOrderPayment(parameter.OrderId, parameter.PaymentId, SelectedPaymentId, parameter.OrderTotalAmount));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Оплата отменена с предупреждениями");
                    ShowValidationResultView("Предупреждения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Оплата успешно отменена");
                }

                if (result.Data is not null)
                {
                    Messenger.Send(new OrderMessage(result.Data, MessageType.Changed));
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при отмене оплаты");
                ShowValidationResultView("Ошибки при отмене оплаты", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to return payment");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при отмене оплаты");
                Logger.LogError(exception, "Error while returning payment");
            }
        }
    }
}