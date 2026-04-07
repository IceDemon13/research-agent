using System;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderPaymentControlViewModel : TelemartDialogViewModelBase
    {
        private OrderPaymentControlParameter parameter;
        private decimal amount;

        public OrderPaymentControlViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            CancelPaymentCommand = new DelegateCommand(CancelPayment);
        }

        public IDelegateCommand CancelPaymentCommand { get; }

        public string ToPayAmountStr
        {
            get { return GetProperty(() => ToPayAmountStr); }
            private set { SetProperty(() => ToPayAmountStr, value); }
        }

        public string HoldedAmountStr
        {
            get { return GetProperty(() => HoldedAmountStr); }
            private set { SetProperty(() => HoldedAmountStr, value); }
        }

        public string AmountStr
        {
            get { return GetProperty(() => AmountStr); }
            private set { SetProperty(() => AmountStr, value); }
        }

        private IMessenger Messenger { get; }

        protected override Task HandleLoadedAsync()
        {
            parameter = (OrderPaymentControlParameter)Parameter;

            ToPayAmountStr = CurrencyFormatingRules.ToUahStr(parameter.ToPayAmount);
            HoldedAmountStr = CurrencyFormatingRules.ToUahStr(parameter.HoldedAmount + parameter.ExtraAmount);

            amount = (parameter.ExternalPayment.Payment.Credit ? parameter.HoldedAmount : Math.Min(parameter.ToPayAmount, parameter.HoldedAmount)) + parameter.ExtraAmount;

            AmountStr = CurrencyFormatingRules.ToUahStr(amount);

            Title = $"Контроль оплаты ({parameter.ExternalPayment.Payment.Name})";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            DelayedConfirmParameter delayedConfirmParameter;

            if (parameter.ToPayAmount < amount)
            {
                delayedConfirmParameter = new DelayedConfirmParameter(
                    "Сумма списания больше чем необходимо для заказа. Продолжить?",
                    5);
            }
            else
            {
                delayedConfirmParameter = new DelayedConfirmParameter(
                    $"Вы собираетесь списать с карты клиента {AmountStr}?",
                    2);
            }

            DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>(delayedConfirmParameter, this);

            if (viewModel.IsOk)
            {
                try
                {
                    Result<OrderPaymentResultDto> result = await WebClient.ExecuteApiRequestAsync(new ConfirmOrderPayment(parameter.OrderId, amount - parameter.ExtraAmount, parameter.ExternalPayment.Payment.Id));

                    if (result.Data?.OrderPayment is not null)
                    {
                        Messenger.Send(new OrderPaymentMessage(result.Data.OrderPayment, MessageType.Added));
                    }

                    if (result.Warnings.Any())
                    {
                        MessageFacadeService.ShowNotificationWarning("Оплата подтверждена с предупреждениями");
                        ShowValidationResultView("Предупреждения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationInfo("Оплата успешно подтверждена");
                    }

                    IsOk = true;
                    Close();
                }
                catch (UnexpectedSatusException exception)
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при подтверждении оплаты");
                    ShowValidationResultView("Ошибки при подтверждении оплаты", exception.GetErrorItems());
                }
                catch (UnexpectedErrorException exception)
                {
                    Logger.LogError(exception, "Failed to confirm payment");
                    ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при подтверждении оплаты");
                    Logger.LogError(exception, "Error while confirm payment");
                }
            }
        }

        protected void CancelPayment()
        {
            DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>($"Вы собираетесь отменить платеж клиента на сумму {AmountStr}?", this);

            if (viewModel.IsOk)
            {
                OrderEditPaymentControlViewModel editPaymentViewModel = DialogDocumentManagerService
                    .ShowView<OrderEditPaymentControlViewModel>(
                        new OrderEditPaymentControlParameter(
                        parameter.OrderId,
                        parameter.SubdivisionId,
                        parameter.ExternalPayment.Payment.Id,
                        amount,
                        parameter.ExternalPayment.Payment.Id == Payment.NovaPayId ? Payment.CashId : null),
                        this);

                if (editPaymentViewModel.IsOk)
                {
                    IsOk = true;
                    Close();
                }
            }
        }
    }
}