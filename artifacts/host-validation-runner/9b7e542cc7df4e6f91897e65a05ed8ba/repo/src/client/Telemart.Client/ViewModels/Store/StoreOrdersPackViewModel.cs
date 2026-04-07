using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using DevExpress.XtraReports;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.PackList;
using Telemart.Client.Data.Requests.Features.PrintReport;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.Requests.Features.Warehouse.Delivery;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.Reports.Order.Assembly;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.PackList;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.Warehouse.Delivery;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.CreateScanSheet;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.PackList;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store
{
    internal sealed class StoreOrdersPackViewModel : TelemartViewModelBase, ISupportHotkeys, IDataErrorInfo
    {
        private IReadOnlyCollection<ContractorDto> contractorsList;
        private IReadOnlyCollection<WarehouseDto> warehousesList;
        private IReadOnlyCollection<DeliveryDto> warehouseDeliveriesList;
        private IReadOnlyCollection<EmployeeDto> employeesList;

        public StoreOrdersPackViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IMediator mediator,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory,
            IPrintingSettingsStore printingSettingsStore,
            DocumentCommands documentCommands)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Messenger = messenger;
            Mediator = mediator;
            LockableOperationProcessorFactory = lockableOperationProcessorFactory;
            PrintingSettingsStore = printingSettingsStore;
            DocumentCommands = documentCommands;

            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand<OrderPackViewItem>(Edit, x => x != null);
            EditDeliveryDateCommand = new AsyncCommand<OrderPackViewItem>(EditDeliveryDateAsync, x => x != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            PackCommand = new AsyncCommand<OrderPackViewItem>(PackAsync, CanPack);
            PrintChequeCommand = new AsyncCommand<OrderPackViewItem>(PrintChequeAsync, x => x != null);
            PrintWarrantyCardCommand = new AsyncCommand<OrderPackViewItem>(PrintWarrantyCardAsync, x => x != null);
            PrintAcceptanceProtocolCommand = new AsyncCommand<OrderPackViewItem>(PrintAcceptanceProtocolAsync, x => x != null);
            PrintTrackNumberCommand = new AsyncCommand<OrderPackViewItem>(PrintTrackNumberAsync, x => !string.IsNullOrWhiteSpace(x?.PackageTtn));
            CreateScanSheetCommand = new DelegateCommand(CreateScanSheet);
            CreatePackListCommand = new DelegateCommand(CreatePackList);
            EditPackListCommand = new AsyncCommand(EditPackListAsync, () => WebClient.IsOperationAllowed(BusinessOperation.PackListChangePackegerCollectorPackList));
            OpenPackListCommand = new AsyncCommand(OpenPackListAsync);
            PackListSettingsCommand = new DelegateCommand(PackListSettings, () => WebClient.IsOperationAllowed(BusinessOperation.PackListSettings));
            PrintAssemblyCommand = new AsyncCommand(PrintAssemblyAsync, () => SelectedOrder != null && (SelectedOrder.State == OrderStatus.Confirmed || SelectedOrder.State == OrderStatus.Packed || SelectedOrder.State == OrderStatus.Done));
            Messenger.Register<OrderMessage>(this, OnOrderMessage);
            Messenger.Register<OnOrderBeforeEditMessage>(this, _ => IsLongOperationInProgress = false);

            Subdivisions.AddRange(Dictionaries.GetItems<Subdivision>());
            Carries.AddRange(Dictionaries.GetItems<CarryType>().Where(x => x.IsActive()));
        }

        public StoreOrdersPackViewModel()
        {
        }

        #region Commands

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand PrintAssemblyCommand { get; }

        public IAsyncCommand EditDeliveryDateCommand { get; }

        public IAsyncCommand PackCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand PrintAcceptanceProtocolCommand { get; }

        public IAsyncCommand PrintChequeCommand { get; }

        public IAsyncCommand PrintWarrantyCardCommand { get; }

        public IAsyncCommand PrintTrackNumberCommand { get; }

        public IDelegateCommand CreateScanSheetCommand { get; }

        public IDelegateCommand CreatePackListCommand { get; }

        public IAsyncCommand EditPackListCommand { get; }

        public IAsyncCommand OpenPackListCommand { get; }

        public IDelegateCommand PackListSettingsCommand { get; }

        public DocumentCommands DocumentCommands { get; }

        #endregion

        #region Collections

        public ObservableRangeCollection<ComboBoxItem> Emplyees { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<CarryType> Carries { get; } = new ObservableRangeCollection<CarryType>();

        public ObservableRangeCollection<ComboBoxItem> Contractors { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<Subdivision> Subdivisions { get; } = new ObservableRangeCollection<Subdivision>();

        public ObservableRangeCollection<ComboBoxItem> Warehouses { get; } = new ObservableRangeCollection<ComboBoxItem>();

        #endregion

        #region INPC

        public DateTime? DeliveryTimeAfter
        {
            get { return GetProperty(() => DeliveryTimeAfter); }
            set { SetProperty(() => DeliveryTimeAfter, value); }
        }

        public DateTime? DeliveryTimeBefore
        {
            get { return GetProperty(() => DeliveryTimeBefore); }
            set { SetProperty(() => DeliveryTimeBefore, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            private set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public int NeedToPack
        {
            get { return GetProperty(() => NeedToPack); }
            private set { SetProperty(() => NeedToPack, value); }
        }

        public int DistinctProductsQuantity
        {
            get { return GetProperty(() => DistinctProductsQuantity); }
            private set { SetProperty(() => DistinctProductsQuantity, value); }
        }

        public string PackListIds
        {
            get { return GetProperty(() => PackListIds); }
            set { SetProperty(() => PackListIds, value); }
        }

        public string OrderNumbers
        {
            get { return GetProperty(() => OrderNumbers); }
            set { SetProperty(() => OrderNumbers, value); }
        }

        public ObservableCollection<OrderPackViewItem> Orders
        {
            get { return GetProperty(() => Orders); }
            private set { SetProperty(() => Orders, value); }
        }

        public ObservableCollection<OrderPackViewItem> AllConfirmedOrders
        {
            get { return GetProperty(() => AllConfirmedOrders); }
            private set { SetProperty(() => AllConfirmedOrders, value); }
        }

        public int Packed
        {
            get { return GetProperty(() => Packed); }
            private set { SetProperty(() => Packed, value, () => RaisePropertiesChanged(nameof(LeftToPack))); }
        }

        public int Total
        {
            get { return GetProperty(() => Total); }
            private set { SetProperty(() => Total, value, () => RaisePropertiesChanged(nameof(LeftToPack))); }
        }

        public int LeftToPackDistinct
        {
            get { return GetProperty(() => LeftToPackDistinct); }
            private set { SetProperty(() => LeftToPackDistinct, value); }
        }

        public int LeftToPack => Total - Packed;

        public ObservableCollection<CarryType> SelectedCarries
        {
            get { return GetProperty(() => SelectedCarries); }
            set { SetProperty(() => SelectedCarries, value); }
        }

        public OrderPackViewItem SelectedOrder
        {
            get { return GetProperty(() => SelectedOrder); }
            set { SetProperty(() => SelectedOrder, value); }
        }

        public ObservableCollection<Subdivision> SelectedSubdivisions
        {
            get { return GetProperty(() => SelectedSubdivisions); }
            set { SetProperty(() => SelectedSubdivisions, value); }
        }

        public int? SelectedWarehousе
        {
            get { return GetProperty(() => SelectedWarehousе); }
            set { SetProperty(() => SelectedWarehousе, value); }
        }

        #endregion

        public bool CanCreateScanSheet => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.TechSupport, Role.Warehouse);

        string IDataErrorInfo.Error => string.Empty;

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IMediator Mediator { get; }

        private ILockableOperationProcessorFactory LockableOperationProcessorFactory { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IDialogService WizardDialogService => GetService<IDialogService>("WizardDialogService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<StoreOrdersPackViewModel> builder)
        {
            builder.Property(x => x.OrderNumbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");

            builder.Property(x => x.SelectedWarehousе)
                .Required(() => Resources.RequiredErrorMessage);
        }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsSearchPanelClosed = !IsSearchPanelClosed;
                        handled = true;
                        break;
                }
            }
            else if (msg.Key == Key.F9)
            {
                if (CanPack(SelectedOrder))
                {
                    PackCommand.Execute(SelectedOrder);
                }

                handled = true;
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Refresh:
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;
                    case HotkeyMessageType.Edit:
                        EditCommand.Execute(SelectedOrder);
                        handled = true;
                        break;
                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override void OnInitializeInDesignMode()
        {
            Orders = new ObservableCollection<OrderPackViewItem>();

            Total = 0;
            Packed = 0;
            NeedToPack = 0;
            DistinctProductsQuantity = 0;
            LeftToPackDistinct = 0;
        }

        protected override async Task HandleLoadedAsync()
        {
            if (Orders != null)
            {
                return;
            }

            IsSearchPanelClosed = true;

            await Task.WhenAll(
                RefreshContractorsAsync(),
                RefreshWarehousesAsync(),
                RefreshEmployeesAsync());

            ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private static void MapLockImage(OrderPackViewItem item, int employeeLockId, string employeeLockName)
        {
            item.LockEmployee = employeeLockId > 0
                ? employeeLockName
                : string.Empty;
        }

        private static bool IsOrderPackAllowed(OrderDto order)
        {
            return order != null
                && order.Products.Any()
                && order.Products.All(y => y.StateId == OrderProductStatus.Agreed.Id && y.WarehouseId == order.WarehouseId && y.SourceId == OrderProductSourceType.WarehouseSource.Id);
        }

        private void CancelFiltering()
        {
            ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void Edit(OrderPackViewItem orderViewItem)
        {
            if (!IsLongOperationInProgress)
            {
                IsLongOperationInProgress = true;
                Messenger.Send(new OrderEditViewMessage(orderViewItem.Id));
            }
        }

        private OrderFilteringItem GetFilteringItem()
        {
            OrderFilteringItem item = new OrderFilteringItem(
                string.Empty,
                SelectedSubdivisions.Select(x => x.Id).ToList());
            if (DeliveryTimeAfter.HasValue)
            {
                item.OrderClosedAfter = DeliveryTimeAfter.Value.Date;
            }

            if (DeliveryTimeBefore.HasValue)
            {
                item.OrderClosedBefore = DeliveryTimeBefore.Value.Date.AddDays(1);
            }

            item.OrderNumbers = OrderNumbers;
            item.Carries = SelectedCarries.Select(x => x.Id).ToList();
            item.Warehouses = new List<int> { SelectedWarehousе.Value };
            item.OrderStatuses = new List<int> { OrderStatus.Packed.Id, OrderStatus.Confirmed.Id };
            item.PackListIds = PackListIds;
            item.PackListInfo = true;

            return item;
        }

        private async Task<OrderPackInfoDto> GetOrderPackInfoAsync(OrderPackViewItem orderViewItem)
        {
            IsLongOperationInProgress = true;

            OrderPackInfoDto info = null;

            try
            {
                Result<OrderPackInfoDto> result = await WebClient.ExecuteApiRequestAsync(new QueryOrderPackInfo(orderViewItem.Id));

                if (result.Warnings.Any())
                {
                    ValidationResultViewModelParameter viewModelParameter = new ValidationResultViewModelParameter(
                        "Предупрежедения",
                        result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());

                    ValidationResultViewModel validationResultViewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                        viewModelParameter,
                        this);

                    if (validationResultViewModel.IsOk)
                    {
                        info = result.Data;
                    }
                }
                else
                {
                    info = result.Data;
                }
            }
            catch (UnexpectedSatusException exception)
            {
                SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                    new ValidationResultViewModelParameter("Ошибки", exception.GetErrorItems()),
                    this);
            }
            catch (UnexpectedErrorException)
            {
                SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                    new ValidationResultViewModelParameter("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }),
                    this);
            }
            finally
            {
                IsLongOperationInProgress = false;
            }

            return info;
        }

        private OrderPackViewItem MapOrder(OrderDto source, OrderPackViewItem target)
        {
            return Mapper.Map(source, target);
        }

        private void OnOrderMessage(OrderMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    {
                        if (!IsOrderPackAllowed(message.Entity))
                        {
                            break;
                        }

                        Orders.Insert(0, MapOrder(message.Entity, OrderPackViewItem.Create()));
                        Total++;
                        break;
                    }

                case MessageType.Changed:
                    {
                        for (int i = 0; i < Orders.Count; i++)
                        {
                            OrderPackViewItem orderViewItem = Orders[i];

                            if (orderViewItem.Id == message.Entity.Id)
                            {
                                if (IsOrderPackAllowed(message.Entity))
                                {
                                    MapOrder(message.Entity, orderViewItem);
                                    MapLockImage(orderViewItem, message.Entity.EmployeeLock?.Id ?? 0, message.Entity.EmployeeLock?.Name ?? string.Empty);
                                }
                                else
                                {
                                    Orders.RemoveAt(i);
                                    Total--;
                                    SelectedOrder = null;
                                }

                                break;
                            }
                        }

                        break;
                    }

                default:
                    {
                        Debug.WriteLine($"Unknown order message type {message.MessageType}");
                        break;
                    }
            }

            Packed = Orders.Count(x => x.State == OrderStatus.Packed);
            NeedToPack = Orders.Count - Packed;
            LeftToPackDistinct = Orders
                .Where(x => x.State != OrderStatus.Packed)
                .Sum(x => x.ProductsCount);
            DistinctProductsQuantity = AllConfirmedOrders.Sum(x => x.ProductsCount);
        }

        private async Task PackAsync(OrderPackViewItem orderViewItem)
        {
            OrderPackInfoDto info = await GetOrderPackInfoAsync(orderViewItem);

            if (info == null)
            {
                return;
            }

            bool locked = await TryLockOrderAsync(orderViewItem.Id);

            if (!locked)
            {
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<OrderPackViewModel>(info, this);

            await TryUnlockOrderAsync(orderViewItem.Id);
        }

        private async Task RefreshAsync()
        {
            if (SelectedWarehousе == null)
            {
                MessageFacadeService.ShowNotificationWarning("Поле Склад не заполнено");
                return;
            }

            SelectedOrder = null;
            IsLongOperationInProgress = true;

            try
            {
                Orders = null;

                await Task.WhenAll(
                    RefreshContractorsAsync(),
                    RefreshWarehousesAsync(),
                    RefreshEmployeesAsync());

                PagedResult<OrderDto> orders = await WebClient.ExecuteApiRequestAsync(new QueryOrders(GetFilteringItem()));

                Orders = orders.Data
                    .Where(IsOrderPackAllowed)
                    .OrderBy(x => x.SubdivisionId)
                    .ThenBy(x => x.CarryId)
                    .ThenBy(x => x.ClientId)
                    .ThenBy(x => x.DeliveryTime)
                    .ThenBy(x => x.CreatedOn)
                    .Select(x => MapOrder(x, OrderPackViewItem.Create()))
                    .ToObservableCollection();

                AllConfirmedOrders = orders.Data
                    .Where(x => x.StateId == OrderStatus.Confirmed.Id)
                    .Select(x => MapOrder(x, OrderPackViewItem.Create()))
                    .ToObservableCollection();

                Total = orders.Data.Count;
                Packed = Orders.Count(x => x.State == OrderStatus.Packed);
                NeedToPack = Orders.Count - Packed;
                DistinctProductsQuantity = AllConfirmedOrders.Sum(x => x.ProductsCount);
                LeftToPackDistinct = Orders.Where(x => x.State != OrderStatus.Packed).Sum(x => x.ProductsCount);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(employees, employeesList))
            {
                return;
            }

            Emplyees.Clear();
            employeesList = employees;
            Emplyees.AddRange(employeesList.Select(x => new ComboBoxItem(x.Id, x.Name)));
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(contractors, contractorsList))
            {
                return;
            }

            Contractors.Clear();
            contractorsList = contractors;
            Contractors.AddRange(contractorsList.Select(x => new ComboBoxItem(x.Id, x.Name)));
        }

        private async Task RefreshWarehousesAsync()
        {
            Task<List<WarehouseDto>> getWarehousesTask = WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
            Task<List<DeliveryDto>> getWarehouseDeliveriesTask = WebClient.ExecuteApiRequestAsync(new QueryWarehouseDeliveries(), true);

            await Task.WhenAll(getWarehousesTask, getWarehouseDeliveriesTask);

            List<WarehouseDto> warehouses = getWarehousesTask.Result;
            IReadOnlyCollection<DeliveryDto> warehouseDeliveries = getWarehouseDeliveriesTask.Result;

            if (ReferenceEquals(warehouses, warehousesList) && ReferenceEquals(warehouseDeliveries, warehouseDeliveriesList))
            {
                return;
            }

            Warehouses.Clear();

            warehousesList = warehouses;
            warehouseDeliveriesList = warehouseDeliveries;

            IEnumerable<ComboBoxItem> enumerable = warehouses
                .Where(x => x.Active == 1
                    && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id)
                    && (x.TypeId == WarehouseKind.Main.Id || x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.Assembly.Id)
                    && warehouseDeliveriesList.Any(y => y.WarehouseId == x.Id))
                .OrderByDescending(x => x.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name));

            Warehouses.AddRange(enumerable);
        }

        private void ResetFilterValues()
        {
            DeliveryTimeAfter = null;
            DeliveryTimeBefore = DateTime.Today;
            OrderNumbers = string.Empty;
            SelectedSubdivisions = new ObservableCollection<Subdivision>();
            SelectedCarries = new ObservableCollection<CarryType>();
            SelectedWarehousе = Warehouses.Any()
                ? Warehouses.First().Id
                : null;
            PackListIds = null;
        }

        private async Task<bool> TryLockOrderAsync(int orderId)
        {
            IsLongOperationInProgress = true;

            bool success = false;

            try
            {
                LockResponse<OrderDto> lockResponse = await WebClient.ExecuteApiRequestAsync(new LockOrder(orderId));

                if (!lockResponse.Success)
                {
                    MessageFacadeService.ShowNotificationWarning($"Заказ уже заблокирован пользователем {lockResponse.Dto.EmployeeLock?.ShortName}");
                }

                Messenger.Send(new OrderMessage(lockResponse.Dto, MessageType.Changed));
                success = lockResponse.Success;
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при блокировании заказа");
                Logger.LogError(exception, "Failed to block order");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }

            return success;
        }

        private async Task TryUnlockOrderAsync(int orderId)
        {
            IsLongOperationInProgress = true;

            try
            {
                LockResponse<OrderDto> unlockResult = await WebClient.ExecuteApiRequestAsync(new UnlockOrder(orderId));
                Messenger.Send(new OrderMessage(unlockResult.Dto, MessageType.Changed));
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при разблокировании заказа");
                Logger.LogError(exception, "Failed to unblock order");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private async Task PrintAssemblyAsync()
        {
            PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

            OrderAssemblyReportSimpleDataDto reportDto = await WebClient.ExecuteApiRequestAsync(new QueryOrderAssemblyReportSimple(SelectedOrder.Id));

            OrderAssemblyReportSimpleData reportData = Mapper.Map<OrderAssemblyReportSimpleData>(reportDto);

            IReport report = new OrderAssemblyReportSimple { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = printSettings?.Main != null
                   ? new PrintReportRequest(report, true, printSettings.Main.Name, printSettings.Main.PaperSource)
                   : new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }

        private async Task PrintAcceptanceProtocolAsync(OrderPackViewItem order)
        {
            try
            {
                await Mediator.Send(new PrintOrderDocumentRequest(order.Id, OrderDocumentType.AcceptanceProtocolId));
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private async Task PrintChequeAsync(OrderPackViewItem order)
        {
            try
            {
                await Mediator.Send(new PrintOrderDocumentRequest(order.Id, OrderDocumentType.ChequeId));
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private async Task PrintWarrantyCardAsync(OrderPackViewItem order)
        {
            try
            {
                ProductsSelectionViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ProductsSelectionViewModel>(
                    new ProductsSelectionParameter(order.Id, false),
                    this);

                if (viewModel.IsOk)
                {
                    await Mediator.Send(new PrintWarrantyCardRequest(
                        order.Id,
                        viewModel.SelectedItems.Select(x => x.Id).ToArray(),
                        viewModel.SeparateWarrantyCards,
                        true));
                }
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private Task PrintTrackNumberAsync(OrderPackViewItem order)
        {
            return order.Carry
                .GetTrackNumberProvider()
                .PrintAsync(order.PackageTtn, true);
        }

        private bool CanPack(OrderPackViewItem order)
        {
            return order != null && order.State == OrderStatus.Confirmed;
        }

        private void CreateScanSheet()
        {
            WarehouseDto warehouse = warehousesList.First(x => x.Id == SelectedWarehousе);

            HashSet<int> availableCarryIds = Orders?.Select(x => x.Carry.Id).ToHashSet() ?? new HashSet<int>();
            HashSet<int> warehouseDeliveriesCarryIds = GetWarehouseDeliveries(warehouse.Id).ToHashSet();

            CreateScanSheetModel model = new CreateScanSheetModel(Dictionaries, availableCarryIds, warehouseDeliveriesCarryIds)
            {
                SelectedState = new ComboBoxItem(OrderStatus.Packed.Id, OrderStatus.Packed.Name),
                SelectedWarehouse = new ComboBoxItem(warehouse.Id, warehouse.Name)
            };

            WizardDialogViewModel<CreateScanSheetModel> wizardDialogViewModel = new WizardDialogViewModel<CreateScanSheetModel>(
                typeof(SearchPageViewModel),
                model,
                this);

            WizardDialogService.ShowDialog(MessageButton.OKCancel, "Создание реестра", wizardDialogViewModel);
        }

        private async Task EditDeliveryDateAsync(OrderPackViewItem orderViewItem)
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.OrderSetDeliveryDate))
            {
                MessageFacadeService.ShowNotificationWarning("У вас нет прав на выполнение операции");
                return;
            }

            bool canEditDateByStatus = orderViewItem.State == OrderStatus.Received || orderViewItem.State == OrderStatus.Confirmed;

            if (!canEditDateByStatus)
            {
                MessageFacadeService.ShowNotificationWarning("Недопустимый статус заказа");
                return;
            }

            await LockableOperationProcessorFactory.Create<OrderDto>().DoActionAsync(orderViewItem.Id, DeliveryDate);

            void DeliveryDate(OrderDto order)
            {
                DialogDocumentManagerService.ShowView<OrderEditDeliveryDateViewModel>(new [] { order.Id }, this);
            }
        }

        private void CreatePackList()
        {
            ComboBoxItem warehouse = Warehouses.FirstOrDefault(x => x.Id == SelectedWarehousе);
            HashSet<int> availableCarryIds = Orders?.Select(x => x.Carry.Id).ToHashSet() ?? new HashSet<int>();
            HashSet<int> warehouseDeliveryCarryIds = GetWarehouseDeliveries(warehouse.Id).ToHashSet();

            PackListCreateParameter createParameter = new PackListCreateParameter(
                OrderStatus.Packed,
                warehouse,
                warehouseDeliveryCarryIds,
                availableCarryIds,
                DateTime.Now.Date.AddHours(20),
                DateTime.Now.Date);

            PackListCreateViewModel viewModel = DialogDocumentManagerService.ShowView<PackListCreateViewModel>(createParameter, this);

            if (viewModel.IsOk)
            {
                ResetFilterValues();

                PackListIds = viewModel.NewPackList.Id.ToString();
                DeliveryTimeBefore = viewModel.NewParkListDate();

                RefreshCommand.Execute(null);
            }
        }

        private async Task EditPackListAsync()
        {
            List<PackListDto> packListDtos = await WebClient.ExecuteApiRequestAsync(new QueryInProgressPackLists(new PackListsInProgressFilteringItem(SelectedWarehousе.Value)));

            if (packListDtos == null || packListDtos.Count == 0)
            {
                packListDtos = await WebClient.ExecuteApiRequestAsync(new QueryInProgressPackLists(new PackListsInProgressFilteringItem(SelectedWarehousе.Value, true)));

                string message = packListDtos?.Any() == true
                    ? "У Вас нет прав на просмотр листов на сборку, которые созданы другими пользователями"
                    : "Для выбранного склада нет активных листов на сборку";

                MessageFacadeService.ShowNotificationWarning(message);

                return;
            }

            PackListChoiceViewModel model = DialogDocumentManagerService.ShowView<PackListChoiceViewModel>(new PackListChoiceParameter(SelectedWarehousе.Value), this);

            if (model.IsOk)
            {
                DialogDocumentManagerService.ShowView<PackListEditViewModel>(new PackListEditParameter(model.GetPackList(), SelectedWarehousе.Value), this);
            }
        }

        private async Task OpenPackListAsync()
        {
            if (SelectedWarehousе.HasValue)
            {
                List<PackListDto> packListDtos = await WebClient.ExecuteApiRequestAsync(new QueryInProgressPackLists(new PackListsInProgressFilteringItem(SelectedWarehousе.Value)));

                if (packListDtos == null || packListDtos.Count == 0)
                {
                    packListDtos = await WebClient.ExecuteApiRequestAsync(new QueryInProgressPackLists(new PackListsInProgressFilteringItem(SelectedWarehousе.Value, true)));

                    string message = packListDtos?.Any() == true
                        ? "У Вас нет прав на просмотр листов на сборку, которые созданы другими пользователями"
                        : "Для выбранного склада нет активных листов на сборку";

                    MessageFacadeService.ShowNotificationWarning(message);

                    return;
                }

                PackListChoiceViewModel model = DialogDocumentManagerService.ShowView<PackListChoiceViewModel>(new PackListChoiceParameter(SelectedWarehousе.Value), this);

                if (model.IsOk)
                {
                    DialogDocumentManagerService.ShowView<PackListViewModel>(new PackListParameter(model.GetPackList(), SelectedWarehousе.Value), this);
                }
            }
        }

        private void PackListSettings()
        {
            DialogDocumentManagerService.ShowView<PackListSettingsViewModel>(null, this);
        }

        private IEnumerable<int> GetWarehouseDeliveries(int warehouseId)
        {
            return warehouseDeliveriesList
                .Where(x => x.WarehouseId == warehouseId)
                .SelectMany(x => x.CarryIds)
                .Distinct()
                .OrderBy(x => x);
        }

        private bool ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }
    }
}