using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
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
    public sealed class OrderCancelViewModel : TelemartDialogViewModelBase
    {
        private OrderCancelInfoDto _cancelInfo;

        public OrderCancelViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public OrderCancelViewModel()
        {
        }

        #region INPC

        public ObservableCollection<OrderStateChangeReasonViewItem> ChangeReasons
        {
            get { return GetProperty(() => ChangeReasons); }
            private set { SetProperty(() => ChangeReasons, value); }
        }

        public OrderStatus NewOrderState
        {
            get { return GetProperty(() => NewOrderState); }
            set { SetProperty(() => NewOrderState, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public OrderStateChangeReasonViewItem CurrentChangeReason
        {
            get { return GetProperty(() => CurrentChangeReason); }
            set { SetProperty(() => CurrentChangeReason, value); }
        }

        #endregion

        private IMessenger Messenger { get; }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            ChangeReasons = Enumerable
                .Range(1, 10)
                .Select(x => new OrderStateChangeReasonViewItem(x, x == 2 ? 1 : 0, $"Reason{x}", 0))
                .ToObservableCollection();
        }

        protected override Task HandleLoadedAsync()
        {
            OrderCancelInfoDto p = (OrderCancelInfoDto)Parameter;

            NewOrderState = Dictionaries.GetItemById<OrderStatus>(p.NewStateId);
            _cancelInfo = p;

            ChangeReasons = p.Reasons
                .Select(x => new OrderStateChangeReasonViewItem(x.Id, x.ParentId ?? 0, x.Name, x.Position))
                .ToObservableCollection();

            Title = "Отмена заказа";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            if (CurrentChangeReason == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите причину");
                return;
            }

            if (ChangeReasons.Any(x => x.ParentId == CurrentChangeReason.Id))
            {
                MessageFacadeService.ShowNotificationWarning("Выберите причину, а не группу");
                return;
            }

            const string ErrorMessage = "Ошибка при отмене заказа";

            try
            {
                CancelOrder gatewayRequest = new CancelOrder(_cancelInfo.Order.Id, CurrentChangeReason.Id, Comment, _cancelInfo.CellIds);

                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                MessageFacadeService.ShowNotificationInfo($"Заказ №{result.Data.Id} успешно отменен");
                Messenger.Send(new OrderMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(ErrorMessage);
                ShowValidationResultView("Ошибки при отмене заказа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError(ErrorMessage);
                ShowValidationResultView("Ошибки при отмене заказа", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to cancel order");
                MessageFacadeService.ShowNotificationError(ErrorMessage);
            }
        }
    }
}