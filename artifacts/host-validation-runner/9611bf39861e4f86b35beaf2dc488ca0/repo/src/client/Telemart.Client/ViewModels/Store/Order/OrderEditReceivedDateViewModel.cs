using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
    public sealed class OrderEditReceivedDateViewModel : TelemartDialogViewModelBase
    {
        private OrderDto _order;

        public OrderEditReceivedDateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public OrderEditReceivedDateViewModel()
        {
        }

        public DateTime? SelectedDateTime
        {
            get { return GetProperty(() => SelectedDateTime); }
            set { SetProperty(() => SelectedDateTime, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            _order = (OrderDto)Parameter;

            SelectedDateTime = _order.CustomerReceivedOn;

            Title = "Редактировать дату получения";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                UpdateCustomerReceivedOn request = new UpdateCustomerReceivedOn(_order.Id, SelectedDateTime);

                Result result = await WebClient.ExecuteApiRequestAsync(request);

                IsOk = true;
                Close();

                MessageFacadeService.ShowNotificationInfo($"Заказ №{_order.Id} успешно обработан");
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки при обработке заказа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при обработке заказа", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while making order as taken");
                MessageFacadeService.ShowNotificationError("Ошибка при обработке заказа");
            }
        }
    }
}