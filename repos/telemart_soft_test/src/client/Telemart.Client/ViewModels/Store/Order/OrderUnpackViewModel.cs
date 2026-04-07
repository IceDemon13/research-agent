using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderUnpackViewModel : TelemartDialogViewModelBase
    {
        private readonly IMessenger _messenger;
        private readonly IErrorHandler _errorHandler;
        private OrderUnpackParameter _parameter;

        public OrderUnpackViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _messenger = messenger;
            _errorHandler = errorHandler;
        }

        public OrderUnpackViewModel()
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

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            ChangeReasons = Enumerable
                .Range(1, 10)
                .Select(x => new OrderStateChangeReasonViewItem(x, x == 2 ? 1 : 0, $"Reason{x}", 0))
                .ToObservableCollection();
        }

        protected override async Task HandleLoadedAsync()
        {
            _parameter = (OrderUnpackParameter)Parameter;

            List<OrderStateChangeReasonDto> reasons = await WebClient.ExecuteApiRequestAsync(new QueryOrderStateChangeReasons(false));

            ChangeReasons = reasons
                .Where(x => x.StateId == null || x.StateId == OrderStatus.Confirmed.Id || x.StateId == OrderStatus.Received.Id)
                .Select(x => new OrderStateChangeReasonViewItem(x.Id, x.ParentId ?? 0, x.Name, x.Position))
                .ToObservableCollection();

            Title = "Распаковка заказа";
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

            Result<OrderDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new UnpackOrder(_parameter.OrderId, CurrentChangeReason.Id, Comment, _parameter.OrderCellIds)),
                "распаковке заказа",
                $"Заказ №{_parameter.OrderId} распакован",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                _messenger.Send(new OrderMessage(result.Data, MessageType.Changed));

                IsOk = true;
            }

            Close();
        }
    }
}