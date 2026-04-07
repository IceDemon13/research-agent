using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business.Order;
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
    public sealed class OrderEditDeliveryDateViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper _mapper;
        private int orderId;
        private IReadOnlyCollection<int> _orderIds;

        public OrderEditDeliveryDateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IOrderDeliveryCalculator orderDeliveryCalculator,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            OrderDeliveryCalculator = orderDeliveryCalculator;
            _mapper = mapper;
        }

        public OrderEditDeliveryDateViewModel()
        {
        }

        #region INPC

        public ObservableCollection<OrderStateChangeReasonViewItem> ChangeReasons
        {
            get { return GetProperty(() => ChangeReasons); }
            private set { SetProperty(() => ChangeReasons, value); }
        }

        public OrderStateChangeReasonViewItem CurrentChangeReason
        {
            get { return GetProperty(() => CurrentChangeReason); }
            set { SetProperty(() => CurrentChangeReason, value); }
        }

        public DateTime? CalculatedDeliveryDateFrom
        {
            get { return GetProperty(() => CalculatedDeliveryDateFrom); }
            set { SetProperty(() => CalculatedDeliveryDateFrom, value); }
        }

        public DateTime? CalculatedDeliveryDateTo
        {
            get { return GetProperty(() => CalculatedDeliveryDateTo); }
            set { SetProperty(() => CalculatedDeliveryDateTo, value); }
        }

        public DateTime? CurrentDeliveryDateFrom
        {
            get { return GetProperty(() => CurrentDeliveryDateFrom); }
            set { SetProperty(() => CurrentDeliveryDateFrom, value); }
        }

        public DateTime? CurrentDeliveryDateTo
        {
            get { return GetProperty(() => CurrentDeliveryDateTo); }
            set { SetProperty(() => CurrentDeliveryDateTo, value); }
        }

        public DateTime? SelectedDeliveryDateFrom
        {
            get { return GetProperty(() => SelectedDeliveryDateFrom); }
            set { SetProperty(() => SelectedDeliveryDateFrom, value, () => RaisePropertyChanged(nameof(SelectedDeliveryDateTo))); }
        }

        public DateTime? SelectedDeliveryDateTo
        {
            get { return GetProperty(() => SelectedDeliveryDateTo); }
            set { SetProperty(() => SelectedDeliveryDateTo, value); }
        }

        public string NullText
        {
            get { return GetProperty(() => NullText); }
            set { SetProperty(() => NullText, value); }
        }

        public bool CalculatedDeliveryVisibility
        {
            get { return GetProperty(() => CalculatedDeliveryVisibility); }
            set { SetProperty(() => CalculatedDeliveryVisibility, value); }
        }

        #endregion

        private IOrderDeliveryCalculator OrderDeliveryCalculator { get; }

        public static void BuildMetadata(MetadataBuilder<OrderEditDeliveryDateViewModel> builder)
        {
            builder.Property(x => x.SelectedDeliveryDateFrom).MatchesRule(
                x => x.HasValue,
                () => Resources.RequiredErrorMessage)
                .MatchesRule(x => x >= DateTime.Today, () => "Дата должна быть больше текущей");

            builder.Property(x => x.SelectedDeliveryDateTo)
                .MatchesInstanceRule(
                    (x, y) => x.HasValue && x > y.SelectedDeliveryDateFrom,
                    () => "Значение До должно быть больше значения От");

        }

        protected override async Task HandleLoadedAsync()
        {
            List<OrderStateChangeReasonDto> reasons = await WebClient.ExecuteApiRequestAsync(new QueryOrderStateChangeReasons(false));

            ChangeReasons = reasons
                .Where(x => x.StateId == null || x.StateId == OrderStatus.Confirmed.Id || x.StateId == OrderStatus.Received.Id)
                .Select(x => new OrderStateChangeReasonViewItem(x.Id, x.ParentId ?? 0, x.Name, x.Position))
                .ToObservableCollection();

            int[] orderIds = (int[])Parameter;

            if (orderIds.Any() != true)
            {
                return;
            }

            if (orderIds.Length == 1)
            {
                orderId = orderIds.First();

                await OneOrderAsync();
            }
            else
            {
                _orderIds = orderIds;

                await ManyOrdersAsync();
            }

            Title = "Редактор даты доставки";
        }

        protected override async Task HandleOkAsync()
        {
            if (CurrentChangeReason == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите причину переноса");
                return;
            }

            if (ChangeReasons.Any(x => x.ParentId == CurrentChangeReason.Id))
            {
                MessageFacadeService.ShowNotificationWarning("Выберите причину, а не группу");
                return;
            }

            bool isBothDatesFilled = SelectedDeliveryDateTo.HasValue && SelectedDeliveryDateFrom.HasValue;
            bool isBothDatesEmpty = !SelectedDeliveryDateTo.HasValue && !SelectedDeliveryDateFrom.HasValue;

            bool canEdit = isBothDatesFilled || isBothDatesEmpty;

            if (!canEdit)
            {
                MessageFacadeService.ShowNotificationWarning("Сохранить можно только обе даты");
                return;
            }

            try
            {
                bool result = false;

                if (_orderIds?.Any() == true)
                {
                    result = await ManyOrdersUpdateDeliveryDateAsync();
                }
                else
                {
                    result = await OneOrderUpdateDeliveryDateAsync();
                }

                IsOk = result;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки при изменении Даты Х", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
        }

        private async Task OneOrderAsync()
        {
            OrderDto orderDto = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId));

            CurrentDeliveryDateFrom = orderDto.DeliveryTime;
            CurrentDeliveryDateTo = orderDto.DeliveryTimeTo;

            IReadOnlyCollection<OrderDeliveryTime> orders = await GetOrderDeliveryTimeAsync(new[] { orderDto });

            OrderDeliveryTime result = orders.First();

            CalculatedDeliveryDateFrom = result.DeliveryTimeFrom;
            CalculatedDeliveryDateTo = result.DeliveryTimeTo;

            SelectedDeliveryDateFrom = result.DeliveryTimeFrom;
            SelectedDeliveryDateTo = result.DeliveryTimeTo;

            NullText = "не заполнено";

            CalculatedDeliveryVisibility = true;
        }

        private async Task ManyOrdersAsync()
        {
            OrderFilteringItem orderFilteringItem = new OrderFilteringItem(string.Empty, new List<int>())
            {
                OrderNumbers = string.Join(", ", _orderIds)
            };

            List<OrderDto> listOrders = await WebClient.ExecuteApiRequestAsync(new QueryOrders(orderFilteringItem)).GetPagedResultDataAsync();

            DateTime? currentFrom = listOrders.First().DeliveryTime;
            DateTime? currentTo = listOrders.First().DeliveryTimeTo;

            IReadOnlyCollection<OrderDeliveryTime> orderDeliveryTimes = await GetOrderDeliveryTimeAsync(listOrders);

            if (listOrders.All(x => x.DeliveryTime == currentFrom && x.DeliveryTimeTo == currentTo))
            {
                NullText = "не заполнено";
                CurrentDeliveryDateFrom = currentFrom;
                CurrentDeliveryDateTo = currentTo;
            }
            else
            {
                NullText = "разная дата";
            }

            CalculatedDeliveryVisibility = false;

            SelectedDeliveryDateFrom = orderDeliveryTimes.MaxBy(x => x.DeliveryTimeFrom).DeliveryTimeFrom;
            SelectedDeliveryDateTo = orderDeliveryTimes.MaxBy(x => x.DeliveryTimeTo).DeliveryTimeTo;
        }

        private async Task<IReadOnlyCollection<OrderDeliveryTime>> GetOrderDeliveryTimeAsync(IReadOnlyCollection<OrderDto> orders)
        {
            List<Task<OrderDeliveryTime>> tasks = new List<Task<OrderDeliveryTime>>();

            List<OrderDeliveryTime> orderDeliveryTimes = new List<OrderDeliveryTime>();

            foreach (OrderDto orderDto in orders)
            {
                OrderViewItem order = _mapper.Map<OrderViewItem>(orderDto);

                tasks.Add(OrderDeliveryCalculator.CalculateOrderDeliveryTimeAsync(
                    orderDto.Id,
                    orderDto.WarehouseId,
                    orderDto.AssemblyWarehouseId,
                    orderDto.BufferWarehouseId,
                    orderDto.AdditionalServiceWarehouseId,
                    orderDto.CarryId,
                    orderDto.SubdivisionId,
                    order!.State,
                    order.Products.ToList(),
                    orderDto.Folders));
            }

            await Task.WhenAll(tasks);

            tasks.ForEach(x => orderDeliveryTimes.Add(x.Result));

            return orderDeliveryTimes;
        }

        private async Task<bool> ManyOrdersUpdateDeliveryDateAsync()
        {
            UpdateOrdersDeliveryDateDto ordersDeliveryDateDto = new UpdateOrdersDeliveryDateDto()
            {
                OrderIds = _orderIds.ToArray(),
                OrderStateChangeReasonId = CurrentChangeReason.Id,
                DeliveryDateFrom = SelectedDeliveryDateFrom,
                DeliveryDateTo = SelectedDeliveryDateTo
            };

            Result<OrderDto[]> result = await WebClient.ExecuteApiRequestAsync(new UpdateOrdersDeliveryDate(ordersDeliveryDateDto));

            if (result.Warnings.Any())
            {
                ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
            }

            MessageFacadeService.ShowNotificationInfo($"Новая Дата X сохранена");

            return result.IsSuccess;
        }

        private async Task<bool> OneOrderUpdateDeliveryDateAsync()
        {
            UpdateDeliveryDate gatewayRequest = new UpdateDeliveryDate(orderId, SelectedDeliveryDateFrom, SelectedDeliveryDateTo, CurrentChangeReason.Id);

            Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

            if (result.Warnings.Any())
            {
                ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
            }

            MessageFacadeService.ShowNotificationInfo($"Новая Дата X для заказа №{result.Data.Id} сохранена");

            return result.IsSuccess;
        }
    }
}