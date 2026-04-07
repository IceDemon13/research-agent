using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Business;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order.OrderEditPrice;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class ChangeMoneyBackAmountViewModel : TelemartDialogViewModelBase
    {
        private readonly IMessenger _messenger;
        private readonly IMapper _mapper;
        private readonly IErrorHandler _errorHandler;

        public ChangeMoneyBackAmountViewModel(
            IWebClient webClient,
            IErrorHandler errorHandler,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _messenger = messenger;
            _mapper = mapper;
            _errorHandler = errorHandler;

            OpenOrderCommand = new DelegateCommand<int>(OpenOrder);
        }

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public decimal SumOrder
        {
            get { return GetProperty(() => SumOrder); }
            set { SetProperty(() => SumOrder, value); }
        }

        public decimal ToPay
        {
            get { return GetProperty(() => ToPay); }
            set { SetProperty(() => ToPay, value, () => RaisePropertyChanged(nameof(MoneyBackAmount))); }
        }

        public decimal MoneyBackAmount
        {
            get { return GetProperty(() => MoneyBackAmount); }
            set { SetProperty(() => MoneyBackAmount, value); }
        }

        public IDelegateCommand OpenOrderCommand { get; }

        public static void BuildMetadata(MetadataBuilder<ChangeMoneyBackAmountViewModel> b)
        {
            b.Property(x => x.MoneyBackAmount)
                .MatchesInstanceRule(
                    (x, y) => x >= 0 && x <= y.ToPay,
                    (_, y) => $"Наложенный платеж должен быть в пределах 0...{y.ToPay}");
        }

        protected override async Task HandleLoadedAsync()
        {
            OrderId = (int)Parameter;

            OrderDto orderDto = await WebClient.ExecuteApiRequestAsync(new QueryOrder(OrderId));

            MoneyBackAmount = orderDto.MoneyBackAmount ?? 0;

            (decimal SummaryAmont, decimal ToPayAmount) orderInfo = GetOrderPaymentInfo(orderDto);

            SumOrder = orderInfo.SummaryAmont;
            ToPay = orderInfo.ToPayAmount;

            Title = "Изменение суммы наложенного платежа";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new ChangeMoneyBackAmount(OrderId, MoneyBackAmount)),
                "при изменении наложенного платежа",
                "Наложенный платеж изменен",
                this,
                true,
                showDialog: false,
                onSuccess: (_, _) =>
                {
                   CloseOk();

                   return Task.CompletedTask;
                });
        }

        private void OpenOrder(int orderId)
        {
            _messenger.Send(new OrderEditViewMessage(orderId));
        }

        private (decimal SummaryAmont, decimal ToPayAmount) GetOrderPaymentInfo(OrderDto orderDto)
        {
            OrderPaymentInfoViewModel orderPaymentViewModel = new OrderPaymentInfoViewModel();

            OrderEditPriceViewItem order = _mapper.Map<OrderEditPriceViewItem>(orderDto);

            orderPaymentViewModel.CalcPaymentInfo(order);

            Prices sumOrder = orderPaymentViewModel.SummaryItem.Value + orderPaymentViewModel.DeliveryItem.Value;

            return (sumOrder.Uah, orderPaymentViewModel.ToPayItem.Value.Uah);
        }
    }
}