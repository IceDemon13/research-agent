using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Data.Filtering;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using DevExpress.XtraReports;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Business.Order;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Layouts;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.Requests.Features.PrintReport;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.Reports.AdditionalServiceProduct;
using Telemart.Client.Reports.Order.Assembly;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.AdditionalServiceProduct;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Hashtags;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Novaposhta;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.AutoSource;
using Telemart.Client.ViewModels.Store.Order.CreateCompleted;
using Telemart.Client.ViewModels.Store.Order.OrderEditPrice;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store
{
    internal sealed class StoreOrdersViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private List<CustomComboBoxItem> _phoneFilterItems;
        private IReadOnlyDictionary<int, OrderSourceType> _orderSources;

        public StoreOrdersViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IFilterModuleLayoutService<OrderFilteringItem> filterModuleLayoutService,
            IMessenger messenger,
            IMediator mediator,
            IMapper mapper,
            IOrderRules orderRules,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory,
            IPrintingSettingsStore printingSettingsStore,
            IFiscalRegistrarClientFactory fiscalRegistrarClientFactory,
            IErrorHandler errorHandler,
            IRroPrintHelper rroPrintHelper,
            DocumentCommands documentCommands)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mediator = mediator;
            Mapper = mapper;
            OrderRules = orderRules;
            LockableOperationProcessorFactory = lockableOperationProcessorFactory;
            PrintingSettingsStore = printingSettingsStore;
            FiscalRegistrarClientFactory = fiscalRegistrarClientFactory;
            ErrorHandler = errorHandler;
            FilterModuleLayoutService = filterModuleLayoutService;
            DocumentCommands = documentCommands;
            RroPrintHelper = rroPrintHelper;

            AddCommand = new DelegateCommand(Add);
            AddFromTemplateCommand = new DelegateCommand(AddFromTemplate);
            AddPresaleCommand = new DelegateCommand(AddPresale, CanCreatePresale);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand<OrderViewItem>(Edit, CanEdit);
            EditDeliveryDateCommand = new AsyncCommand<OrderViewItem>(EditDeliveryDateAsync, x => x != null);
            EditContractorCommand = new AsyncCommand<OrderViewItem>(EditContractorAsync, x => x != null);
            EditWarrantyCommand = new DelegateCommand<OrderViewItem>(EditWarranty, x => x != null);
            EditPaymentCommand = new AsyncCommand<OrderViewItem>(EditPaymentAsync, x => x != null);
            EditInfoCommand = new AsyncCommand<OrderViewItem>(EditInfoAsync, x => x != null);
            EditPriceCommand = new AsyncCommand<OrderViewItem>(EditPriceAsync, x => x != null);
            CopyOrderCommand = new AsyncCommand<OrderViewItem>(CopyOrderAsync, x => x is { EmployeeLockId: null });
            MergeOrderCommand = new AsyncCommand<OrderViewItem>(MergeOrderAsync, CanMerge);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            SendSmsCommand = new AsyncCommand<OrderViewItem>(SendSmsAsync, x => x != null && (x.OrderSourceId is null || _orderSources.GetValueOrDefault(x.OrderSourceId.Value)?.CanContactCustomer != false));
            CopyWaybillToClipboardCommand = new DelegateCommand<OrderViewItem>(CopyWaybillToClipboard, IsPackageWaybillNumberNotEmpty);
            TrackWaybillCommand = new AsyncCommand<OrderViewItem>(TrackWaybillAsync, CanTrackWaybill);
            PrintChequeCommand = new AsyncCommand<OrderViewItem>(PrintChequeAsync, x => x is not null && !string.IsNullOrEmpty(x.FiscalId));
            PrintActIncomeCommand = new AsyncCommand<OrderViewItem>(PrintActIncomeAsync, CanPrintAct);
            PrintActOutcomeCommand = new AsyncCommand<OrderViewItem>(PrintActOutcomeAsync, CanPrintAct);
            PrintWarrantyCardCommand = new AsyncCommand<OrderViewItem>(PrintWarrantyCardAsync, CanPrint);
            PrintAcceptanceProtocolCommand = new AsyncCommand<OrderViewItem>(PrintAcceptanceProtocolAsync, CanPrint);
            PrintTrackNumberCommand = new AsyncCommand<OrderViewItem>(PrintTrackNumberAsync, CanPrintTrackNumber);
            HandleCustomColumnSortCommand = new DelegateCommand<CustomColumnSortEventArgs>(HandleCustomColumnSort);
            MoveProductCommand = new AsyncCommand<OrderViewItem>(MoveProductsAsync, CanMoveProducts);
            SelectProductCommand = new DelegateCommand(SelectProduct);
            CancelProductCommand = new DelegateCommand(() => Filter.Product = null);
            ShowFilterPopupHandlerCommand = new DelegateCommand<FilterPopupEventArgs>(ShowFilterPopupHandler);
            SetCustomerCommand = new AsyncCommand<OrderViewItem>(SetCustomerAsync, x => x != null && (x.CustomerId == null || x.CustomerId == 1 || WebClient.IsOperationAllowed(BusinessOperation.OrderEditCustomer)) && x.EmployeeLockId == null);
            SetManagerCommand = new AsyncCommand<OrderViewItem>(SetManagerAsync, x => x != null && (x.State == OrderStatus.Received));
            SetCarryCommand = new AsyncCommand<OrderViewItem>(SetCarryAsync, x => x != null && (x.State == OrderStatus.Received || x.State == OrderStatus.Confirmed));
            SetMoneyBackAmountCommand = new AsyncCommand<OrderViewItem>(SetMoneyBackAmountAsync, x => x != null && WebClient.IsOperationAllowed(BusinessOperation.OrderChangeMoneyBackAmount) && x.State == OrderStatus.Done && x.MoneyBackAmount.HasValue && !x.CustomerReceivedOn.HasValue);
            CreateCompletedOrderCommand = new DelegateCommand(CreateCompletedOrder);
            PrintAssemblyCommand = new AsyncCommand(PrintAssemblyAsync, () => SelectedOrder != null && (SelectedOrder.State == OrderStatus.Confirmed || SelectedOrder.State == OrderStatus.Packed || SelectedOrder.State == OrderStatus.Done));
            SetJokerCommand = new DelegateCommand(SetJoker, () => SelectedOrder != null && !SelectedOrder.CustomerId.HasValue && !SelectedOrder.CustomerEstId.HasValue && CanSetJoker && SelectedOrder.EmployeeLockId == null);
            ManagePhoneHashtagsCommand = new DelegateCommand(ManagePhoneHashtags, () => CanManagePhoneHashtags);
            OrderConfigurationCommand = new DelegateCommand(ShowOrderConfiguration, () => OrderConfigurationVisible);
            OrderAutoConfirmSettingsCommand = new DelegateCommand(ShowOrderAutoConfirmSettings, () => OrderAutoConfirmSettingsVisible);
            CreateUnpackEventCommand = new AsyncCommand(CreateUnpackEventAsync, () => CanCreateUnpackEvent && SelectedOrder != null);
            AutoSourceSettingsCommand = new DelegateCommand(ShowAutoSourceSettings, () => WebClient.IsOperationAllowed(BusinessOperation.AutoSourceSettings));
            UpdateNpTtnCommand = new AsyncCommand(UpdateNpTtnAsync, CanUpdateNpTtn);
            ScheduleDeliveryCommand = new DelegateCommand(ScheduleDelivery, CanScheduleDelivery);
            StopCancelingOrderFromSiteCommand = new AsyncCommand(StopCancelingOrderFromSiteAsync, CanStopCancelingOrderFromSite);
            ScheduleDeliveriesCommand = new DelegateCommand(ScheduleDeliveries, () => WebClient.IsOperationAllowed(BusinessOperation.OrderScheduleDelivery));

            Filter = new StoreOrdersFilterViewModel(webClient, dictionaries);

            Messenger.Register<OrderMessage>(this, OnOrderMessage);
            Messenger.Register<OnOrderCreationFinishedMessage>(this, _ => IsAddButtonEnabled = true);
            Messenger.Register<OnOrderBeforeEditMessage>(this, _ => IsLongOperationInProgress = false);

            IsAddButtonEnabled = true;

            CanSetJoker = WebClient.IsOperationAllowed(BusinessOperation.OrderSetJoker);
            CanCreateUnpackEvent = WebClient.IsOperationAllowed(BusinessOperation.OrderUnpackEvent);
            CanManagePhoneHashtags = WebClient.IsOperationAllowed(BusinessOperation.ManageHashtagsByPhone);
            OrderConfigurationVisible = WebClient.IsOperationAllowed(BusinessOperation.OrderConfiguration);
            OrderAutoConfirmSettingsVisible = WebClient.IsOperationAllowed(BusinessOperation.OrderAutoConfirmSettings);
            SettingsVisible = WebClient.IsOperationAllowed(BusinessOperation.OrderAutoConfirmSettings) || WebClient.IsOperationAllowed(BusinessOperation.OrderConfiguration);
            CanSetManager = WebClient.IsOperationAllowed(BusinessOperation.OrderSetManager);
            CanSetCarry = WebClient.IsOperationAllowed(BusinessOperation.OrderSetCarry);
            CanSetMoneyBack = WebClient.IsOperationAllowed(BusinessOperation.OrderChangeMoneyBackAmount);
            CanSetCustomer = WebClient.IsOperationAllowed(BusinessOperation.OrderSetCustomer);
            CanEditProductPrices = WebClient.IsOperationAllowed(BusinessOperation.OrderEditProductPrices);
            CanCreateCompletedOrder = WebClient.IsOperationAllowed(BusinessOperation.OrderCreateCompleted);
            CanChangeContractor = WebClient.IsOperationAllowed(BusinessOperation.OrderChangeContractor);
            CanChangeWarranty = WebClient.IsOperationAllowed(BusinessOperation.OrderChangeWarranty);
            IsOutsourceSeller = WebClient.AuthenticatedEmployee.HasAnyRole(Role.OutsourceSeller);
        }

        public StoreOrdersViewModel()
        {
        }

        #region Commands

        public IAsyncCommand UpdateNpTtnCommand { get; }

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand AddFromTemplateCommand { get; }

        public IDelegateCommand AddPresaleCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand CopyWaybillToClipboardCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand EditDeliveryDateCommand { get; }

        public IAsyncCommand EditContractorCommand { get; }

        public IDelegateCommand EditWarrantyCommand { get; }

        public IAsyncCommand EditPaymentCommand { get; }

        public IDelegateCommand ScheduleDeliveryCommand { get; }

        public IAsyncCommand StopCancelingOrderFromSiteCommand { get; }

        public IDelegateCommand ScheduleDeliveriesCommand { get; }

        public IAsyncCommand EditInfoCommand { get; }

        public IAsyncCommand CopyOrderCommand { get; }

        public IAsyncCommand MergeOrderCommand { get; }

        public IAsyncCommand PrintAssemblyCommand { get; }

        public IDelegateCommand ManagePhoneHashtagsCommand { get; }

        public IDelegateCommand OrderConfigurationCommand { get; }

        public IDelegateCommand OrderAutoConfirmSettingsCommand { get; }

        public IDelegateCommand AutoSourceSettingsCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand SendSmsCommand { get; }

        public IAsyncCommand TrackWaybillCommand { get; }

        public IAsyncCommand PrintAcceptanceProtocolCommand { get; }

        public IAsyncCommand PrintChequeCommand { get; }

        public IAsyncCommand PrintActIncomeCommand { get; }

        public IAsyncCommand PrintActOutcomeCommand { get; }

        public IAsyncCommand PrintWarrantyCardCommand { get; }

        public IAsyncCommand PrintTrackNumberCommand { get; }

        public IDelegateCommand HandleCustomColumnSortCommand { get; }

        public IAsyncCommand MoveProductCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        public IDelegateCommand CancelProductCommand { get; }

        public IDelegateCommand ShowFilterPopupHandlerCommand { get; }

        public IAsyncCommand EditPriceCommand { get; }

        public IAsyncCommand SetCustomerCommand { get; }

        public IDelegateCommand SetJokerCommand { get; }

        public IAsyncCommand CreateUnpackEventCommand { get; }

        public IAsyncCommand SetManagerCommand { get; }

        public IAsyncCommand SetCarryCommand { get; }

        public IDelegateCommand CreateCompletedOrderCommand { get; }

        public DocumentCommands DocumentCommands { get; }

        public IAsyncCommand SetMoneyBackAmountCommand { get; }

        #endregion

        public StoreOrdersFilterViewModel Filter { get; }

        public List<ContractorDto> ContractorsList
        {
            get { return GetProperty(() => ContractorsList); }
            private set { SetProperty(() => ContractorsList, value); }
        }

        public List<WarehouseDto> WarehousesList
        {
            get { return GetProperty(() => WarehousesList); }
            private set { SetProperty(() => WarehousesList, value); }
        }

        public List<CityDto> CitiesList
        {
            get { return GetProperty(() => CitiesList); }
            private set { SetProperty(() => CitiesList, value); }
        }

        public bool IsAddButtonEnabled
        {
            get { return GetProperty(() => IsAddButtonEnabled); }
            set { SetProperty(() => IsAddButtonEnabled, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ObservableRangeCollection<OrderViewItem> Orders { get; } = new ObservableRangeCollection<OrderViewItem>();

        public OrderViewItem SelectedOrder
        {
            get { return GetProperty(() => SelectedOrder); }
            set { SetProperty(() => SelectedOrder, value); }
        }

        public bool ShowProducts
        {
            get { return GetProperty(() => ShowProducts); }
            set { SetProperty(() => ShowProducts, value); }
        }

        public bool CanSetJoker { get; }

        public bool CanCreateUnpackEvent { get; }

        public bool CanManagePhoneHashtags { get; }

        public bool OrderConfigurationVisible { get; }

        public bool OrderAutoConfirmSettingsVisible { get; }

        public bool SettingsVisible { get; }

        public bool CanSetCustomer { get; }

        public bool CanSetManager { get; }

        public bool CanSetCarry { get; }

        public bool CanSetMoneyBack { get; }

        public bool CanEditProductPrices { get; }

        public bool CanCreateCompletedOrder { get; }

        public bool CanChangeContractor { get; }

        public bool CanChangeWarranty { get; }

        public bool IsOutsourceSeller { get; }

        public IFilterModuleLayoutService<OrderFilteringItem> FilterModuleLayoutService { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService NonModalDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalDialogDocumentManagerService");

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IMediator Mediator { get; }

        private IOrderRules OrderRules { get; }

        private ILockableOperationProcessorFactory LockableOperationProcessorFactory { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IFiscalRegistrarClientFactory FiscalRegistrarClientFactory { get; }

        private IErrorHandler ErrorHandler { get; }

        private IRroPrintHelper RroPrintHelper { get; }

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
                    case Key.P:
                        ShowProducts = !ShowProducts;
                        handled = true;
                        break;
                    case Key.Insert:
                        CopyOrderCommand.Execute(SelectedOrder);
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Add:
                        AddCommand.Execute(null);
                        handled = true;
                        break;
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

        protected override async Task HandleLoadedAsync()
        {
            _phoneFilterItems = GetPhoneFilterItems().ToList();
            _orderSources = Dictionaries.GetItems<OrderSourceType>().ToDictionary(x => x.Id, y => y);
            IsSearchPanelClosed = false;
            await Filter.RefreshAsync();
            CancelFilteringCommand.Execute(null);

            FilterModuleLayoutService.Init(Module.OrdersId, this, Filter);
        }

        private static IEnumerable<CustomComboBoxItem> GetPhoneFilterItems()
        {
            yield return new CustomComboBoxItem
            {
                DisplayValue = "Звонить клиенту",
                EditValue = CriteriaOperator.Parse($"[{nameof(OrderViewItem.DontCall)}] = false")
            };

            yield return new CustomComboBoxItem
            {
                DisplayValue = "Не звонить клиенту",
                EditValue = CriteriaOperator.Parse($"[{nameof(OrderViewItem.DontCall)}] = true")
            };
        }

        private static bool IsPackageWaybillNumberNotEmpty(OrderViewItem order)
        {
            return !string.IsNullOrWhiteSpace(order?.PackageTtn);
        }

        private static Task PrintTrackNumberAsync(OrderViewItem order)
        {
            return order.Carry
                .GetTrackNumberProvider()
                .PrintAsync(order.PackageTtn, true);
        }

        private static bool CanPrint(OrderViewItem order)
        {
            return order != null;
        }

        private static bool CanPrintTrackNumber(OrderViewItem order)
        {
            return !string.IsNullOrWhiteSpace(order?.PackageTtn);
        }

        private static bool CanTrackWaybill(OrderViewItem order)
        {
            return !string.IsNullOrWhiteSpace(order?.PackageTtn);
        }

        private void Add()
        {
            if (IsAddButtonEnabled)
            {
                IsAddButtonEnabled = false;
                Messenger.Send(new OrderCreateViewMessage());
            }
        }

        private void AddFromTemplate()
        {
            SelectContractorTemplateViewModel viewModel = DialogDocumentManagerService.ShowView<SelectContractorTemplateViewModel>(ContractorTemplateMode.CreateOrder, this);

            if (viewModel.IsOk)
            {
                Messenger.Send(new OrderCreateViewMessage(viewModel.TemplatesEditor.SelectedContractorTemplate));
            }
        }

        private void AddPresale()
        {
            DialogDocumentManagerService.ShowView<CreatePresaleOrderViewModel>(null, this);
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private bool CanEdit(OrderViewItem order)
        {
            return order != null;
        }

        private void CopyWaybillToClipboard(OrderViewItem order)
        {
            Clipboard.SetDataObject(order.PackageTtn);
        }

        private void Edit(OrderViewItem orderViewItem)
        {
            if (!IsLongOperationInProgress)
            {
                IsLongOperationInProgress = true;
                Messenger.Send(new OrderEditViewMessage(orderViewItem.Id));
            }
        }

        private void OnOrderMessage(OrderMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    {
                        Orders.Insert(0, MapOrder(message.Entity, OrderViewItem.Create()));
                        break;
                    }

                case MessageType.Changed:
                    {
                        foreach (OrderViewItem orderViewItem in Orders)
                        {
                            if (orderViewItem.Id == message.Entity.Id)
                            {
                                MapOrder(message.Entity, orderViewItem);
                                RaisePropertyChanged(nameof(Orders));
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
        }

        private async Task RefreshAsync()
        {
            IsLongOperationInProgress = true;

            try
            {
                await Task.WhenAll(RefreshContractorsAsync(), RefreshWarehousesAsync(), RefreshCitiesAsync());
                await Filter.RefreshAsync();

                Orders.Clear();

                PagedResult<OrderSimpleDto> orders = await WebClient.ExecuteApiRequestAsync(new QuerySimpleOrders(Filter.GetFilteringItem()));

                Orders.AddRange(orders.Data.Select(x => MapOrder(x, OrderViewItem.Create())));

                RaisePropertyChanged(nameof(Orders));
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

            async Task RefreshWarehousesAsync()
            {
                WarehousesList = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
            }

            async Task RefreshCitiesAsync()
            {
                CitiesList = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();
            }

            async Task RefreshContractorsAsync()
            {
                ContractorsList = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
            }
        }

        private async Task SendSmsAsync(OrderViewItem orderViewItem)
        {
            decimal toPayUah = orderViewItem.TotalPrice.Uah;

            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderViewItem.Id));

            DialogDocumentManagerService.ShowView<SendSmsViewModel>(
                new SendSmsParameter(
                    orderViewItem.Id,
                    null,
                    order.ClientPriceTypeId,
                    orderViewItem.Subdivision,
                    orderViewItem.Phone,
                    orderViewItem.Phone2,
                    OrderRules.GetSmsTemplates(
                        orderViewItem.Id,
                        toPayUah,
                        orderViewItem.Payment,
                        orderViewItem.Subdivision,
                        order.LegalEntity?.Id,
                        order.OldClient,
                        orderViewItem.State.Id),
                    orderViewItem.TotalPrice.Uah),
                this);
        }

        private async Task TrackWaybillAsync(OrderViewItem order)
        {
            IsLongOperationInProgress = true;

            try
            {
                await order.Carry.GetTrackNumberProvider().TrackAsync(order.Id, order.PackageTtn);
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private OrderViewItem MapOrder(OrderDto source, OrderViewItem target)
        {
            target.Products.Clear();

            Mapper.Map(source, target);

            return target;
        }

        private OrderViewItem MapOrder(OrderSimpleDto source, OrderViewItem target)
        {
            target.Products.Clear();

            Mapper.Map(source, target);

            return target;
        }

        private async Task PrintAcceptanceProtocolAsync(OrderViewItem order)
        {
            try
            {
                await Mediator.Send(new PrintOrderDocumentRequest(order.Id, OrderDocumentType.AcceptanceProtocolId));
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
                    new ValidationResultViewModelParameter(
                        "Ошибки при получении данных",
                        new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }),
                    this);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private async Task PrintChequeAsync(OrderViewItem order)
        {
            OrderDto orderDto = await WebClient.ExecuteApiRequestAsync(new QueryOrder(order.Id));

            if (string.IsNullOrWhiteSpace(orderDto.FiscalId))
            {
                MessageFacadeService.ShowNotificationError("Номер чека не заполнен в заказе");
                return;
            }

            Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                "Определении настроек РРО",
                null,
                this,
                true);

            if (!clientResult.IsSuccess)
            {
                return;
            }

            int cashboxId = clientResult.Data.Settings.CashboxId;

            CashboxDto cashbox = await WebClient.ExecuteApiRequestAsync(new QueryCashbox(cashboxId));

            if (cashbox.Session?.Closed != false)
            {
                MessageFacadeService.ShowNotificationWarning("Смена закрыта");
                return;
            }

            await PrintOrderOnFiscalRegistrarAsync(order.Id, cashboxId, orderDto.FiscalId, orderDto.Email, orderDto.Phone);
        }

        private bool CanPrintAct(OrderViewItem order)
        {
            int[] guestOrderProductIds = SelectedOrder?.Products?.Where(x => x.ProductTypeId == ProductType.GuestProductId).Select(x => x.Id).ToArray();

            if (guestOrderProductIds?.Any() != true)
            {
                return false;
            }

            return guestOrderProductIds.All(x => SelectedOrder.Products?.Any(y => (y.IsAdditionalService && y.ParentRecordId == x)
                                                                           || (y.IsAdditionalServiceConsumable && y.Id == x)) == true);
        }

        private async Task PrintActIncomeAsync(OrderViewItem order)
        {
            AdditionalServiceProductClientProductReportDataDto data = await WebClient.ExecuteApiRequestAsync(new QueryActAdditionalServiceProductReportData(order.Id));

            if (data.GuestProducts?.Any() != true)
            {
                MessageFacadeService.ShowMessageBoxWarning("В заказе отсутствуют гостевые товары,\nкоторые останутся у нас");
                return;
            }

            AdditionalServiceProductClientProductsReportData reportData = new(DateTime.Now, data.FullNameClient, string.Empty, data.PlaceName, WebClient.AuthenticatedEmployee.Name, true);

            reportData.SetGuestProducs(data.GuestProducts?.Select(x => Mapper.Map<GuestProductReportData>(x)).ToArray());
            reportData.SetNumber(order.Id);

            IReport report = new ActIncomeClientProductReport { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }

        private async Task PrintActOutcomeAsync(OrderViewItem order)
        {
            AdditionalServiceProductsFilteringItem filter = new AdditionalServiceProductsFilteringItem()
            {
                OrderIds = order.Id.ToString()
            };

            PagedResult<AdditionalServiceProductDto> additionalServiceProductsPagedResult = await WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceProducts(filter));
            IReadOnlyCollection<AdditionalServiceProductDto> additionalServiceProducts = additionalServiceProductsPagedResult.Data.ToReadOnlyObservableCollection();

            if (additionalServiceProducts.Any(x => (x.ProductTypeId == ProductType.GuestProductId || x.ConsumableProducts?.Any(y => y.ProductTypeId == ProductType.GuestProductId) == true)
                                                                        && x.StateId != AdditionalServiceProductState.CompletedId))
            {
                MessageFacadeService.ShowMessageBoxWarning("Услуги с гостевыми товарами не завершены");
                return;
            }

            AdditionalServiceProductClientProductReportDataDto dataDto = await WebClient.ExecuteApiRequestAsync(new QueryActAdditionalServiceProductReportData(order.Id));

            if (dataDto.GuestProducts?.Any() != true)
            {
                MessageFacadeService.ShowMessageBoxWarning("В заказе отсутствуют гостевые товары,\nкоторые оставались у нас");
                return;
            }

            AdditionalServiceProductClientProductsReportData reportData = new(DateTime.Now, dataDto.FullNameClient, string.Empty, dataDto.PlaceName, WebClient.AuthenticatedEmployee.Name);

            reportData.SetGuestProducs(dataDto.GuestProducts?.Select(x => Mapper.Map<GuestProductReportData>(x)).ToArray());
            reportData.SetNumber(order.Id);

            IReport report = new ActOutcomeClientProductReport { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }

        private async Task PrintOrderOnFiscalRegistrarAsync(int id, int cashboxId, string fiscalId, string email, string phone)
        {
            Result resultSentRroCheck = await RroPrintHelper.SentCheckAsync(fiscalId, phone, email, cashboxId, this, true);

            if (resultSentRroCheck.IsSuccess)
            {
                await WebClient.ExecuteApiRequestAsync(new ConfirmOrderOnFiscalRegistrar(id, fiscalId, true));
            }
        }

        private async Task PrintWarrantyCardAsync(OrderViewItem order)
        {
            try
            {
                if (!order.Products.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("В заказе должен быть хотя бы один товар");
                    return;
                }

                ProductsSelectionViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ProductsSelectionViewModel>(
                    new ProductsSelectionParameter(order.Id, order.SeparateWarrantyCards),
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
                SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                    new ValidationResultViewModelParameter("Ошибки", exception.GetErrorItems()),
                    this);
            }
            catch (UnexpectedErrorException)
            {
                SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                    new ValidationResultViewModelParameter(
                        "Ошибки при получении данных",
                        new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }),
                    this);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private async Task CopyOrderAsync(OrderViewItem orderViewItem)
        {
            if (IsLongOperationInProgress)
            {
                return;
            }

            IsLongOperationInProgress = true;

            OrderDto orderDto = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderViewItem.Id));

            OrderCopyEditContractorViewModel viewModel = new OrderCopyEditContractorViewModel(WebClient, Dictionaries, MessageFacadeService, Mapper);

            DialogDocumentManagerService.ShowView(
                "OrderEditContractorView",
                viewModel,
                new OrderEditContractorParameter(0, orderDto.ClientId),
                this);

            if (!viewModel.IsOk)
            {
                IsLongOperationInProgress = false;
                return;
            }

            orderDto.ClientId = viewModel.SelectedContractor.Id;

            Messenger.Send(new OrderCopyViewMessage(orderDto));
        }

        private async Task EditDeliveryDateAsync(OrderViewItem viewItem)
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.OrderSetDeliveryDate))
            {
                MessageFacadeService.ShowNotificationWarning("У вас нет прав на выполнение операции");
                return;
            }

            bool canEditDateByStatus = viewItem.State == OrderStatus.Received || viewItem.State == OrderStatus.Confirmed;

            if (!canEditDateByStatus)
            {
                MessageFacadeService.ShowNotificationWarning("Недопустимый статус заказа");
                return;
            }

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
                        MessageFacadeService.ShowNotificationWarning("В заказе не заполнен буферный склад");
                        return;
                    }
                }
            }

            await LockableOperationProcessorFactory.Create<OrderDto>().DoActionAsync(viewItem.Id, EditDeliveryDate);

            void EditDeliveryDate(OrderDto order)
            {
                DialogDocumentManagerService.ShowView<OrderEditDeliveryDateViewModel>(new [] { order.Id }, this);
            }
        }

        private void EditWarranty(OrderViewItem order)
        {
            DialogDocumentManagerService.ShowView<EditOrderWarrantyViewModel>(order.Id, this);
        }

        private Task EditContractorAsync(OrderViewItem viewItem)
        {
            return LockableOperationProcessorFactory.Create<OrderDto>().DoActionAsync(viewItem.Id, EditContractor);

            void EditContractor(OrderDto order)
            {
                OrderEditContractorViewModel viewModel = new OrderEditContractorViewModel(WebClient, Dictionaries, MessageFacadeService, Mapper, Messenger);

                DialogDocumentManagerService.ShowView(
                    "OrderEditContractorView",
                    viewModel,
                    new OrderEditContractorParameter(order.Id, order.ClientId),
                    this);
            }
        }

        private async Task EditPaymentAsync(OrderViewItem viewItem)
        {
            await LockableOperationProcessorFactory.Create<OrderDto>().DoActionAsync(viewItem.Id, EditPayment);

            void EditPayment(OrderDto order)
            {
                DialogDocumentManagerService.ShowView<OrderEditPaymentViewModel>(new OrderEditPaymentParameter(order.Id, order.SubdivisionId, order.PaymentId, order.GetTotalAmount().Uah, changePayment: true), this);
            }
        }

        private Task EditInfoAsync(OrderViewItem viewItem)
        {
            return LockableOperationProcessorFactory.Create<OrderDto>().DoActionAsync(viewItem.Id, EditInfo);

            void EditInfo(OrderDto order)
            {
                DialogDocumentManagerService.ShowView<OrderEditInfoViewModel>(order, this);
            }
        }

        private Task SetCustomerAsync(OrderViewItem viewItem)
        {
            return LockableOperationProcessorFactory.Create<OrderDto>().DoActionAsync(viewItem.Id, SetCustomer);

            void SetCustomer(OrderDto order)
            {
                DialogDocumentManagerService.ShowView<SetCustomerViewModel>(new SetCustomerParameter("Привязка клиента к заказу", SetCustomerAsync), this);
            }
        }

        private async Task<bool> SetCustomerAsync(CustomerDto customer)
        {
            DelayedConfirmViewModel viewModel = DialogDocumentManagerService
                .ShowView<DelayedConfirmViewModel>(
                    $"Вы уверены что хотите привязать заказ №{SelectedOrder.Id} к клиенту \"{customer.Fio}\"?",
                    this);

            if (!viewModel.IsOk)
            {
                return false;
            }

            bool success = false;

            try
            {
                Result<OrderDto> result =
                    await WebClient.ExecuteApiRequestAsync(new OrderSetCustomer(SelectedOrder.Id, customer.Id));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    const string ErrorMessage = "Клиент привязан с ошибками";

                    MessageFacadeService.ShowValidationResultView(ErrorMessage, validationResultItems, this);

                    MessageFacadeService.ShowNotificationWarning(ErrorMessage);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Клиент успешно привязан");
                }

                success = true;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при привязке клиента");
                MessageFacadeService.ShowValidationResultView(
                    "Ошибки при привязке клиента",
                    exception.GetErrorItems(),
                    this);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to set customer to order");
                MessageFacadeService.ShowValidationResultView(
                    Resources.ServerConnectError,
                    new[] { new ValidationResultItem(Resources.ServerUnavailable, true) },
                    this);
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при привязке клиента");
                Logger.LogError(exception, "Error while seting customer to order");
            }

            return success;
        }

        private void ShowOrderConfiguration()
        {
            DialogDocumentManagerService.ShowView<OrderConfigurationViewModel>(null, this);
        }

        private void ShowOrderAutoConfirmSettings()
        {
            DialogDocumentManagerService.ShowView<OrderAutoConfirmSettingsViewModel>(null, this);
        }

        private async Task CreateUnpackEventAsync()
        {
            UnpackOrderEventInfoDto dto = await WebClient.ExecuteApiRequestAsync(new QueryUnpackOrderEventInfo(SelectedOrder.Id));

            if (dto.Errors?.Any() == true)
            {
                MessageFacadeService.ShowValidationResultView("Ошибки", dto.Errors.Select(x => new ValidationResultItem(x, true)).ToArray(), this);
                return;
            }

            DialogDocumentManagerService.ShowView<CreateUnpackOrderEventViewModel>(
                new CreateUnpackOrderEventParameter(
                SelectedOrder.Id,
                SelectedOrder.WarehouseId ?? 0,
                dto.EventForWarehouseEmployees,
                dto.TaskForPickupEmployees),
                this);
        }

        private async Task StopCancelingOrderFromSiteAsync()
        {
            Result<OrderDto> orderResult = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new StopCancelingOrderFromSite(SelectedOrder.Id)),
                "при остановки oтмены заказа с сайта",
                "Отмена заказа с сайта остановлена",
                this,
                true);

            if (orderResult.IsSuccess)
            {
                SelectedOrder.CanceledFromSite = orderResult.Data.CanceledFromSite;
            }
        }

        private async Task UpdateNpTtnAsync()
        {
            NpDocumentDto npDocument = await WebClient.ExecuteApiRequestAsync(new QueryNpDocument(SelectedOrder.PackageTtn));

            if (npDocument.ReceiveDate.HasValue)
            {
                MessageFacadeService.ShowNotificationError("Заказ уже получен");
                return;
            }

            UpdateNovaposhtaTtnParameter parameter = new UpdateNovaposhtaTtnParameter(SelectedOrder.Id, npDocument);

            DialogDocumentManagerService.ShowView<UpdateNovaposhtaTtnViewModel>(parameter, this);
        }

        private void ScheduleDeliveries()
        {
            ScheduleDeliveryCityCarryViewModel viewModel = DialogDocumentManagerService.ShowView<ScheduleDeliveryCityCarryViewModel>(null, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            DialogDocumentManagerService.ShowView<ScheduleDeliveriesViewModel>(
                new ScheduleDeliveriesParameter(
                    viewModel.SelectedCarryId!.Value,
                    viewModel.SelectedCityId!.Value,
                    viewModel.SelectedDate!.Value,
                    viewModel.Orders),
                this);
        }

        private void ScheduleDelivery()
        {
            if (!SelectedOrder.Carry.ScheduleDelivery)
            {
                MessageFacadeService.ShowNotificationError("Способ доставки не поддерживает планирование");
                return;
            }

            DialogDocumentManagerService.ShowView<ScheduleDeliveryViewModel>(new ScheduleDeliveryParameter(SelectedOrder.Id), this);
        }

        private void ShowAutoSourceSettings()
        {
            SizeableDialogDocumentManagerService.ShowView<AutoSourceSettingsViewModel>(null, this);
        }

        private void ManagePhoneHashtags()
        {
            GetTextFromUserViewModel phoneViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                new GetTextFromUserParameter(contentCaption: null, "Укажите номер телефона", null, contentMask: "(000) 000-00-00"),
                this);

            if (!phoneViewModel.IsOk)
            {
                return;
            }

            DialogDocumentManagerService.ShowView<ManagePhoneHashtagsViewModel>(
              new ManagePhoneHashtagsParameter(phoneViewModel.Content), this);
        }

        private async Task SetCarryAsync(OrderViewItem orderViewItem)
        {
            ChangeItemViewModel viewModel = DialogDocumentManagerService.ShowView<ChangeItemViewModel>(
                new ChangeItemParameter(
                    Dictionaries.GetItems<CarryType>().Where(x => x.CanSwitchInOrders).Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                    orderViewItem.Carry.Name,
                    $"Способ доставки по заказу №{orderViewItem.Id}"),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            (await ErrorHandler.HandleErrorsAsync(
                _ =>
                    WebClient.ExecuteApiRequestAsync(
                        new UpdateOrderCarry(orderViewItem.Id, new UpdateOrderCarryDto(viewModel.NewItem!.Value.Id))),
                "сохранении способа доставки",
                "Способ доставки сохранен",
                this,
                true)).IfNotNull(_ =>
            {
                orderViewItem.Carry = Dictionaries.GetItemById<CarryType>(viewModel.NewItem!.Value.Id);
            });
        }

        private Task SetMoneyBackAmountAsync(OrderViewItem order)
        {
            return LockableOperationProcessorFactory.Create<OrderDto>().DoActionAsync(order.Id, SetManager);

            void SetManager(OrderDto orderDto)
            {
                DialogDocumentManagerService.ShowView<ChangeMoneyBackAmountViewModel>(orderDto.Id, this);
            }
        }

        private Task SetManagerAsync(OrderViewItem viewItem)
        {
            return LockableOperationProcessorFactory.Create<OrderDto>().DoActionAsync(viewItem.Id, SetManager);

            void SetManager(OrderDto order)
            {
                DialogDocumentManagerService.ShowView<OrderSetManagerViewModel>(new OrderSetManagerParameter(order.Id, order.ManagerEmployeeId), this);
            }
        }

        private Task EditPriceAsync(OrderViewItem viewItem)
        {
            if (viewItem.Rt != 1)
            {
                MessageFacadeService.ShowNotificationWarning("Запрещено менять цены в непроведенном в 1С заказе");
                return Task.CompletedTask;
            }

            if (viewItem.State != OrderStatus.Done)
            {
                MessageFacadeService.ShowNotificationWarning("Заказ должен быть в статусе \"Выполнен\"");
                return Task.CompletedTask;
            }

            if (Payment.IsCreditPayment(viewItem.Payment.Id))
            {
                MessageFacadeService.ShowNotificationWarning("Запрещено менять цены в кредитных заказах");
                return Task.CompletedTask;
            }

            return LockableOperationProcessorFactory.Create<OrderDto>().DoActionAsync(viewItem.Id, EditPrice, false);

            void EditPrice(OrderDto orderEditInfo)
            {
                DialogDocumentManagerService.ShowView<OrderEditPriceViewModel>(orderEditInfo.Id, this);
            }
        }

        private async Task PrintAssemblyAsync()
        {
            PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

            OrderAssemblyReportDataDto reportDto = await WebClient.ExecuteApiRequestAsync(new QueryOrderAssemblyReport(SelectedOrder.Id));

            OrderAssemblyReportData reportData = Mapper.Map<OrderAssemblyReportData>(reportDto);

            reportData.SetDateTimeAssemblyPrint(DateTime.Now);

            IReport report = new OrderAssemblyReport { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = printSettings?.Main != null
                   ? new PrintReportRequest(report, true, printSettings.Main.Name, printSettings.Main.PaperSource)
                   : new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }

        private void SetJoker()
        {
            DialogDocumentManagerService.ShowView<SelectHashtagsViewModel>(
                  new SelectHashtagsParameter("Выбор отрицательных тегов для шутника", true, false, true, false, SetJokerOkCommandAsync), this);
        }

        private async Task<bool> SetJokerOkCommandAsync((List<int> plusHashTagIds, List<int> minusHashtagIds) hashtags)
        {
            bool success = false;

            try
            {
                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(new CreateJoker(SelectedOrder.Id, hashtags.minusHashtagIds));

                SelectedOrder = Mapper.Map<OrderViewItem>(result.Data);

                success = true;
                MessageFacadeService.ShowNotificationInfo("Шутник успешно создан");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowValidationResultView("Ошибки при создании шутника", exception.GetErrorItems(), this);
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowValidationResultView("Ошибки при создании шутника", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, this);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while creating joker");
                MessageFacadeService.ShowNotificationError("Ошибки при создании шутника");
            }

            return success;
        }

        private async Task MergeOrderAsync(OrderViewItem order)
        {
            IsLongOperationInProgress = true;

            try
            {
                OrderDto orderFromServer = await WebClient.ExecuteApiRequestAsync(new QueryOrder(order.Id));

                OnOrderMessage(new OrderMessage(orderFromServer, MessageType.Changed));

                if (orderFromServer.EmployeeLockId.HasValue && orderFromServer.EmployeeLockId != WebClient.AuthenticatedEmployee.Id)
                {
                    MessageFacadeService.ShowNotificationWarning($"Заказ уже заблокирован пользователем {orderFromServer.EmployeeLock.ShortName}");
                    return;
                }

                if (Dictionaries.GetItemById<Subdivision>(orderFromServer.SubdivisionId).IsRetail)
                {
                    MessageFacadeService.ShowNotificationWarning("Функция объединения доступна только для оптовых заказов");
                    return;
                }

                if (orderFromServer.StateId != OrderStatus.Received.Id)
                {
                    MessageFacadeService.ShowNotificationWarning($"Статус заказа должен быть \"{OrderStatus.Received.Name}\"");
                    return;
                }

                if (orderFromServer.CityId == null || orderFromServer.WarehouseId == null)
                {
                    MessageFacadeService.ShowNotificationWarning("Не все поля заказа заполнены");
                    return;
                }

                List<int> wholesaleSubdivisionsIds = Dictionaries.GetItems<Subdivision>()
                    .Where(x => !x.IsRetail)
                    .Select(x => x.Id)
                    .ToList();

                OrderFilteringItem filteringItem = new OrderFilteringItem(null, wholesaleSubdivisionsIds)
                {
                    Contractors = new List<int> { orderFromServer.ClientId },
                    Cities = new List<int> { orderFromServer.CityId.Value },
                    Carries = new List<int> { orderFromServer.CarryId },
                    Warehouses = new List<int> { orderFromServer.WarehouseId.Value },
                    Payments = new List<int> { orderFromServer.PaymentId },
                    OrderStatuses = new List<int> { OrderStatus.Received.Id }
                };

                PagedResult<OrderDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryOrders(filteringItem));

                List<OrderDto> ordersParam = pagedResult.Data.Except(pagedResult.Data.Where(x => x.Id == orderFromServer.Id)).ToList();

                if (ordersParam.Any())
                {
                    MergeOrdersParameter parameter = new MergeOrdersParameter(orderFromServer, ordersParam);

                    MergeOrdersViewModel viewModel = DialogDocumentManagerService.ShowView<MergeOrdersViewModel>(parameter, this);

                    if (viewModel.IsOk)
                    {
                        await RefreshAsync();
                        SelectedOrder = Orders.FirstOrDefault(x => x.Id == order.Id);
                    }
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning("Нечего объединять");
                }
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                Logger.LogError(exception, "Failed to initialize orders merge");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private void CreateCompletedOrder()
        {
            if (!string.IsNullOrWhiteSpace(WebClient.AuthenticatedEmployee.CardKey))
            {
                GetPasswordFromUserParameter fromUserParameter = new GetPasswordFromUserParameter(
                    "Ключ",
                    "Верификация пользователя");

                GetPasswordFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetPasswordFromUserViewModel>(fromUserParameter, this);

                if (!fromUserViewModel.IsOk)
                {
                    return;
                }

                if (fromUserViewModel.Content != WebClient.AuthenticatedEmployee.CardKey)
                {
                    MessageFacadeService.ShowNotificationWarning("Невалидный ключ");
                    return;
                }
            }

            NonModalDialogDocumentManagerService.ShowView<CreateCompletedOrderViewModel>(new CreateCompletedOrderParameter(WebClient.AuthenticatedEmployee.ContractorTemplateId), this);
        }

        private void HandleCustomColumnSort(CustomColumnSortEventArgs e)
        {
            if (e.Column.FieldName == nameof(OrderViewItem.TotalPrice))
            {
                OrderViewItem o1 = Orders[e.ListSourceRowIndex1];
                OrderViewItem o2 = Orders[e.ListSourceRowIndex2];

                decimal totalCost1 = o1.TotalCostUsd ?? 0;
                decimal totalCost2 = o2.TotalCostUsd ?? 0;

                e.Result = totalCost1.CompareTo(totalCost2);
                e.Handled = true;
            }
        }

        private bool CanMerge(OrderViewItem order)
        {
            return order is { EmployeeLockId: null } && WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.Manager);
        }

        private bool CanCreatePresale()
        {
            return WebClient.IsOperationAllowed(BusinessOperation.OrderCreatePresale);
        }

        private bool CanScheduleDelivery()
        {
            return WebClient.IsOperationAllowed(BusinessOperation.OrderScheduleDelivery)
                   && SelectedOrder is { DeliveryTime: { }, DeliveryTimeTo: { }, Carry.Kind.Id: CarryTypeKind.CourierId }
                   && (SelectedOrder.State == OrderStatus.Confirmed || SelectedOrder.State == OrderStatus.Packed);
        }

        private bool CanStopCancelingOrderFromSite()
        {
            return WebClient.IsOperationAllowed(BusinessOperation.OrderStopCancelingFromSite)
                   && SelectedOrder != null
                   && (SelectedOrder.State == OrderStatus.Confirmed || SelectedOrder.State == OrderStatus.Packed || SelectedOrder.State == OrderStatus.Received)
                   && SelectedOrder.CanceledFromSite;
        }

        private bool CanUpdateNpTtn()
        {
            return SelectedOrder?.PackageTtn is not null
                   && SelectedOrder.Carry.CarryProviderId == CarryProvider.NovaPoshtaId
                   && (SelectedOrder.State == OrderStatus.Packed || SelectedOrder.State == OrderStatus.Done)
                   && (WebClient.IsOperationAllowed(BusinessOperation.UpdateNpTtn) || WebClient.IsOperationAllowed(BusinessOperation.UpdateReceiverNpTtn));
        }

        private bool CanMoveProducts(OrderViewItem order)
        {
            return order != null && WebClient.AuthenticatedEmployee.HasAnyRole(
                                     Role.Admin,
                                     Role.TechSupport,
                                     Role.Manager,
                                     Role.Seller,
                                     Role.Operator,
                                     Role.Warehouse,
                                     Role.Packager)
                                 && !Payment.IsCreditPayment(order.Payment?.Id)
                                 && order.EmployeeLockId is null
                                 && order.Payment?.Id != Payment.LiqPayId
                                 && order.Payment?.Id != Payment.MonoPayId
                                 && order.Payment?.Id != Payment.NovaPayId
                                 && order.Payment?.Id != Payment.PortmoneId;
        }

        private Task MoveProductsAsync(OrderViewItem viewItem)
        {
            return LockableOperationProcessorFactory.Create<OrderDto>().DoActionAsync(viewItem.Id, MoveProducts);

            void MoveProducts(OrderDto lockedOrder)
            {
                SizeableDialogDocumentManagerService.ShowView<MoveProductViewModel>(lockedOrder, this);
            }
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();

                Filter.Product = product.Name;
            }
        }

        private void ShowFilterPopupHandler(FilterPopupEventArgs args)
        {
            switch (args.Column.FieldName)
            {
                case nameof(OrderViewItem.PhoneFormatted):
                    args.ComboBoxEdit.ItemsSource = _phoneFilterItems;
                    args.Handled = true;
                    break;
            }
        }
    }
}