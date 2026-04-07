using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Logistics;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.LogisticsAnalitics;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Store.Invoice;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Validation;
using Telemart.Client.ViewModels.Warehouse.Movement;

namespace Telemart.Client.ViewModels.LogisticsAnalitics
{
    public sealed class LogisticsAnaliticsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private readonly IMessenger _messenger;
        private readonly IMapper _mapper;
        private readonly IErrorHandler _errorHandler;

        public LogisticsAnaliticsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;
            _messenger = messenger;
            _errorHandler = errorHandler;

            Filter = new LogisticsAnaliticsFilterViewModel(webClient);

            RefreshCommand = new AsyncCommand(RefreshAsync, () => !InvoiceMoveCommand.IsExecuting && !MovementMoveCommand.IsExecuting && !OrderMoveCommand.IsExecuting);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            InvoiceMoveCommand = new AsyncCommand<InvoiceLogisticsAnaliticsViewItem>(InvoiceMoveAsync, x => x != null);
            MovementMoveCommand = new AsyncCommand<MovementLogisticsAnaliticsViewItem>(MovementMoveAsync, x => x != null);
            OrderMoveCommand = new AsyncCommand<ObservableCollection<OrderLogisticsAnaliticsViewItem>>(OrderMoveAsync, _ => SelectedOrderLogisticsAnaliticsViewItems?.Any() == true);
            InvoiceHandleRowDoubleClickCommand = new DelegateCommand(InvoiceHandleRowDoubleClick);
            MovementHandleRowDoubleClickCommand = new DelegateCommand(MovementHandleRowDoubleClick);
            OrderHandleRowDoubleClickCommand = new DelegateCommand(OrderHandleRowDoubleClick);
            OpenCloseInvoiceGridCommand = new DelegateCommand<bool>(OpenCloseInvoiceGrid);
            OpenCloseMovementGridCommand = new DelegateCommand<bool>(OpenCloseMovementGrid);
            OpenCloseOrderGridCommand = new DelegateCommand<bool>(OpenCloseOrderGrid);

