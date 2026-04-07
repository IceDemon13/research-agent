using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.OrderPayment;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class EditOrderPaymentViewModel : TelemartDialogViewModelBase
    {
        private OrderPaymentRecordViewItem orderPayment;
        private IReadOnlyDictionary<int, string> employees;

        public EditOrderPaymentViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public EditOrderPaymentViewModel()
        {
        }

        #region INPC

        public DateTime? ReceivedOn
        {
            get { return GetProperty(() => ReceivedOn); }
            set { SetProperty(() => ReceivedOn, value); }
        }

        public DateTime ReceivedOnMinValue
        {
            get { return GetProperty(() => ReceivedOnMinValue); }
            private set { SetProperty(() => ReceivedOnMinValue, value); }
        }

        public DateTime ReceivedOnMaxValue
        {
            get { return GetProperty(() => ReceivedOnMaxValue); }
            private set { SetProperty(() => ReceivedOnMaxValue, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        #endregion

        private IMessenger Messenger { get; }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            SummaryItems = new[]
            {
                new SummaryViewItem("Способ", "Наличные"),
                new SummaryViewItem("Сумма", "23"),
                new SummaryViewItem("Оплачено", "100"),
                new SummaryViewItem("Валюта", "UAH"),
                new SummaryViewItem("Внесена", "19.01.2018"),
                new SummaryViewItem("Создана", "19.01.2018 01:45"),
                new SummaryViewItem("Создал", "Demo")
            };
        }

        protected override async Task HandleLoadedAsync()
        {
            object[] parameters = (object[])Parameter;

            orderPayment = (OrderPaymentRecordViewItem)parameters[0];
            DateTime orderCreatedOn = (DateTime)parameters[1];

            ReceivedOnMinValue = orderCreatedOn.Date;
            ReceivedOnMaxValue = DateTime.Today;

            ReceivedOn = orderPayment.ReceivedOn;
            Comment = orderPayment.Comment;

            PagedResult<EmployeeDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

            employees = pagedResult.Data.ToDictionary(x => x.Id, x => x.Name);

            RefreshSummaryItems();

            Title = $"Оплата №{orderPayment.Id}";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                UpdateOrderPayment gatewayRequest = new UpdateOrderPayment(
                   orderPayment.OrderId,
                   new OrderPaymentSaveDto { Id = orderPayment.Id, ReceivedOn = ReceivedOn, Comment = Comment });

                Result<OrderPaymentDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                MessageFacadeService.ShowNotificationInfo($"Оплата №{result.Data.Id} успешно сохранена");

                Messenger.Send(new OrderPaymentMessage(result.Data, MessageType.Changed));

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
                Logger.LogError(exception, "Error while updating order payment");
                MessageFacadeService.ShowNotificationError("Ошибка при изменении оплаты заказа");
            }
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems();
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            const string NotSet = "(не задано)";
            Currency currency = Currency.GetById(orderPayment.Currency.Id);

            yield return new SummaryViewItem("Способ", orderPayment.Payment?.Name ?? NotSet);
            yield return new SummaryViewItem("Сумма", CurrencyFormatingRules.ToStr(orderPayment.Amount, currency.Id, "C2"));
            yield return new SummaryViewItem("Оплачено", CurrencyFormatingRules.ToStr(orderPayment.AmountPaid, currency.Id, "C2"));
            yield return new SummaryViewItem("Валюта", currency.Title);
            yield return new SummaryViewItem("Внесена", orderPayment.ReceivedOn?.ToString(DateFormattingRules.DateFormat) ?? NotSet);
            yield return new SummaryViewItem("Создана", orderPayment.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat));
            yield return new SummaryViewItem("Создал", employees.GetValueOrDefault(orderPayment.CreatedBy));

            if (orderPayment.CompletedOnFiscalRegistrar && !string.IsNullOrWhiteSpace(orderPayment.FiscalId))
            {
                yield return new SummaryViewItem("Фиск. чек", "✓");
            }
        }
    }
}