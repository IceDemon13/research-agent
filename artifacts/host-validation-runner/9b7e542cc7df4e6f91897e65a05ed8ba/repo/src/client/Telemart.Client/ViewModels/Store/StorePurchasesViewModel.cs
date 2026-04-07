using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.ErrorHandling;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.Requests.Features.Purchase;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.Store.Purchase;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class StorePurchasesViewModel : ViewModelBase, ISupportHotkeys
    {
        private const string SourceAlreadyEmptyWarning = "Данная строка уже не имеет источника";
        private const string SourceAlreadySetWarning = "Для данной строки источник уже задан";

        private IReadOnlyCollection<CategoryDto> categories;

        public StorePurchasesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory,
            ProductInformationViewModel productInformationViewModel,
            ILogger<StorePurchasesViewModel> logger,
            IErrorHandler errorHandler)
            : this()
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            ErrorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            LockableOperationProcessor = lockableOperationProcessorFactory.Create<OrderDto>();
            ProductInformation = productInformationViewModel;
            Logger = logger;

            Subdivisions = new ObservableCollection<Subdivision>(Dictionaries.GetItems<Subdivision>());

            Messenger.Register<PurchaseMessage>(this, OnPurchaseMessage);
            Messenger.Register<OrderMessage>(this, OnOrderMessage);
            Messenger.Register<OnOrderBeforeEditMessage>(this, _ => IsLongOperationInProgress = false);
        }

        public StorePurchasesViewModel()
        {
            HandleLoadedCommand = new DelegateCommand(HandleLoaded);
            SetPurchasesSourceCommand = new AsyncCommand(SetPurchasesSourceAsync, () => Purchases != null && Purchases.Any());
            RefreshCommand = new AsyncCommand(RefreshAsync);
            ShowFilterPopupHandlerCommand = new DelegateCommand<FilterPopupEventArgs>(ShowFilterPopupHandler);
            CustomColumnSortHandlerCommand = new DelegateCommand<CustomColumnSortEventArgs>(CustomColumnSortHandler);
            ShowSetWarehouseSourceDialogCommand = new AsyncCommand(ShowSetWarehouseSourceDialogAsync, CanSetSource);
            ShowSetMovementSourceDialogCommand = new AsyncCommand(ShowSetMovementSourceDialogAsync, CanSetSource);
            ShowSetInvoiceSourceDialogCommand = new DelegateCommand(ShowSetInvoiceSourceDialog, CanSetSource);
            ShowSetNoProductSourceDialogCommand = new AsyncCommand(ShowSetNoProductSourceDialogAsync, CanSetSource);
            SetNoneSourceCommand = new DelegateCommand(SetNoneSource, CanSetSource);
            EditOrderCommand = new DelegateCommand<PurchaseViewItem>(EditOrder);
            EditInvoiceCommand = new DelegateCommand<PurchaseViewItem>(EditInvoice);
            ReconfirmOrderCommand = new AsyncCommand<PurchaseViewItem>(ReconfirmOrderAsync, x => x != null);
        }

        public event Action OnPurchaseSourceUpdated;

        #region Commands

        public IDelegateCommand CustomColumnSortHandlerCommand { get; }

        public IDelegateCommand HandleLoadedCommand { get; }

        public IAsyncCommand SetPurchasesSourceCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand SetNoneSourceCommand { get; }

        public IDelegateCommand ShowFilterPopupHandlerCommand { get; }

        public IDelegateCommand ShowSetInvoiceSourceDialogCommand { get; }

        public IAsyncCommand ShowSetNoProductSourceDialogCommand { get; }

        public IAsyncCommand ShowSetWarehouseSourceDialogCommand { get; }

        public IAsyncCommand ShowSetMovementSourceDialogCommand { get; }

        public IDelegateCommand EditOrderCommand { get; }

        public IDelegateCommand EditInvoiceCommand { get; }

        public IAsyncCommand ReconfirmOrderCommand { get; }

        #endregion

        #region INPC properties

        public ObservableCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public PurchaseViewItem CurrentPurchase
        {
            get { return GetProperty(() => CurrentPurchase); }
            set { SetProperty(() => CurrentPurchase, value, CurrentPurchaseChangedCallback); }
        }

        public ObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            set { SetProperty(() => ProductInformation, value); }
        }

        public ObservableCollection<PurchaseViewItem> Purchases
        {
            get { return GetProperty(() => Purchases); }
            private set { SetProperty(() => Purchases, value); }
        }

        public ObservableCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public ObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        public bool CanOrderReconfirm
        {
            get { return GetProperty(() => CanOrderReconfirm); }
            private set { SetProperty(() => CanOrderReconfirm, value); }
        }

        #endregion

        private IDictionaries Dictionaries { get; }

        private ILogger<StorePurchasesViewModel> Logger { get; }

        private IMapper Mapper { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private LockableOperationProcessor<OrderDto> LockableOperationProcessor { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IWebClient WebClient { get; }

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;
                case HotkeyMessageType.ShowColumnChooser:
                    IsColumnChooserVisible = !IsColumnChooserVisible;
                    handled = true;
                    break;
            }

            return handled;
        }

        private static PurchaseEmployeeType GetEmployeeKind(int? employeeSupId, int authenticatedEmployeeId)
        {
            PurchaseEmployeeType res;

            if (employeeSupId == null)
            {
                res = null;
            }
            else if (employeeSupId == 1)
            {
                res = PurchaseEmployeeType.SystemUser;
            }
            else if (employeeSupId == authenticatedEmployeeId)
            {
                res = PurchaseEmployeeType.CurrentUser;
            }
            else
            {
                res = PurchaseEmployeeType.AnotherUser;
            }

            return res;
        }

        private static PurchasesSourceSaveDto GetPurchaseSaveSource(PurchaseViewItem purchase)
        {
            PurchasesSourceSaveDto saveDto = new PurchasesSourceSaveDto
            {
                InvoiceId = purchase.ProductInvoiceId.Value,
                OrderId = purchase.OrderId,
                OrderProductId = purchase.ProductId,
                SourceId = OrderProductSourceType.PurchaseId
            };

            return saveDto;
        }

        private bool CanSetSource()
        {
            return CurrentPurchase != null;
        }

        private void CustomColumnSortHandler(CustomColumnSortEventArgs e)
        {
            if (e.Column.FieldName.Equals(nameof(PurchaseViewItem.ProductSourceId), StringComparison.Ordinal))
            {
                OrderProductSource source1 = Purchases[e.ListSourceRowIndex1].ProductSource;
                OrderProductSource source2 = Purchases[e.ListSourceRowIndex2].ProductSource;

                int result = source1.Id.CompareTo(source2.Id);

                if (result == 0)
                {
                    result = string.Compare(source1.SourceText, source2.SourceText, StringComparison.Ordinal);
                }

                e.Result = result;
                e.Handled = true;
            }
        }

        private async Task SetPurchasesSourceAsync()
        {
            List<PurchasesSourceSaveDto> saveDtos = new List<PurchasesSourceSaveDto>();

            List<PurchaseViewItem> purchases = Purchases.Where(x => !x.ProductSource.Real).ToList();

            try
            {
                foreach (PurchaseViewItem purchase in purchases.Where(x => x.ProductInvoiceId.HasValue && x.ProductStateId != OrderProductStatus.Clarify.Id && x.ProductSourceId == OrderProductSourceType.OtherId))
                {
                        saveDtos.Add(GetPurchaseSaveSource(purchase));
                }

                if (saveDtos.Any())
                {
                    bool confirm = MessageFacadeService.Confirm($"Проставить товары в накладные в количестве {saveDtos.Count} шт.");

                    if (!confirm)
                    {
                        return;
                    }
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Нечего проставлять");
                    return;
                }

                IsLongOperationInProgress = true;

                List<SetPurchasesSourceResult> results = await WebClient.ExecuteApiRequestAsync(new UpdatePurchasesSource(saveDtos));

                List<ValidationResultItem> validationItems = new List<ValidationResultItem>();

                int i = 1;
                foreach (SetPurchasesSourceResult result in results)
                {
                    if (result.Success)
                    {
                        Messenger.Send(new PurchaseMessage(result.Purchase, MessageType.Changed));
                    }
                    else
                    {
                        validationItems.Add(new ValidationResultItem($"{i}. {result.SourceText}. {ErrorExtensions.GetErrorMessageById(result.ErrorCodeId.Value)}", true));
                        i++;
                    }
                }

                IsLongOperationInProgress = false;

                if (validationItems.Any())
                {
                    ShowValidationResultView("Ошибки:", validationItems);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Товары успешно добавлены");
                    RefreshCommand.Execute(null);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to set purchase sources");
                MessageFacadeService.ShowNotificationError("Ошибка установки источников");
            }
        }

        private void HandleLoaded()
        {
            if (Categories != null)
            {
                return;
            }

            CanOrderReconfirm = WebClient.IsOperationAllowed(BusinessOperation.OrderReconfirm);

            RefreshCommand.Execute(null);
        }

        private void CurrentPurchaseChangedCallback()
        {
            ProductInformation.ClearProduct();

            if (CurrentPurchase != null && CurrentPurchase.ProductProductId > 0)
            {
                ProductInformation.ProductId = new ProductInfoId(CurrentPurchase.ProductProductId, CurrentPurchase.ProductCurrencyOutId, CurrentPurchase.OrderClientId);
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                Categories = null;

                CurrentPurchase = null;

                await Task.WhenAll(RefreshEmployeesAsync(), RefreshWarehousesAsync(), RefreshCategoriesAsync(), RefreshPurchasesAsync());

                // Show only those categories which have any purchase
                HashSet<int> presentCategoryIds = Purchases.SelectMany(x => x.ProductCategoryIds).ToHashSet();

                Categories = categories
                    .Where(x => presentCategoryIds.Contains(x.Id))
                    .Select(x => Mapper.Map<CategoryViewItem>(x))
                    .ToObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to refresh purchases");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshPurchasesAsync()
        {
            Purchases = null;
            IReadOnlyCollection<PurchaseDto> purchases = await WebClient.ExecuteApiRequestAsync(new QueryPurchases()).GetPagedResultDataAsync();
            Purchases = purchases.Select(MapToViewItem).ToObservableCollection();
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
            Warehouses = warehouses.ToObservableCollection();
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            Employees = employees.ToObservableCollection();
        }

        private async Task RefreshCategoriesAsync()
        {
            categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();
        }

        private PurchaseViewItem MapToViewItem(PurchaseDto source, PurchaseViewItem destination)
        {
            destination = Mapper.Map(source, destination);
            destination.PurchaseEmployeeType = GetEmployeeKind(source.InvoiceEmployeeSupId, WebClient.AuthenticatedEmployee.Id);
            return destination;
        }

        private PurchaseViewItem MapToViewItem(PurchaseDto source)
        {
            return MapToViewItem(source, PurchaseViewItem.Create());
        }

        private void SetNoneSource()
        {
            if (CurrentPurchase.ProductSource.Id != OrderProductSourceType.None.Id)
            {
                if (MessageFacadeService.Confirm("Вы уверены?"))
                {
                    RemovePurchaseSource();
                }
            }
            else
            {
                ShowWarning(SourceAlreadyEmptyWarning);
            }
        }

        private void ShowFilterPopupHandler(FilterPopupEventArgs e)
        {
            switch (e.Column.FieldName)
            {
                case nameof(PurchaseViewItem.ProductSourceId):

                    e.ComboBoxEdit.ItemsSource = Dictionaries.GetItems<OrderProductSourceType>()
                        .Select(x => new CustomComboBoxItem { DisplayValue = x.Id == 0 ? "Не задан" : x.Name, EditValue = x.Id })
                        .ToList();

                    break;
            }
        }

        private void ShowSetInvoiceSourceDialog()
        {
            if (!CurrentPurchase.ProductSource.Real)
            {
                SizeableDialogDocumentManagerService.ShowView<SetInvoiceSourceViewModel>(BuildSetSourceParameter(), this);
            }
            else
            {
                ShowWarning(SourceAlreadySetWarning);
            }
        }

        private async Task ShowSetNoProductSourceDialogAsync()
        {
            if (!CurrentPurchase.ProductSource.Real)
            {
                await LockableOperationProcessor.DoOperationAsync(
                    CurrentPurchase.OrderId,
                    async orderDto =>
                    {
                        SetNoProductParameter parameter = new SetNoProductParameter(
                            orderDto,
                            CurrentPurchase.ProductId,
                            CurrentPurchase.ProductProductId,
                            CurrentPurchase.ProductQuantity,
                            CurrentPurchase.ProductPriceOut,
                            CurrentPurchase.ProductSourceId);

                        CreateReasonNoProductModel model = DialogDocumentManagerService.ShowView<CreateReasonNoProductModel>(parameter, this);

                        if (model.IsOk)
                        {
                            PurchaseSourceSaveDto dto = PurchaseSourceSaveDto.NoProduct(model.Content);

                            dto.AllowLockedByMe = true;

                            bool result = UpdatePurchaseSource(dto);

                            OrderSetUnavailableProductDto orderSetUnavailableProductDto = model.OrderSetUnavailableProductDto;

                            if (result && orderSetUnavailableProductDto != null)
                            {
                                orderDto = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderDto.Id));

                                await FillSourcesInternalAsync(orderDto, orderSetUnavailableProductDto);

                                RefreshCommand.Execute(null);
                            }
                        }
                    },
                    false);
            }
            else
            {
                ShowWarning(SourceAlreadySetWarning);
            }
        }

        private async Task ShowSetWarehouseSourceDialogAsync()
        {
            if (!CurrentPurchase.ProductSource.Real)
            {
                try
                {
                    QueryPurchaseWarehouseSources gatewayRequest = new QueryPurchaseWarehouseSources(
                        CurrentPurchase.ProductProductId,
                        CurrentPurchase.OrderWarehouseId,
                        CurrentPurchase.OrderFolderTypeId == OrderFolderType.AssembledComputerRule.Id);

                    List<PurchaseWarehouseSourceDto> sources = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                    if (sources.Any(x => (x.WarehouseItems - x.ReservedQuantity) >= CurrentPurchase.ProductQuantity))
                    {
                        SizeableDialogDocumentManagerService.ShowView<SetWarehouseSourceViewModel>(
                            new object[] { BuildSetSourceParameter(), sources },
                            this);
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationWarning(
                            $"Товара нигде нет в свободном остатке {CurrentPurchase.ProductQuantity.ToString(CultureInfo.InvariantCulture)} шт.");
                    }
                }
                catch (UnexpectedSatusException exception)
                {
                    MessageFacadeService.ShowNotificationError(exception.Args.Error.GetErrorMessage());
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, Resources.ErrorDuringDataLoading);
                    MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                }
            }
            else
            {
                ShowWarning(SourceAlreadySetWarning);
            }
        }

        private async Task ShowSetMovementSourceDialogAsync()
        {
            if (!CurrentPurchase.ProductSource.Real)
            {
                try
                {
                    QueryPurchaseMovementSources gatewayRequest = new QueryPurchaseMovementSources(
                        CurrentPurchase.ProductProductId,
                        CurrentPurchase.OrderId);

                    List<PurchaseMovementSourceDto> sources = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                    if (sources.Any(x => x.AvailableQuantity >= CurrentPurchase.ProductQuantity))
                    {
                        SizeableDialogDocumentManagerService.ShowView<SetMovementSourceViewModel>(
                            new object[] { BuildSetSourceParameter(), sources },
                            this);
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationWarning(
                            $"Товара нигде нет в свободном остатке {CurrentPurchase.ProductQuantity.ToString(CultureInfo.InvariantCulture)} шт.");
                    }
                }
                catch (UnexpectedSatusException exception)
                {
                    MessageFacadeService.ShowNotificationError(exception.Args.Error.GetErrorMessage());
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, Resources.ErrorDuringDataLoading);
                    MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                }
            }
            else
            {
                ShowWarning(SourceAlreadySetWarning);
            }
        }

        private void ShowWarning(string message)
        {
            MessageFacadeService.ShowNotificationWarning(message);
        }

        private bool UpdatePurchaseSource(PurchaseSourceSaveDto source)
        {
            try
            {
                PurchaseDto purchase = WebClient.ExecuteApiRequest(new UpdatePurchaseSource(CurrentPurchase.ProductId, source));

                Messenger.Send(new PurchaseMessage(purchase, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo("Источник успешно установлен");
                return true;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(exception.Args.Error.GetErrorMessage());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to set purchase source");
                MessageFacadeService.ShowNotificationError("Ошибка установки источника");
            }

            return false;
        }

        private void RemovePurchaseSource()
        {
            try
            {
                Result<PurchaseDto> result = WebClient.ExecuteApiRequest(new RemovePurchaseSource(CurrentPurchase.ProductId));

                Messenger.Send(new PurchaseMessage(result.Data, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo("Источник успешно удaлен");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении источника");
                ShowValidationResultView("Ошибки при удалении источника", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to remove purchase source");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении источника");
                Logger.LogError(exception, "Error while removing purchase source");
            }
        }

        private void EditOrder(PurchaseViewItem purchase)
        {
            IsLongOperationInProgress = true;
            Messenger.Send(new OrderEditViewMessage(purchase.OrderId));
        }

        private void EditInvoice(PurchaseViewItem purchase)
        {
            if (purchase.ProductInvoiceId > 0)
            {
                Messenger.Send(new InvoiceEditViewMessage(purchase.ProductInvoiceId.Value));
            }
        }

        private Task ReconfirmOrderAsync(PurchaseViewItem purchase)
        {
            return LockableOperationProcessor.DoActionAsync(purchase.OrderId, ReconfirmOrderInternal, false);

            void ReconfirmOrderInternal(OrderDto actualOrderObj)
            {
                DialogDocumentManagerService.ShowView<OrderReconfirmViewModel>(new OrderReconfirmParameter(actualOrderObj.Id, purchase.ProductId), this);
            }
        }

        private SetSourceParameter BuildSetSourceParameter()
        {
            WarehouseDto warehouse = Warehouses.FirstOrDefault(x => x.Id == CurrentPurchase.OrderWarehouseId);

            return new SetSourceParameter
            {
                ProductRecordId = CurrentPurchase.ProductId,
                CurrencyId = CurrentPurchase.ProductCurrencyOutId,
                Price = CurrentPurchase.ProductPriceOut,
                Quantity = CurrentPurchase.ProductQuantity,
                ProductId = CurrentPurchase.ProductProductId,
                ProductName = CurrentPurchase.ProductProductName,
                OrderProductState = CurrentPurchase.ProductState,
                OrderId = CurrentPurchase.OrderId,
                OrderState = CurrentPurchase.OrderState,
                OrderWarehouseId = CurrentPurchase.OrderWarehouseId,
                OrderWarehouseName = warehouse?.Name,
                OrderDeliveryTime = CurrentPurchase.OrderDeliveryTime,
                ProductInvoiceId = CurrentPurchase.ProductInvoiceId
            };
        }

        private void OnPurchaseMessage(PurchaseMessage message)
        {
            if (message.MessageType == MessageType.Changed)
            {
                foreach (PurchaseViewItem viewItem in Purchases)
                {
                    if (viewItem.ProductId == message.Entity.ProductId)
                    {
                        MapToViewItem(message.Entity, viewItem);
                        OnPurchaseSourceUpdated?.Invoke();
                        break;
                    }
                }
            }
        }

        private async void OnOrderMessage(OrderMessage message)
        {
            try
            {
                switch (message.MessageType)
                {
                    case MessageType.Added:
                    {
                        RefreshCommand.Execute(null);
                        break;
                    }

                    case MessageType.Changed:
                    {
                        if (Purchases is null)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }

                        if (Purchases is null)
                        {
                            return;
                        }

                        foreach (PurchaseViewItem purchaseViewItem in Purchases)
                        {
                            if (purchaseViewItem.OrderId == message.Entity.Id)
                            {
                                OrderProductDto orderProduct =
                                    message.Entity.Products.FirstOrDefault(x => x.Id == purchaseViewItem.ProductId);

                                if (orderProduct == null)
                                {
                                    continue;
                                }

                                purchaseViewItem.ProductPrice1C = orderProduct.Price1C;
                                purchaseViewItem.ProductPriceOut = orderProduct.PriceOut;
                                purchaseViewItem.ProductCurrencyOutId = orderProduct.CurrencyOutId;
                                purchaseViewItem.ProductInvoiceId = orderProduct.InvoiceId;
                                purchaseViewItem.ProductMovementId = orderProduct.MovementId;
                                purchaseViewItem.ProductPosition = orderProduct.Position;
                                purchaseViewItem.ProductQuantity = orderProduct.Quantity;
                                purchaseViewItem.ProductSourceId = orderProduct.SourceId;
                                purchaseViewItem.ProductSource = Dictionaries.GetOrderProductSource(
                                    orderProduct.SourceId,
                                    orderProduct.WarehouseId,
                                    orderProduct.SourceText,
                                    orderProduct.SourceDate);
                                purchaseViewItem.ProductState =
                                    Dictionaries.GetItemById<OrderProductStatus>(orderProduct.StateId);
                                purchaseViewItem.ProductStateId = orderProduct.StateId;
                                purchaseViewItem.OrderCarryType =
                                    Dictionaries.GetItemById<CarryType>(message.Entity.CarryId);
                                purchaseViewItem.OrderClientId = message.Entity.ClientId;
                                purchaseViewItem.OrderComment = OrderCommentHelper.GetJoinedComment(
                                    message.Entity.CustomerComment,
                                    message.Entity.EmployeeComment,
                                    message.Entity.SystemComment);
                                purchaseViewItem.OrderCreatedOn = message.Entity.CreatedOn;
                                purchaseViewItem.OrderReceiveTime = message.Entity.ReceiveTime;
                                purchaseViewItem.OrderDeliveryTime = message.Entity.DeliveryTime;
                                purchaseViewItem.OrderDeliveryTimeTo = message.Entity.DeliveryTimeTo;
                                purchaseViewItem.OrderEmployeeLockId = message.Entity.EmployeeLockId;
                                purchaseViewItem.OrderEmployeeLockName = message.Entity.EmployeeLock?.Name;
                                purchaseViewItem.OrderConfirmedBy = message.Entity.ConfirmedBy;
                                purchaseViewItem.OrderPayment =
                                    Dictionaries.GetItemById<Payment>(message.Entity.PaymentId);
                                purchaseViewItem.OrderPko = message.Entity.Pko;
                                purchaseViewItem.OrderRt = message.Entity.Rt;
                                purchaseViewItem.OrderState =
                                    Dictionaries.GetItemById<OrderStatus>(message.Entity.StateId);
                                purchaseViewItem.OrderSubdivision =
                                    Dictionaries.GetItemById<Subdivision>(message.Entity.SubdivisionId);
                                purchaseViewItem.OrderWarehouseId = message.Entity.WarehouseId;
                                purchaseViewItem.OrderContainsAssemblyService = message.Entity.Products.Any(
                                    x => x.Product.TypeId == ProductType.AssemblyServiceId);
                            }
                        }
                    }

                    break;

                    default:
                    {
                        Debug.WriteLine($"Unknown order message type {message.MessageType}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Something went wrong when order changed in purchases");
                MessageFacadeService.ShowNotificationError("Непредвиденная ошибка");
            }
        }

        private void ShowValidationResultView(string title, IEnumerable<ValidationResultItem> validationItems)
        {
            SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems), this);
        }

        private async Task FillSourcesInternalAsync(OrderDto lockedOrder, OrderSetUnavailableProductDto orderSetUnavailableProductDto)
        {
            await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new OrderSetUnavailableProduct(lockedOrder.Id, orderSetUnavailableProductDto)),
                "замене товара на альтернативный",
                "Товар в заказе заменен",
                this,
                true);
        }
    }
}