            OrderLogisticsAnaliticsViewItems = new ObservableCollection<OrderLogisticsAnaliticsViewItem>();
            InvoiceLogisticsAnaliticsViewItems = new ObservableCollection<InvoiceLogisticsAnaliticsViewItem>();
            MovementLogisticsAnaliticsViewItems = new ObservableCollection<MovementLogisticsAnaliticsViewItem>();
            SelectedOrderLogisticsAnaliticsViewItems = new ObservableCollection<OrderLogisticsAnaliticsViewItem>();
        }

        public LogisticsAnaliticsFilterViewModel Filter { get; }

        #region INPC

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ObservableCollection<InvoiceLogisticsAnaliticsViewItem> InvoiceLogisticsAnaliticsViewItems
        {
            get { return GetProperty(() => InvoiceLogisticsAnaliticsViewItems); }
            set { SetProperty(() => InvoiceLogisticsAnaliticsViewItems, value); }
        }

        public ObservableCollection<MovementLogisticsAnaliticsViewItem> MovementLogisticsAnaliticsViewItems
        {
            get { return GetProperty(() => MovementLogisticsAnaliticsViewItems); }
            set { SetProperty(() => MovementLogisticsAnaliticsViewItems, value); }
        }

        public ObservableCollection<OrderLogisticsAnaliticsViewItem> OrderLogisticsAnaliticsViewItems
        {
            get { return GetProperty(() => OrderLogisticsAnaliticsViewItems); }
            set { SetProperty(() => OrderLogisticsAnaliticsViewItems, value); }
        }

        public InvoiceLogisticsAnaliticsViewItem SelectedInvoiceLogisticsAnaliticsViewItem
        {
            get { return GetProperty(() => SelectedInvoiceLogisticsAnaliticsViewItem); }
            set { SetProperty(() => SelectedInvoiceLogisticsAnaliticsViewItem, value); }
        }

        public MovementLogisticsAnaliticsViewItem SelectedMovementLogisticsAnaliticsViewItem
        {
            get { return GetProperty(() => SelectedMovementLogisticsAnaliticsViewItem); }
            set { SetProperty(() => SelectedMovementLogisticsAnaliticsViewItem, value); }
        }

        public ObservableCollection<OrderLogisticsAnaliticsViewItem> SelectedOrderLogisticsAnaliticsViewItems
        {
            get { return GetProperty(() => SelectedOrderLogisticsAnaliticsViewItems); }
            set { SetProperty(() => SelectedOrderLogisticsAnaliticsViewItems, value); }
        }

        public OrderLogisticsAnaliticsViewItem CurrentOrder
        {
            get { return GetProperty(() => CurrentOrder); }
            set { SetProperty(() => CurrentOrder, value); }
        }

        public bool ShowInvoicesGrid
        {
            get { return GetProperty(() => ShowInvoicesGrid); }
            set { SetProperty(() => ShowInvoicesGrid, value); }
        }

        public bool ShowMovementsGrid
        {
            get { return GetProperty(() => ShowMovementsGrid); }
            set { SetProperty(() => ShowMovementsGrid, value); }
        }

        public bool ShowOrdersGrid
        {
            get { return GetProperty(() => ShowOrdersGrid); }
            set { SetProperty(() => ShowOrdersGrid, value); }
        }

        #endregion

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IAsyncCommand InvoiceMoveCommand { get; }

        public IAsyncCommand MovementMoveCommand { get; }

        public IAsyncCommand OrderMoveCommand { get; }

        public IDelegateCommand InvoiceHandleRowDoubleClickCommand { get; }

        public IDelegateCommand MovementHandleRowDoubleClickCommand { get; }

        public IDelegateCommand OrderHandleRowDoubleClickCommand { get; }

        public IDelegateCommand OpenCloseInvoiceGridCommand { get; }

        public IDelegateCommand OpenCloseMovementGridCommand { get; }

        public IDelegateCommand OpenCloseOrderGridCommand { get; }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            IsSearchPanelClosed = false;

            ShowOrdersGrid = ShowInvoicesGrid = ShowMovementsGrid = true;

            await Filter.RefreshAsync();
            CancelFilteringCommand.Execute(null);
        }

        private async Task RefreshAsync()
        {
            await Task.WhenAll(LoadLogisticsAnaliticsInfoAsync(Filter.GetFilteringItem()));
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private async Task LoadLogisticsAnaliticsInfoAsync(LogisticsAnaliticsFilteringItem filteringItem)
        {
            OrderLogisticsAnaliticsViewItems.Clear();
            InvoiceLogisticsAnaliticsViewItems.Clear();
            MovementLogisticsAnaliticsViewItems.Clear();

            if (filteringItem.WarehouseIds?.Any() != true)
            {
                return;
            }

            LogisticsAnaliticsDto logisticsAnaliticsDto = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryLogisticsAnalitics(filteringItem)),
                "загрузке данных",
                "Загрузка данных выполнена",
                this,
                true,
                false);

            if (logisticsAnaliticsDto?.Invoices?.Any() == true)
            {
                ReadOnlyObservableCollection<InvoiceLogisticsAnaliticsViewItem> invoices = logisticsAnaliticsDto.Invoices.Select(x => _mapper.Map<InvoiceLogisticsAnaliticsViewItem>(x)).ToReadOnlyObservableCollection();

                InvoiceLogisticsAnaliticsViewItems.AddRange(invoices
                    .OrderByDescending(x => x.OverdueDateArrive)
                    .ThenByDescending(x => x.OrdersCount)
                    .ThenByDescending(x => x.Rows)
                    .ThenByDescending(x => x.OrderRows)
                    .ThenByDescending(x => x.Weight));
            }

            if (logisticsAnaliticsDto?.Movements?.Any() == true)
            {
                ReadOnlyObservableCollection<MovementLogisticsAnaliticsViewItem> movements = logisticsAnaliticsDto.Movements.Select(x => _mapper.Map<MovementLogisticsAnaliticsViewItem>(x)).ToReadOnlyObservableCollection();

                MovementLogisticsAnaliticsViewItems.AddRange(movements
                    .OrderByDescending(x => x.Overdue)
                    .ThenByDescending(x => x.OrdersCount)
                    .ThenByDescending(x => x.OrderRows)
                    .ThenByDescending(x => x.OrdersPrice)
                    .ThenByDescending(x => x.Weight));

                MovementLogisticsAnaliticsViewItems.ForEach(x => x.SetTypeMovement(filteringItem.WarehouseIds));
            }

            if (logisticsAnaliticsDto?.Orders?.Any() == true)
            {
                ReadOnlyObservableCollection<OrderLogisticsAnaliticsViewItem> orders = logisticsAnaliticsDto.Orders.Select(x => _mapper.Map<OrderLogisticsAnaliticsViewItem>(x)).ToReadOnlyObservableCollection();

                OrderLogisticsAnaliticsViewItems.AddRange(orders
                    .OrderByDescending(x => x.State.Id)
                    .ThenByDescending(x => x.Overdue)
                    .ThenByDescending(x => x.Rows)
                    .ThenByDescending(x => x.Price)
                    .ThenByDescending(x => x.Weight));
            }
        }

        private async Task InvoiceMoveAsync(InvoiceLogisticsAnaliticsViewItem item)
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.InvoiceDelay))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            GetDateTimeFromUserParameter fromUserParameter = new GetDateTimeFromUserParameter(
                "Дата прибытия",
                "Введите дату и время прибытия",
                true,
                DateTime.Now,
                nowIsMinTime: true);

            GetDateTimeFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetDateTimeFromUserViewModel>(fromUserParameter, this);

            if (fromUserViewModel.IsOk)
            {
                if (fromUserViewModel.DateTime < DateTime.Today)
                {
                    MessageFacadeService.ShowNotificationWarning("Дата должна быть больше текущей");
                }

                InvoiceDelayViewModel delayViewModel = DialogDocumentManagerService.ShowView<InvoiceDelayViewModel>(new InvoiceDelayParameter(SelectedInvoiceLogisticsAnaliticsViewItem.InvoiceId, fromUserViewModel.DateTime!.Value, Filter.SelectedWarehouseId), this);

                if (delayViewModel.IsOk)
                {
                    await RefreshAsync();
                }
            }
        }

        private async Task MovementMoveAsync(MovementLogisticsAnaliticsViewItem item)
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.MovementDelay))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            GetDateTimeFromUserParameter fromUserParameter = new GetDateTimeFromUserParameter(
                "Дата прибытия\\отправки",
                $"Дата прибытия\\отправки",
                true,
                DateTime.Now,
                nowIsMinTime: true);

            GetDateTimeFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetDateTimeFromUserViewModel>(fromUserParameter, this);

            if (fromUserViewModel.IsOk)
            {
                MovementDelayViewModel delayViewModel = DialogDocumentManagerService.ShowView<MovementDelayViewModel>(new MovementDelayParameter(SelectedMovementLogisticsAnaliticsViewItem.MovementId, fromUserViewModel.DateTime!.Value, SelectedMovementLogisticsAnaliticsViewItem.State.Id, SelectedMovementLogisticsAnaliticsViewItem.WarehouseToId), this);

                if (delayViewModel.IsOk)
                {
                    await RefreshAsync();
                }
            }
        }

        private async Task OrderMoveAsync(ObservableCollection<OrderLogisticsAnaliticsViewItem> items)
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.OrderSetDeliveryDate))
            {
                MessageFacadeService.ShowNotificationWarning("У вас нет прав на выполнение операции");
                return;
            }

            bool canEditDateByStatus = items.All(item => item.State == OrderStatus.Received || item.State == OrderStatus.Confirmed || item.State == OrderStatus.Packed);

            if (!canEditDateByStatus)
            {
                MessageFacadeService.ShowNotificationWarning("В выбранных заказах есть заказ с недопустимым статусом");
                return;
            }

            OrderFilteringItem orderFilteringItem = new OrderFilteringItem(string.Empty, new List<int>())
            {
                OrderNumbers = string.Join(", ", items.Select(x => x.OrderId))
            };

            List<OrderDto> listOrders = await WebClient.ExecuteApiRequestAsync(new QueryOrders(orderFilteringItem)).GetPagedResultDataAsync();

            OrderViewItem[] viewItems = listOrders.Select(x => _mapper.Map<OrderViewItem>(x)).ToArray();

            ValidationResultItem[] validationResultItems = ValidationOrders()?.ToArray();

            if (validationResultItems?.Any() == true)
            {
                MessageFacadeService.ShowValidationResultView("Ошибки при переносе заказов", validationResultItems, this);
                return;
            }

            List<Task<(bool Success, int OrderId)>> lockTasks = SelectedOrderLogisticsAnaliticsViewItems.Select(x => TryLockOrderAsync(x.OrderId)).ToList();

            await Task.WhenAll(lockTasks);

            if (lockTasks.Any(x => x.Result.Success != true))
            {
                int[] locked = lockTasks.Where(x => x.Result.Success).Select(x => x.Result.OrderId).ToArray();

                MessageFacadeService.ShowValidationResultView(
                    "Ошибки при блокировки заказов",
                    lockTasks.Where(x => x.Result.Success != true).Select(x => new ValidationResultItem($"Заказа №{x.Result.OrderId} заблокирован", true)),
                    this);

                await Task.WhenAll(locked.Select(x => TryUnlockOrderAsync(x)));

                return;
            }

            OrderEditDeliveryDateViewModel model = null;

            int[] orderIds = items.Select(x => x.OrderId).ToArray();

            model = DialogDocumentManagerService.ShowView<OrderEditDeliveryDateViewModel>(orderIds, this);

            List<Task> unlockTasks = SelectedOrderLogisticsAnaliticsViewItems.Select(x => TryUnlockOrderAsync(x.OrderId)).ToList();

            await Task.WhenAll(unlockTasks);

            if (model?.IsOk == true)
            {
                await RefreshAsync();
            }

            IEnumerable<ValidationResultItem> ValidationOrders()
            {
                foreach (var viewItem in viewItems)
                {
                    if (viewItem.BufferWarehouseId == null)
                    {
                        Dictionary<int, OrderProductViewItem[]> orderProductsFolders = viewItem.Products
                            .Where(x => x.OrderFolderId.HasValue)
                            .GroupBy(x => x.OrderFolderId.Value)
                            .ToDictionary(x => x.Key, y => y.ToArray());

                        foreach (KeyValuePair<int, OrderProductViewItem[]> orderProductsFolder in orderProductsFolders)
                        {
                            OrderProductViewItem[] orderProducts = orderProductsFolder.Value;

                            if (orderProducts.Any(x => x.AssemblyIncluded && x.ProductTypeId != ProductType.AssemblyServiceId)
                                && orderProducts.Any(x => x.ProductTypeId == ProductType.AssemblyServiceId))
                            {
                                yield return new ValidationResultItem($"В заказе {viewItem.Id} не заполнен буферный склад", true);
                            }
                        }
                    }
                }
            }
        }

        private void InvoiceHandleRowDoubleClick()
        {
            if (SelectedInvoiceLogisticsAnaliticsViewItem == null)
            {
                return;
            }

            _messenger.Send(new InvoiceEditViewMessage(SelectedInvoiceLogisticsAnaliticsViewItem.InvoiceId));
        }

        private void MovementHandleRowDoubleClick()
        {
            if (SelectedMovementLogisticsAnaliticsViewItem == null)
            {
                return;
            }

            _messenger.Send(new MovementViewMessage(SelectedMovementLogisticsAnaliticsViewItem.MovementId));
        }

        private void OrderHandleRowDoubleClick()
        {
            if (CurrentOrder == null)
            {
                return;
            }

            _messenger.Send(new OrderEditViewMessage(CurrentOrder.OrderId));
        }

        private void OpenCloseInvoiceGrid(bool parameneter)
        {
            ShowInvoicesGrid = parameneter;
        }

        private void OpenCloseMovementGrid(bool parameneter)
        {
            ShowMovementsGrid = parameneter;
        }

        private void OpenCloseOrderGrid(bool parameneter)
        {
            ShowOrdersGrid = parameneter;
        }

        private async Task<(bool, int)> TryLockOrderAsync(int orderId)
        {
            try
            {
                LockResponse<OrderDto> lockResponse = await WebClient.ExecuteApiRequestAsync(new LockOrder(orderId));

                return (lockResponse.Success, orderId);
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при блокировании заказа");
                Logger.LogError(exception, "Failed to block order");
            }

            return (false, orderId);
        }

        private async Task TryUnlockOrderAsync(int orderId)
        {
            try
            {
                await WebClient.ExecuteApiRequestAsync(new UnlockOrder(orderId));
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при разблокировании заказа");
                Logger.LogError(exception, "Failed to unblock order");
            }
        }
    }
}