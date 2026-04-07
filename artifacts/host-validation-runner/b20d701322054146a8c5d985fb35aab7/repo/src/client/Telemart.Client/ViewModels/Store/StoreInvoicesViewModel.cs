using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.PriceConversion;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Contractor.Warehouse;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Invoice.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Store.Invoice;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class StoreInvoicesViewModel : ViewModelBase, ISupportHotkeys
    {
        private Dictionary<int, string> supplierWarehousesDictionary;

        public StoreInvoicesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessenger messenger,
            IMessageFacadeService messageFacadeService,
            IPriceConverterFactory priceConverterFactory,
            IMapper mapper,
            DocumentCommands documentCommands,
            IErrorHandler errorHandler,
            ILogger<StoreInvoicesViewModel> logger)
            : this()
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            DocumentCommands = documentCommands;
            PriceConverterFactory = priceConverterFactory;
            ErrorHandler = errorHandler;
            Logger = logger;

            Filter = new StoreInvoicesFilterViewModel(dictionaries, webClient);

            Messenger.Register<InvoiceMessage>(this, OnInvoiceMessage);
        }

        public StoreInvoicesViewModel()
        {
            PrintSnCommand = new DelegateCommand(PrintSn);
            PrintOurBarcodeCommand = new DelegateCommand(PrintOurBarcode);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            MassInvoiceAcceptCommand = new AsyncCommand(MassInvoiceAcceptAsync);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            HandleLoadedCommand = new DelegateCommand(HandleLoaded);
            EditCommand = new DelegateCommand<InvoiceViewItem>(Edit, CanEdit);
            EditInvoiceLogisticsCommand = new DelegateCommand<InvoiceViewItem>(EditInvoiceLogistics, x => x != null);
            CreateInvoiceCommand = new DelegateCommand(CreateInvoice);
            SetCurrencyRateCommand = new AsyncCommand<InvoiceViewItem>(SetCurrencyRateAsync, x => x != null && x.State != InvoiceState.Received);
            EditInvoiceWarehouseCommand = new AsyncCommand<InvoiceViewItem>(EditInvoiceWarehouseAsync, x => x != null && (x.State == InvoiceState.Closed || x.State == InvoiceState.Open || x.State == InvoiceState.Arrived) && x.EmployeeLock == null);
            EditInvoiceBudgetsCommand = new DelegateCommand(EditInvoiceBudgets, () => WebClient.IsOperationAllowed(BusinessOperation.InvoiceEditBudget));
            EditInvoiceCategoryBudgetsCommand = new DelegateCommand(EditInvoiceCategoryBudgets, () => WebClient.IsOperationAllowed(BusinessOperation.InvoiceEditCategoryBudget));
        }

        public StoreInvoicesFilterViewModel Filter { get; }

        #region Commands

        public IDelegateCommand EditInvoiceBudgetsCommand { get; }

        public IDelegateCommand EditInvoiceCategoryBudgetsCommand { get; }

        public IDelegateCommand PrintSnCommand { get; }

        public IDelegateCommand PrintOurBarcodeCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand MassInvoiceAcceptCommand { get; }

        public IAsyncCommand SetCurrencyRateCommand { get; }

        public IAsyncCommand EditInvoiceWarehouseCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand HandleLoadedCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand EditInvoiceLogisticsCommand { get; }

        public IDelegateCommand CreateInvoiceCommand { get; }

        #endregion

        #region INPC

        public List<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        public ObservableCollection<InvoiceViewItem> Invoices
        {
            get { return GetProperty(() => Invoices); }
            private set { SetProperty(() => Invoices, value); }
        }

        public InvoiceViewItem SelectedInvoice
        {
            get { return GetProperty(() => SelectedInvoice); }
            set { SetProperty(() => SelectedInvoice, value, () => RaisePropertiesChanged(nameof(CanCreateReturnInvoice))); }
        }

        public InvoiceViewItem CurrentInvoice
        {
            get { return GetProperty(() => CurrentInvoice); }
            set { SetProperty(() => CurrentInvoice, value); }
        }

        public List<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public DocumentCommands DocumentCommands { get; }

        public bool IsAddAllowed => WebClient.IsOperationAllowed(BusinessOperation.InvoiceCreate);

        public bool IsEditAllowed => WebClient.IsOperationAllowed(BusinessOperation.InvoiceUpdate);

        public bool SetCurrencyRateAllowed => WebClient.IsOperationAllowed(BusinessOperation.InvoiceSetCurrencyRate);

        public bool CanCreateReturnInvoice => SelectedInvoice?.State.Id == InvoiceState.Received.Id;
        
        public bool InvoiceBudgetsTabVisible => WebClient.IsOperationAllowed(BusinessOperation.InvoiceEditBudget) || WebClient.IsOperationAllowed(BusinessOperation.InvoiceEditCategoryBudget);

        public bool IsSearchPanelOpened
        {
            get { return GetProperty(() => IsSearchPanelOpened); }
            set { SetProperty(() => IsSearchPanelOpened, value); }
        }

        #endregion

        private IWebClient WebClient { get; }

        private IMessenger Messenger { get; }

        private IMapper Mapper { get; }

        private IPriceConverterFactory PriceConverterFactory { get; }

        private IErrorHandler ErrorHandler { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private ILogger<StoreInvoicesViewModel> Logger { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsSearchPanelOpened = !IsSearchPanelOpened;
                        handled = true;
                        break;
                }
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
                        EditCommand.Execute(CurrentInvoice);
                        handled = true;
                        break;

                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;

                    case HotkeyMessageType.Add:
                        CreateInvoiceCommand.Execute(null);
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        private static string TryGetSupplierWarehouseName(int? supplierWarehouseId, IReadOnlyDictionary<int, string> supplierWarehouses)
        {
            supplierWarehouses.TryGetValue(supplierWarehouseId ?? 0, out string supplierWarehouse);
            return supplierWarehouse ?? string.Empty;
        }

        private async Task RefreshAsync()
        {
            IsLongOperationInProgress = true;

            try
            {
                Invoices = null;
                Filter.InvoicesCount = 0;

                IFilteringItem filteringItem = Filter.GetInvoiceFilteringItem();

                Contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
                Warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

                await Filter.RefreshAsync();

                List<SupplierWarehouseDto> supplierWarehouses = await WebClient.ExecuteApiRequestAsync(new QueryContractorsWarehouses(), true);
                supplierWarehousesDictionary = supplierWarehouses.ToDictionary(x => x.Id, x => x.Name);

                PagedResult<InvoiceDto> invoices = await WebClient.ExecuteApiRequestAsync(new QueryInvoices(filteringItem));

                IReadOnlyDictionary<int, IPriceConverter> priceConverters = await PriceConverterFactory
                    .CreateForInvoicesAsync(invoices.Data.ToArray());

                Invoices = invoices.Data
                    .Select(x => MapInvoice(x, InvoiceViewItem.Create(), supplierWarehousesDictionary, priceConverters[x.Id]))
                    .ToObservableCollection();

                Filter.InvoicesCount = Invoices.Count;
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                Logger.LogError(exception, "Exception while refreshing grid");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private async Task MassInvoiceAcceptAsync()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.InvoiceSaveComparison))
            {
                MessageFacadeService.ShowNotificationError("У Вас нет прав на выполнение операции");
                return;
            }

            InvoiceFilteringItem allInvoicesFilter = new InvoiceFilteringItem()
            {
                InvoiceStatesIds = new[] { InvoiceState.Arrived.Id },
            };

            PagedResult<InvoiceDto> arrivedInvoicesResult = await WebClient.ExecuteApiRequestAsync(new QueryInvoices(allInvoicesFilter));

            List<MassInvoiceAcceptViewItem> arrivedInvoices = arrivedInvoicesResult.Data
                .Where(x => !string.IsNullOrWhiteSpace(x.SupplierOrganization))
                .GroupBy(x => x.SupplierOrganization)
                .Where(x => x.Count() > 1)
                .SelectMany(x => x)
                .Select(x => Mapper.Map<MassInvoiceAcceptViewItem>(x))
                .OrderBy(x => x.InvoiceId)
                .ToList();

            if (!arrivedInvoices.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Накладных для массовой приемки не найдено");
                return;
            }

            MassInvoiceAcceptViewModel acceptInvoiceViewModel = DialogDocumentManagerService.ShowView<MassInvoiceAcceptViewModel>(new MassInvoiceAcceptParameter(arrivedInvoices), this);

            if (!acceptInvoiceViewModel.IsOk)
            {
                return;
            }

            int[] selectedInvoiceIds = acceptInvoiceViewModel.SelectedInvoices.Select(x => x.InvoiceId).ToArray();

            List<int> lockInvoiceIds = new List<int>();

            SplashScreenManager splashScreenManager = SplashScreenManager.CreateWaitIndicator();
            splashScreenManager.Show();

            foreach (int invoiceId in selectedInvoiceIds)
            {
                LockResponse<InvoiceDto> lockResponse = await TryLockAsync(invoiceId);

                if (lockResponse.Success)
                {
                    lockInvoiceIds.Add(lockResponse.Dto.Id);
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning($"Накладная {lockResponse.Dto.Id} уже заблокирована пользователем: {lockResponse.Dto?.EmployeeLock.Name}");

                    await UnlockRangeAsync(lockInvoiceIds);
                    splashScreenManager.Close();
                    return;
                }
            }

            splashScreenManager.Close();

            SizeableDialogDocumentManagerService.ShowView<MassInvoiceProductsComparisonViewModel>(
                new MassInvoiceProductsComparisonParameter(acceptInvoiceViewModel.SelectedInvoices.SelectMany(x => x.InvoiceProducts).ToObservableCollection(), acceptInvoiceViewModel.SelectedInvoices.Where(x => x.SupplierAllowDocuments).Select(x => x.InvoiceId).ToList()),
                this);

            await UnlockRangeAsync(lockInvoiceIds);
        }

        private async Task UnlockRangeAsync(IEnumerable<int> invoiceIds)
        {
            SplashScreenManager splashScreenManager = SplashScreenManager.CreateWaitIndicator();
            splashScreenManager.Show();

            foreach (int invoiceId in invoiceIds)
            {
                LockResponse<InvoiceDto> unlockResponse = await UnlockInvoiceAsync(invoiceId);

                if (unlockResponse?.Success == true)
                {
                    Messenger.Send(new InvoiceMessage(unlockResponse.Dto, MessageType.Changed));
                }
            }

            splashScreenManager.Close();
        }

        private InvoiceViewItem MapInvoice(
            InvoiceDto source,
            InvoiceViewItem target,
            IReadOnlyDictionary<int, string> supplierWarehouses,
            IPriceConverter priceConverter)
        {
            target = Mapper.Map(source, target);

            target.InvoiceProducts.ForEach(x => x.SetPriceConverter(priceConverter));

            target.SupplierWarehouseName = TryGetSupplierWarehouseName(target.SupplierWarehouseId, supplierWarehouses);
            return target;
        }

        private void HandleLoaded()
        {
            Invoices ??= new ObservableCollection<InvoiceViewItem>();

            if (Invoices.Any())
            {
                return;
            }

            IsSearchPanelOpened = true;

            RefreshCommand.Execute(null);
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterState();
            RefreshCommand.Execute(null);
        }

        private void Edit(InvoiceViewItem invoiceViewItem)
        {
            Messenger.Send(new InvoiceEditViewMessage(invoiceViewItem.Id));
        }

        private bool CanEdit(InvoiceViewItem invoiceViewItem)
        {
            return invoiceViewItem != null;
        }

        private void CreateInvoice()
        {
            DialogDocumentManagerService.ShowView<CreateInvoiceViewModel>(null, this);
        }

        private async Task SetCurrencyRateAsync(InvoiceViewItem invoice)
        {
            InvoiceCurrencyRatesParameter parameter = new(invoice.Id, invoice.CurrencyRates);

            InvoiceCurrencyRatesViewModel viewModel = DialogDocumentManagerService.ShowView<InvoiceCurrencyRatesViewModel>(parameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                InvoiceCurrencyRateDto[] saveDtos = viewModel.CurrencyRates
                    .Where(x => x.ConversionRate.HasValue).Select(x => Mapper.Map<InvoiceCurrencyRateDto>(x))
                    .ToArray();

                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new SetInvoiceCurrencyRate(invoice.Id, saveDtos));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Курс присвоен с предупреждениями");
                    ShowValidationResultView("Предупреждения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Курс успешно присвоен");
                }

                Messenger.Send(new InvoiceMessage(result.Data, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при присвоении курса");
                ShowValidationResultView("Ошибки при присвоении курса", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при присвоении курса");
                ShowValidationResultView("Ошибки при присвоении курса", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при присвоении курса");
                Logger.LogError(exception, "Failed to set rate to invoice");
            }
        }

        private void EditInvoiceBudgets()
        {
            DialogDocumentManagerService.ShowView<InvoiceEditBudgetsViewModel>(null, this);
        }

        private void EditInvoiceCategoryBudgets()
        {
            DialogDocumentManagerService.ShowView<InvoiceEditCategoryBudgetsViewModel>(null, this);
        }

        private async Task EditInvoiceWarehouseAsync(InvoiceViewItem invoice)
        {
            LockResponse<InvoiceDto> lockResponse = await TryLockAsync(invoice.Id);

            if (lockResponse.Success)
            {
                invoice.EmployeeLock = lockResponse.Dto.EmployeeLock;

                EditInvoiceWarehouseParameter parameter = new EditInvoiceWarehouseParameter(invoice.WarehouseId);

                EditInvoiceWarehouseViewModel model = DialogDocumentManagerService.ShowView<EditInvoiceWarehouseViewModel>(parameter, this);

                if (model.IsOk)
                {
                    int warehouseId = model.GetSelectedWarehouseId();

                    await ErrorHandler.HandleErrorsAsync(
                        _ => WebClient.ExecuteApiRequestAsync(new UpdateInvoiceWarehouse(invoice.Id, warehouseId)),
                        "сохранение склада",
                        "Склад изменен",
                        this,
                        true);
                }

                LockResponse<InvoiceDto> unlockResponse = await UnlockInvoiceAsync(invoice.Id);

                if (unlockResponse?.Success == true)
                {
                    Messenger.Send(new InvoiceMessage(unlockResponse.Dto, MessageType.Changed));
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning($"Накладная {lockResponse.Dto.Id} уже заблокирована пользователем: {lockResponse.Dto?.EmployeeLock.Name}");
            }
        }

        private bool ShowValidationResultView(string title, IEnumerable<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }

        private async Task<LockResponse<InvoiceDto>> TryLockAsync(int invoiceId)
        {
            LockResponse<InvoiceDto> lockResponse = null;

            try
            {
                lockResponse = await WebClient.ExecuteApiRequestAsync(new LockInvoice(invoiceId));
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError(Resources.ServerConnectError);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to lock invoice");
                MessageFacadeService.ShowNotificationError("Не удалось заблокировать накладную");
            }

            return lockResponse;
        }

        private async Task<LockResponse<InvoiceDto>> UnlockInvoiceAsync(int invoiceId)
        {
            LockResponse<InvoiceDto> lockResponse = null;

            try
            {
                lockResponse = await WebClient.ExecuteApiRequestAsync(new UnlockInvoice(invoiceId));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to lock invoice");
                MessageFacadeService.ShowNotificationError("Не удалось разблокировать накладную");
            }

            return lockResponse;
        }

        private async void OnInvoiceMessage(InvoiceMessage message)
        {
            try
            {
                if (message?.Entity == null)
                {
                    return;
                }

                ConversionRate[] invoiceConversionRates = message.Entity.CurrencyRates
                    .Select(x => x.CreateConversionRate())
                    .ToArray();

                IPriceConverter priceConverter = await PriceConverterFactory.CreateForInvoiceAsync(message.Entity.SupplierId, invoiceConversionRates);

                switch (message.MessageType)
                {
                    case MessageType.Added:
                        InvoiceViewItem viewItem = MapInvoice(message.Entity, InvoiceViewItem.Create(), supplierWarehousesDictionary, priceConverter);
                        Invoices.Add(viewItem);
                        break;

                    case MessageType.Changed:

                        foreach (InvoiceViewItem invoiceViewItem in Invoices)
                        {
                            if (invoiceViewItem.Id == message.Entity.Id)
                            {
                                MapInvoice(message.Entity, invoiceViewItem, supplierWarehousesDictionary, priceConverter);
                                RaisePropertyChanged(nameof(Invoices));
                                break;
                            }
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create price converter");
            }
        }

        private void EditInvoiceLogistics(InvoiceViewItem invoice)
        {
            DialogDocumentManagerService.ShowView<EditInvoiceLogisticsViewModel>(invoice.Id, this);
        }

        private void PrintSn()
        {
            DialogDocumentManagerService.ShowView<PrintSnViewModel>(null, this);
        }

        private void PrintOurBarcode()
        {
            DialogDocumentManagerService.ShowView<PrintOurBarcodeViewModel>(null, this);
        }
    }
}