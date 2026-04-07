using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using DevExpress.Export.Xl;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using KellermanSoftware.CompareNetObjects;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Business.PriceConversion;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Contractor.Warehouse;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Invoice.Actions;
using Telemart.Client.Data.Requests.Features.Parser;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Telegram;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.SupplierBill.Create;
using Telemart.Client.ViewModels.Validation;
using Telemart.Client.WebClient.Prices;
using Telemart.Common.ErrorHandling;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class InvoiceViewModel : TelemartDialogViewModelBase, IDisposable
    {
        private const int RefreshIntervalSeconds = 10;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly CompareLogic compareLogic;

        private IReadOnlyDictionary<int, string> contractors;
        private IReadOnlyDictionary<int, string> warehouses;
        private IReadOnlyDictionary<int, string> supplerWarehouses;
        private IReadOnlyDictionary<int, string> employees;

        private InvoiceSaveDto originalSaveDto;

        public InvoiceViewModel(
            IWebClient webClient,
            IPricesClient pricesClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper,
            IPriceConverterFactory priceConverterFactory,
            IErrorHandler errorHandler,
            ProductInformationViewModel productInformationViewModel)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mapper = mapper;
            PriceConverterFactory = priceConverterFactory;
            ErrorHandler = errorHandler;
            PricesClient = pricesClient;

            LoadValuesCommand = new DelegateCommand<InvoiceProductViewItem>(LoadValues);
            AddProductCommand = new AsyncCommand(AddProductAsync, CanAddProducts);
            DeleteProductCommand = new DelegateCommand(DeleteProducts, CanDeleteProducts);
            CompareProductsCommand = new AsyncCommand(CompareProductsAsync);
            AssignToMeCommand = new AsyncCommand(AssignSelectedInvoicesToCurrentUserAsync, CanAssign);
            ExportToXlsxCommand = new DelegateCommand<TableView>(ExportToXlsx, CanExport);
            OpenInvoiceCommand = new AsyncCommand(OpenInvoiceAsync);
            CloseInvoiceCommand = new AsyncCommand(CloseInvoiceAsync);
            ArriveInvoiceCommand = new AsyncCommand(ArriveInvoiceAsync);
            ReceiveInvoiceCommand = new AsyncCommand(ReceiveInvoiceAsync);
            DontReceiveInvoiceCommand = new AsyncCommand(DontReceiveInvoiceAsync);
            CancelInvoiceCommand = new AsyncCommand(CancelInvoiceAsync);
            NotifySupplierInTelegramCommand = new AsyncCommand(NotifySupplierInTelegramAsync, () => NotifySupplierInTelegramVisible);
            BulkAddProductCommand = new DelegateCommand(BulkAddProduct, CanAddProducts);
            ShowDimensionsCommand = new AsyncCommand(ShowDimensionsAsync);
            SetCurrencyRateCommand = new AsyncCommand(SetCurrencyRateAsync, () => webClient.IsOperationAllowed(BusinessOperation.InvoiceSetCurrencyRate));
            DelayInvoiceCommand = new DelegateCommand(Delay);
            CreateBillCommand = new DelegateCommand(CreateBill);
            AnalyzeCommand = new AsyncCommand(() => AnalyzeAsync(true), () => Invoice?.InvoiceProducts?.Any() == true);
            AddAdditionalCostCommand = new DelegateCommand(AddAdditionalCost, () => Invoice?.State != InvoiceState.Open && Invoice?.State != InvoiceState.Cancelled && WebClient.IsOperationAllowed(BusinessOperation.InvoiceAdditionalCostCreate));
            EditAdditionalCostCommand = new DelegateCommand(EditAdditionalCost, () => Invoice?.State != InvoiceState.Open && Invoice?.State != InvoiceState.Cancelled && WebClient.IsOperationAllowed(BusinessOperation.InvoiceAdditionalCostUpdate) && SelectedAdditionalCost != null);
            DeleteAdditionalCostCommand = new AsyncCommand(DeleteAdditionalCostAsync, () => Invoice?.State != InvoiceState.Open && Invoice?.State != InvoiceState.Cancelled && WebClient.IsOperationAllowed(BusinessOperation.InvoiceAdditionalCostDelete) && SelectedAdditionalCost != null);

            SwitchIgnoreTransitCommand = new AsyncCommand(SwitchIgnoreTransitAsync, () => webClient.IsOperationAllowed(BusinessOperation.InvoiceSwitchIgnoreTransit));

            PurchaseCommand = new AsyncCommand(PurchaseInvoiceAsync);
            SetAutoReserveCommand = new AsyncCommand(SetAutoReserveAsync, () => Invoice?.SupplierAutoReserve != null && Invoice.State == InvoiceState.Open);

            SelectedInvoiceProductViewItems = new ObservableCollection<InvoiceProductViewItem>();
            SelectedInvoiceProductViewItems.CollectionChanged += SelectedInvoiceProductViewItemsCollectionChanged;

            ProductInformation = productInformationViewModel;
            EditorsNames = new ObservableRangeCollection<string>();

            compareLogic = new CompareLogic(new ComparisonConfig
            {
                CompareChildren = true,
                CompareFields = false,
                ComparePrivateFields = false,
                CompareProperties = true,
                CompareStaticProperties = false,
                CompareStaticFields = false,
                CompareReadOnly = true,
                ComparePrivateProperties = false,
                MembersToIgnore = new List<string> { nameof(IDataErrorInfo.Error) },
                AutoClearCache = false,
                IgnoreCollectionOrder = false
            });

            Messenger.Register<EntityMessage<InvoiceAdditionalCostDto>>(this, OnInvoiceAdditionalCostMessage);

            CellValueChangedCommand = new DelegateCommand<CellValueChangedEventArgs>(CellValueChanged);
        }

        public InvoiceViewModel()
        {
        }

        #region Commands

        public IDelegateCommand LoadValuesCommand { get; }

        public IDelegateCommand CellValueChangedCommand { get; }

        public IAsyncCommand AddProductCommand { get; }

        public IDelegateCommand DeleteProductCommand { get; }

        public IAsyncCommand SwitchIgnoreTransitCommand { get; }

        public IAsyncCommand ShowDimensionsCommand { get; }

        public IAsyncCommand CompareProductsCommand { get; }

        public IAsyncCommand AssignToMeCommand { get; }

        public IDelegateCommand ExportToXlsxCommand { get; }

        public IAsyncCommand OpenInvoiceCommand { get; }

        public IAsyncCommand CloseInvoiceCommand { get; }

        public IDelegateCommand AddAdditionalCostCommand { get; }

        public IAsyncCommand DeleteAdditionalCostCommand { get; }

        public IDelegateCommand EditAdditionalCostCommand { get; }

        public IAsyncCommand ArriveInvoiceCommand { get; }

        public IAsyncCommand ReceiveInvoiceCommand { get; }

        public IAsyncCommand DontReceiveInvoiceCommand { get; }

        public IAsyncCommand AnalyzeCommand { get; }

        public IAsyncCommand CancelInvoiceCommand { get; }

        public IDelegateCommand BulkAddProductCommand { get; }

        public IAsyncCommand NotifySupplierInTelegramCommand { get; }

        public IAsyncCommand SetCurrencyRateCommand { get; }

        public IDelegateCommand DelayInvoiceCommand { get; }

        public IDelegateCommand CreateBillCommand { get; }

        public IAsyncCommand PurchaseCommand { get; }

        public IAsyncCommand SetAutoReserveCommand { get; }

        #endregion

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ObservableCollection<InvoiceProductViewItem> SelectedInvoiceProductViewItems
        {
            get { return GetProperty(() => SelectedInvoiceProductViewItems); }
            set { SetProperty(() => SelectedInvoiceProductViewItems, value); }
        }

        public InvoiceAdditionalCostViewItem SelectedAdditionalCost
        {
            get { return GetProperty(() => SelectedAdditionalCost); }
            set { SetProperty(() => SelectedAdditionalCost, value); }
        }

        public string AllocatedQuantityStr
        {
            get { return GetProperty(() => AllocatedQuantityStr); }
            private set { SetProperty(() => AllocatedQuantityStr, value); }
        }

        public InvoiceViewItem Invoice
        {
            get { return GetProperty(() => Invoice); }
            private set { SetProperty(() => Invoice, value, InvoiceChanged); }
        }

        public object Model
        {
            get { return GetProperty(() => Model); }
            set { SetProperty(() => Model, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private set { SetProperty(() => ProductInformation, value); }
        }

        public bool IsLocked
        {
            get { return GetProperty(() => IsLocked); }
            private set { SetProperty(() => IsLocked, value); }
        }

        public EmployeeSimpleDto LockedBy
        {
            get { return GetProperty(() => LockedBy); }
            private set { SetProperty(() => LockedBy, value, () => RaisePropertyChanged(nameof(CompareIsEnabled))); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<InvoiceAdditionalCostSource> AdditionalCostSources
        {
            get { return GetProperty(() => AdditionalCostSources); }
            private set { SetProperty(() => AdditionalCostSources, value); }
        }

        public ObservableCollection<Currency> Currencies { get; } = new(new[] { Currency.Uah, Currency.Usd, Currency.Eur });

        public ObservableCollection<InvoiceAdditionalCostType> AdditionalCostTypes { get; } = new(new[] { InvoiceAdditionalCostType.Amount, InvoiceAdditionalCostType.Quantity, InvoiceAdditionalCostType.Weight });

        public bool CanEditPrice => Invoice != null && !IsLocked &&
                                    ((Invoice.State != InvoiceState.Received && Invoice.State != InvoiceState.Cancelled && IsEditPermitted) ||
                                     (Invoice.State == InvoiceState.Received && WebClient.IsOperationAllowed(BusinessOperation.InvoiceReceivedUpdate)));

        public bool CanEditQuantity => CanAddProducts();

        public ObservableRangeCollection<string> EditorsNames
        {
            get { return GetProperty(() => EditorsNames); }
            private set { SetProperty(() => EditorsNames, value); }
        }

        public TimeSpan? BeforeClosingTime
        {
            get { return GetProperty(() => BeforeClosingTime); }
            private set { SetProperty(() => BeforeClosingTime, value); }
        }

        public bool OkVisible => (Invoice != null && Invoice.State != InvoiceState.Received && Invoice.State != InvoiceState.Cancelled && WebClient.IsOperationAllowed(BusinessOperation.InvoiceUpdate)) || CanEditPrice;

        public bool OpenVisible => Invoice != null && Invoice.State == InvoiceState.Closed && WebClient.IsOperationAllowed(BusinessOperation.InvoiceOpen);

        public bool CloseVisible => Invoice != null && Invoice.State == InvoiceState.Open && WebClient.IsOperationAllowed(BusinessOperation.InvoiceClose);

        public bool ArrivedVisible => Invoice != null && Invoice.State == InvoiceState.Closed && WebClient.IsOperationAllowed(BusinessOperation.InvoiceArrive);

        public bool DelayVisible => Invoice != null && Invoice.State == InvoiceState.Closed;

        public bool CompareIsEnabled => LockedBy == null || LockedBy.Id == WebClient.AuthenticatedEmployee.Id;

        public bool CompareVisible => Invoice != null &&
                                      ((Invoice.State == InvoiceState.Arrived && WebClient.IsOperationAllowed(BusinessOperation.InvoiceSaveComparison)) ||
                                       (Invoice.State == InvoiceState.Received && WebClient.IsOperationAllowed(BusinessOperation.InvoiceReceivedAddProduct)));

        public bool ReceiveVisible => Invoice != null && Invoice.State == InvoiceState.Arrived && WebClient.IsOperationAllowed(BusinessOperation.InvoiceReceive);

        public bool DontReceiveVisible => Invoice != null && Invoice.State == InvoiceState.Arrived && WebClient.IsOperationAllowed(BusinessOperation.InvoiceDontReceive);

        public bool CreateBillVisible => Invoice != null && Invoice.State == InvoiceState.Received && WebClient.IsOperationAllowed(BusinessOperation.SupplierBillCreateByInvoice);

        public bool CancelVisible => Invoice != null && (Invoice.State == InvoiceState.Closed || Invoice.State == InvoiceState.Arrived) && WebClient.IsOperationAllowed(BusinessOperation.InvoiceCancel);

        public bool PurchaseVisible => Invoice != null && (Invoice.State == InvoiceState.Closed) && Invoice.SupplierAutoPurchase == true;

        public bool NotifySupplierInTelegramVisible => Invoice?.SupplierTelegramChatId != null;

        public bool IsEditPermitted => WebClient.IsOperationAllowed(BusinessOperation.InvoiceUpdate);

        #region DialogSettings

        public override int Height => 640;

        public override int MinHeight => 640;

        public override int MinWidth => 1024;

        public override int Width => 1024;

        #endregion

        private IMapper Mapper { get; }

        private IPriceConverterFactory PriceConverterFactory { get; }

        private IPricesClient PricesClient { get; }

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        private IDialogService WizardDialogService => GetService<IDialogService>("CreateSupplierBillWizardDialogService", ServiceSearchMode.LocalOnly);

        private EmployeeDto CurrentUser => WebClient.AuthenticatedEmployee;

        private bool IsChanged => compareLogic?.Compare(GetSaveDto(), originalSaveDto).AreEqual == false;

        public override void OnClose(CancelEventArgs e)
        {
            base.OnClose(e);

            cancellationTokenSource.Cancel();

            Task closeEditTask = WebClient.ExecuteApiRequestAsync(new CloseInvoiceEditing(Invoice.Id));

            closeEditTask.ConfigureAwait(false);

            closeEditTask.ContinueWith(
                (task, _) =>
                {
                    if (task.Exception != null)
                    {
                        Logger.LogError(task.Exception.InnerException, "Failed to close invoice edit");
                        MessageFacadeService.ShowNotificationError("Ошибка при закрытии накладной");
                    }
                },
                TaskContinuationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext());

            foreach (InvoiceProductViewItem invoiceProduct in Invoice.InvoiceProducts)
            {
                invoiceProduct.PropertyChanged -= Invoice.OnProductPropertyChanged;
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (Invoice.State.Id == InvoiceState.Received.Id)
            {
                if (Invoice.InvoiceProducts.Any(x => x.Currency.Id != Currency.UahId && Invoice.CurrencyRates.All(z => z.FromCurrencyId != x.Currency?.Id)))
                {
                    MessageFacadeService.ShowNotificationError("В накладной присутствуют товары в валюте.\n Задайте курс");
                    return;
                }
            }

            if (Invoice.InvoiceProducts.Any(x => x.ErrorImage) && !WebClient.IsOperationAllowed(BusinessOperation.InvoiceSaveWithErrors))
            {
                MessageFacadeService.ShowNotificationError("В таблице с товарами присутствуют ошибки");
                return;
            }

            try
            {
                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateInvoice(GetSaveDto()));

                Messenger.Send(new InvoiceMessage(result.Data, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo($"Накладная №{Invoice.Id.ToString(CultureInfo.InvariantCulture)} успешно сохранена");

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                ShowValidationResultView("Ошибки при сохранении накладной", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                ShowValidationResultView("Ошибки при сохранении накладной", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                Logger.LogError(exception, "Failed to update invoice");
            }
        }

        protected override async Task HandleLoadedAsync()
        {
            int invoiceId = (int)Parameter;

            await WebClient.ExecuteApiRequestAsync(new TryOpenInvoiceForEditing(invoiceId));

            InvoiceDto invoiceDto = await WebClient.ExecuteApiRequestAsync(new QueryInvoice(invoiceId));

            InvoiceViewItem invoiceViewItem = Mapper.Map(invoiceDto, InvoiceViewItem.Create());

            ConversionRate[] invoiceConversionRates = invoiceViewItem.CurrencyRates.Select(x => x.CreateConversionRate()).ToArray();

            Task<InvoiceEditingInfoDto> infoTask = WebClient.ExecuteApiRequestAsync(new QueryInvoiceInfo(invoiceId));
            Task<List<ContractorDto>> getContractorsTask = WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
            Task<List<WarehouseDto>> getWarehousesTask = WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
            Task<List<SupplierWarehouseDto>> getSupplierWarehouseTask = WebClient.ExecuteApiRequestAsync(new QueryContractorsWarehouses(), true);
            Task<List<EmployeeDto>> getEmployeesTask = WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            Task<List<ProductDto>> productsTask = WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(GetQueryProductsCatalogRequest(invoiceViewItem.SupplierId, invoiceViewItem.InvoiceProducts)));
            Task<IPriceConverter> priceConverterTask = PriceConverterFactory.CreateForInvoiceAsync(invoiceViewItem.SupplierId, invoiceConversionRates);

            await Task.WhenAll(getContractorsTask, getWarehousesTask, getSupplierWarehouseTask, getEmployeesTask, productsTask, priceConverterTask, infoTask);

            Employees = getEmployeesTask.Result.Select(x => new ComboBoxItem(x.Id, x.Name, x.Active)).ToReadOnlyObservableCollection();
            contractors = getContractorsTask.Result.ToDictionary(x => x.Id, x => x.Name);
            warehouses = getWarehousesTask.Result.ToDictionary(x => x.Id, x => x.Name);
            supplerWarehouses = getSupplierWarehouseTask.Result.ToDictionary(x => x.Id, x => x.Name);
            employees = Employees.ToDictionary(x => x.Id, x => x.DisplayValue);

            List<ProductDto> products = productsTask.Result;
            InvoiceEditingInfoDto info = infoTask.Result;

            AdditionalCostSources = Dictionaries.GetItems<InvoiceAdditionalCostSource>().ToReadOnlyObservableCollection();

            IPriceConverter priceConverter = priceConverterTask.Result;

            RefreshView(invoiceViewItem);
            ApplyEditingInfo(info, products, priceConverter);

            originalSaveDto = GetSaveDto();

            Title = $"Накладная №{invoiceViewItem.Id.ToString(CultureInfo.InvariantCulture)}";

            // don`t await!!!
            RefreshInvoiceAsync(cancellationTokenSource.Token);

            foreach (InvoiceProductViewItem invoiceProduct in Invoice.InvoiceProducts)
            {
                invoiceProduct.PropertyChanged += Invoice.OnProductPropertyChanged;
            }

            Invoice.AnalyzeWasDone = true;
        }

        private static string GetErrorMessage(int invoiceId)
        {
            return $"Ошибка при сохранении накладной №{invoiceId.ToString(CultureInfo.InvariantCulture)}";
        }

        private static bool CanExport(TableView tableView)
        {
            return tableView?.Grid?.SelectedItems?.Count > 0;
        }

        private static QueryProductByIdsDto GetQueryProductsCatalogRequest(int contractorId, ObservableCollection<InvoiceProductViewItem> products)
        {
            return new QueryProductByIdsDto(products.Select(x => x.ProductId).ToArray(), contractorId, priceQuantity: false);
        }

        private InvoiceSaveDto GetSaveDto()
        {
            IEnumerable<InvoiceProductSaveDto> products = Invoice.InvoiceProducts
                    .Select(x => Mapper.Map<InvoiceProductSaveDto>(x));

            if (Invoice.State != InvoiceState.Received)
            {
                products = products.Where(x => x.EmployeeId == CurrentUser.Id);
            }

            return new InvoiceSaveDto
            {
                Id = Invoice.Id,
                StateId = Invoice.State.Id,
                InvoiceProducts = products.ToList()
            };
        }

        private void LoadValues(InvoiceProductViewItem invoiceProduct)
        {
            ProductInformation.ClearProduct();

            if (invoiceProduct != null && invoiceProduct.ProductId != 0)
            {
                ProductInformation.ProductId = new ProductInfoId(invoiceProduct.ProductId, invoiceProduct.Currency.Id);
            }
        }

        private async Task AddProductAsync()
        {
            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(
                new NomenclatureViewOptions(
                    NomenclatureViewPriceContext.Supplier,
                    Invoice.SupplierId,
                    NomenclatureViewSelectionMode.ByQuantity,
                    true,
                    true),
                this);

            if (!nomenclatureViewModel.IsOk)
            {
                return;
            }

            List<NomenclatureViewItem> nomenclatureItems = nomenclatureViewModel.GetSelectedItems().ToList();

            await ProcessProductsForAddingAsync(nomenclatureItems);
        }

        private void BulkAddProduct()
        {
            InvoiceProductBulkAddViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<InvoiceProductBulkAddViewModel>(
                new InvoiceProductBulkAddParameter(Invoice.SupplierId),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            List<string> warnings = new();

            foreach (InvoiceProductBulkAddViewItem item in viewModel.ValidItemsToAdd)
            {
                InvoiceProductViewItem existedInvoiceProduct = Invoice.InvoiceProducts.FirstOrDefault(x => x.ProductId == item.ProductId);

                if (existedInvoiceProduct != null)
                {
                    if (existedInvoiceProduct.EmployeeId == WebClient.AuthenticatedEmployee.Id)
                    {
                        existedInvoiceProduct.Quantity = item.Quantity;

                        if (item.Price is not null)
                        {
                            existedInvoiceProduct.Price = item.Price.Value;
                            existedInvoiceProduct.Currency = Currency.GetById(viewModel.CurrencyId!.Value);
                        }
                    }
                    else
                    {
                        warnings.Add($"Товар '{existedInvoiceProduct.ProductName}' обновлен не будет. Его создал {existedInvoiceProduct.Employee.Name}");
                    }

                    continue;
                }

                InvoiceProductViewItem invoiceProductViewItem = new InvoiceProductViewItem
                {
                    Id = 0,
                    InvoiceId = Invoice.Id,
                    Employee = new EmployeeSimpleDto
                    {
                        Id = CurrentUser.Id,
                        Name = CurrentUser.Name,
                        Login = CurrentUser.Login,
                        ShortName = CurrentUser.ShortName
                    },
                    EmployeeId = CurrentUser.Id,
                    Quantity = item.Quantity,
                    OrderQuantity = 0,
                    ProductId = item.ProductId!.Value,
                    SupplierProductId = item.SupplierProductId,
                    ProductName = item.ProductName,
                    IsEditableForCurrentUser = true,
                    ProductPn = item.Pn
                };

                invoiceProductViewItem.Price = item.Price!.Value;
                invoiceProductViewItem.Currency = Currency.GetById(viewModel.CurrencyId!.Value);

                AddInvoiceProducts(invoiceProductViewItem);
            }

            if (warnings.Any())
            {
                MessageFacadeService.ShowValidationResultView("Ошибки при добавлении товаров", warnings.Select(x => new ValidationResultItem(x, false)), this);
            }

            if (viewModel.ValidItemsToAdd.Count > 0)
            {
                Sort();

                if (warnings.Any() && warnings.Count < viewModel.ValidItemsToAdd.Count)
                {
                    MessageFacadeService.ShowNotificationInfo("Товары добавлены с предупреждениями");
                }
                else if (warnings.Count != viewModel.ValidItemsToAdd.Count)
                {
                    MessageFacadeService.ShowNotificationInfo("Товары успешно добавлены");
                }
            }

            RefreshSummaryItems();
        }

        private async Task ProcessProductsForAddingAsync(IReadOnlyCollection<NomenclatureViewItem> nomenclatureItems)
        {
            List<string> skippedProductNames = null;

            QuerySupplierProductsDto requestDto = new QuerySupplierProductsDto(nomenclatureItems.Select(x => x.Id).ToArray(), new[] { Invoice.SupplierId });

            IReadOnlyCollection<SupplierProductDto> supplierProducts = await WebClient.ExecuteApiRequestAsync(new QuerySupplierProducts(requestDto));

            foreach (NomenclatureViewItem item in nomenclatureItems)
            {
                // skip if that product already exists in invoice or orderProduct
                if (Invoice.InvoiceProducts.Any(x => x.ProductId == item.Id))
                {
                    skippedProductNames ??= new List<string>();

                    skippedProductNames.Add(item.Name);

                    continue;
                }

                InvoiceProductViewItem invoiceProductViewItem = new InvoiceProductViewItem
                {
                    Id = 0,
                    InvoiceId = Invoice.Id,
                    Price = item.Price,
                    Currency = Currency.GetById(item.CurrencyId),
                    Employee = new EmployeeSimpleDto
                    {
                        Id = CurrentUser.Id,
                        Name = CurrentUser.Name,
                        Login = CurrentUser.Login,
                        ShortName = CurrentUser.ShortName
                    },
                    EmployeeId = CurrentUser.Id,
                    Quantity = item.Quantity,
                    OrderQuantity = 0,
                    ProductId = item.Id,
                    SupplierProductId = supplierProducts.FirstOrDefault(x => x.ProductId == item.Id)?.SupplierProductId,
                    ProductName = item.Name,
                    IsEditableForCurrentUser = true,
                    ProductPn = item.ProductPn
                };

                AddInvoiceProducts(invoiceProductViewItem);
            }

            if (nomenclatureItems.Count > 0)
            {
                Sort();
            }

            if (skippedProductNames != null)
            {
                MessageFacadeService.ShowNotificationWarning($"Такие товары уже есть в накладной: {string.Join(Environment.NewLine, skippedProductNames)}");
            }

            RefreshSummaryItems();
        }

        private void DeleteProducts()
        {
            for (int i = SelectedInvoiceProductViewItems.Count - 1; i >= 0; i--)
            {
                InvoiceProductViewItem invoiceProductToDelete = SelectedInvoiceProductViewItems[i];

                invoiceProductToDelete.PropertyChanged -= Invoice.OnProductPropertyChanged;

                Invoice.InvoiceProducts.Remove(invoiceProductToDelete);
            }

            RefreshSummaryItems();
        }

        private void Sort()
        {
            IOrderedEnumerable<InvoiceProductViewItem> orderedInvoceViewItems = Invoice.InvoiceProducts
                .OrderBy(x => !string.Equals(x.Employee.ShortName, WebClient.AuthenticatedEmployee.ShortName, StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => x.Employee.ShortName)
                .ThenBy(x => x.ProductName);

            Invoice.InvoiceProducts = new ObservableRangeCollection<InvoiceProductViewItem>(orderedInvoceViewItems);
        }

        private bool CanDeleteProducts()
        {
            return CanAddProducts()
                && SelectedInvoiceProductViewItems.Any()
                && SelectedInvoiceProductViewItems.All(x => x.Employee.Id == CurrentUser.Id && x.OrderQuantity == 0);
        }

        private bool CanAddProducts()
        {
            return Invoice?.State == InvoiceState.Open && !IsLocked && IsEditPermitted;
        }

        private async Task OpenInvoiceAsync()
        {
            if (IsChanged)
            {
                MessageFacadeService.ShowNotificationWarning("Сначала сохраните изменения");
                return;
            }

            InvoiceOpenViewModel viewModel = DialogDocumentManagerService.ShowView<InvoiceOpenViewModel>(null, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                DateTime dateClose = DateTime.Today.Add(viewModel.DateClose!.Value.TimeOfDay);
                OpenInvoice gatewayRequest = new OpenInvoice(Invoice.Id, dateClose, viewModel.EmployeeRequestId!.Value);

                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                InvoiceViewItem invoiceViewItem = Mapper.Map<InvoiceViewItem>(result.Data);

                RefreshView(invoiceViewItem);

                Messenger.Send(new InvoiceMessage(result.Data, MessageType.Changed));

                MessageFacadeService.ShowNotificationInfo($"Накладная №{Invoice.Id.ToString(CultureInfo.InvariantCulture)} успешно открыта");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                ShowValidationResultView("Ошибки при сохранении накладной", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                ShowValidationResultView("Ошибки при сохранении накладной", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                Logger.LogError(exception, "Failed to open invoice");
            }
        }

        private async Task CloseInvoiceAsync()
        {
            if (IsChanged)
            {
                MessageFacadeService.ShowNotificationWarning("Сначала сохраните изменения");
                return;
            }

            int orderQuantityDeficit = Invoice.InvoiceProducts.Sum(x => Math.Max(0, x.OrderQuantity - x.Quantity));

            if (Invoice.InvoiceProducts.Any(x => x.Quantity < x.OrderQuantity))
            {
                if (!DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>(
                "Под закупку проставлено меньше товаров, чем требуется для заказов. " +
                "Отмените действие и закупите недостающие товары иначе источники для товаров, " +
                $"которым не хватит количества, будут очищены({orderQuantityDeficit} шт). Удалить источники в заказах?",
                this).IsOk)
                {
                    return;
                }
            }
            else if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new CloseInvoice(Invoice.Id));
                InvoiceViewItem invoiceViewItem = Mapper.Map<InvoiceViewItem>(result.Data);
                RefreshView(invoiceViewItem);
                Messenger.Send(new InvoiceMessage(result.Data, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo($"Накладная №{Invoice.Id.ToString(CultureInfo.InvariantCulture)} успешно закрыта");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                ShowValidationResultView("Ошибки при сохранении накладной", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                ShowValidationResultView("Ошибки при сохранении накладной", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                Logger.LogError(exception, "Failed to close invoice");
            }
        }

        private async Task PurchaseInvoiceAsync()
        {
            string confirmText;

            if (Invoice.InvoiceProducts.Any(x => Math.Max(x.OrderQuantity, x.Quantity) > x.QuantityReserved))
            {
                confirmText = "Недостаточно товаров в резерве. Продолжить?";
            }
            else
            {
                confirmText = "Вы уверены?";
            }

            if (!MessageFacadeService.Confirm(confirmText))
            {
                return;
            }

            (await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new PurchaseInvoice(Invoice.Id)), "закупке накладной", "Накладная закуплена", this, true))
                .IfNotNull(x =>
                {
                    InvoiceViewItem invoiceViewItem = Mapper.Map<InvoiceViewItem>(x.Data);
                    RefreshView(invoiceViewItem);
                    Messenger.Send(new InvoiceMessage(x.Data, MessageType.Changed));
                });
        }

        private async Task AnalyzeAsync(bool fromCommand)
        {
            if (Invoice.InvoiceProducts.Any(x => x.Currency?.Id != Currency.UahId && Invoice.CurrencyRates.All(z => z.FromCurrencyId != x.Currency?.Id)))
            {
                if (fromCommand)
                {
                    MessageFacadeService.ShowNotificationError("В накладной присутствуют товары в валюте.\n Задайте курс");
                }

                return;
            }

            Invoice.InvoiceProducts.ForEach(x =>
            {
                x.PriceGreenColor = false;
                x.PriceRedColor = false;
            });

            ConversionRate[] invoiceConversionRates = Invoice.CurrencyRates.Select(x => x.CreateConversionRate()).ToArray();

            int[] productIds = Invoice.InvoiceProducts.Select(x => x.ProductId).Distinct().ToArray();

            Task<IPriceConverter> priceConverterTask = PriceConverterFactory.CreateForInvoiceAsync(Invoice.SupplierId, invoiceConversionRates);

            Task<Dictionary<int, ParserContractorPriceDto[]>> productContractorPricesDictionaryTask = WebClient.ExecuteApiRequestAsync(
                new QueryAllContractorPrices(
                    new ParserContractorPriceSearchDto(null, productIds)));

            await Task.WhenAll(priceConverterTask, productContractorPricesDictionaryTask);

            List<string> warnings = new();

            CalculateExtraChargeDto calculateExtraChargeDto = new(
                productContractorPricesDictionaryTask.Result.SelectMany(
                    x => x.Value.Select(
                        z => new CalculateExtraChargeProductDto(
                            x.Key,
                            priceConverterTask.Result.Convert(z.Price, z.CurrencyId, Currency.UahId, CurrencyTypeIds.UsdMinusId, false),
                            Invoice.InvoiceProducts.FirstOrDefault(p => p.ProductId == x.Key)?.PriceTelemartUah ?? 0,
                            z.ContractorId))).ToArray());

            Result<IReadOnlyCollection<ContractorProductExtraChargeDto>> calculateExtraChargeResult = await PricesClient.CalculateExtraChargeAsync(calculateExtraChargeDto, this, CancellationToken.None);

            foreach (InvoiceProductViewItem invoiceProduct in Invoice.InvoiceProducts)
            {
                if (productContractorPricesDictionaryTask.Result.TryGetValue(invoiceProduct.ProductId, out ParserContractorPriceDto[] prices))
                {
                    decimal? extraChargeToCheck;

                    if (invoiceProduct.Price > 0 && invoiceProduct.PriceTelemartUah > 0 && invoiceProduct.ExtraCharge.HasValue)
                    {
                        extraChargeToCheck = invoiceProduct.ExtraCharge.Value;
                    }
                    else
                    {
                        ParserContractorPriceDto currentSupplier = prices.FirstOrDefault(x => x.ContractorId == Invoice.SupplierId);

                        if (currentSupplier is null)
                        {
                            warnings.Add($"По товару '{invoiceProduct.ProductName}' не удалось найти цену у текущего поставщика для рассчета рентабельности");
                            continue;
                        }

                        if (invoiceProduct.PriceTelemartUah is null or 0)
                        {
                            warnings.Add($"По товару '{invoiceProduct.ProductName}' нет цены Телемарт1");
                            continue;
                        }

                        extraChargeToCheck = calculateExtraChargeResult?.Data?.FirstOrDefault(x => x.ProductId == invoiceProduct.ProductId && x.ContractorId == currentSupplier.ContractorId)?.ExtraCharge;
                    }

                    if (extraChargeToCheck is null)
                    {
                        warnings.Add($"По товару '{invoiceProduct.ProductName}' не удалось рассчитать рентабельность для проверки");
                        continue;
                    }

                    decimal? maxExtraChargeFromAnotherSuppliers = prices
                        .Where(x => x.ContractorId != Invoice.SupplierId)
                        .Max(x => calculateExtraChargeResult?.Data.FirstOrDefault(z => z.ProductId == invoiceProduct.ProductId && z.ContractorId == x.ContractorId)?.ExtraCharge);

                    if (extraChargeToCheck >= maxExtraChargeFromAnotherSuppliers || maxExtraChargeFromAnotherSuppliers is null)
                    {
                        invoiceProduct.PriceGreenColor = true;
                    }
                    else
                    {
                        invoiceProduct.PriceRedColor = true;
                    }
                }
                else
                {
                    warnings.Add($"По товару '{invoiceProduct.ProductName}' отсутствуют цены поставщиков");
                }
            }

            foreach (InvoiceProductViewItem invoiceProduct in Invoice.InvoiceProducts)
            {
                invoiceProduct.ImageToolTip = null;
                invoiceProduct.ErrorImage = false;
            }

            InvoiceAnalyzeDto dto = new InvoiceAnalyzeDto(Invoice.Id, Invoice.InvoiceProducts
                .Where(x => x.Quantity > 0)
                .Select(x => new InvoiceAnalyzeProductDto(
                    x.ProductId,
                    x.Quantity,
                    x.OrderQuantity,
                    priceConverterTask.Result.Convert(x.Price, x.Currency.Id, Currency.UahId, CurrencyTypeIds.UsdMinusId, false))).ToArray());

            Result<InvoiceAnalyzeResultDto> result = await WebClient.ExecuteApiRequestAsync(new AnalyzeInvoice(Invoice.Id, dto));

            List<ValidationResultItem> analyzeValidationResults = new List<ValidationResultItem>();

            foreach (InvoiceAnalyzeResultProductDto productData in result.Data.Products.OrderBy(x => x.SegmentName))
            {
                InvoiceProductViewItem productViewItem = Invoice.InvoiceProducts.First(x => x.ProductId == productData.ProductId);

                productViewItem.ImageToolTip = string.Join("\r\n", productData.ResultItems.Select(x => x.Text));
                productViewItem.ErrorImage = productData.ResultItems.Any(x => x.IsError);

                foreach (InvoiceAnalyzeResultItemDto productResultItem in productData.ResultItems)
                {
                    analyzeValidationResults.Add(new ValidationResultItem($"{productViewItem.ProductName} ({productData.SegmentName}) {productResultItem.Text}", productResultItem.IsError));
                }
            }

            if (fromCommand && warnings.Any() && WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin))
            {
                MessageFacadeService.ShowValidationResultView("Ошибки при расчете рентабельности", warnings.Select(x => new ValidationResultItem(x, false)).ToArray(), this);
            }

            if (fromCommand && analyzeValidationResults.Any())
            {
                MessageFacadeService.ShowValidationResultView("Результат анализа", analyzeValidationResults, this);
            }

            Invoice.AnalyzeWasDone = true;
        }

        private async Task DeleteAdditionalCostAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите удалить доп. расход?"))
            {
                return;
            }

            (await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new DeleteInvoiceAdditionalCost(SelectedAdditionalCost.Id)), "удалении доп. расхода", "Доп. расход удален", this, true))
                .IfNotNull(_ => Invoice.AdditionalCosts.Remove(SelectedAdditionalCost));
        }

        private void AddAdditionalCost()
        {
            DialogDocumentManagerService.ShowView<AdditionalCostViewModel>(new AdditionalCostParameter(0, Invoice.Id), this);
        }

        private void EditAdditionalCost()
        {
            DialogDocumentManagerService.ShowView<AdditionalCostViewModel>(new AdditionalCostParameter(SelectedAdditionalCost.Id, Invoice.Id), this);
        }

        private async Task ArriveInvoiceAsync()
        {
            if (IsChanged)
            {
                MessageFacadeService.ShowNotificationWarning("Сначала сохраните изменения");
                return;
            }

            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                ArriveInvoice gatewayRequest = new ArriveInvoice(Invoice.Id);
                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);
                InvoiceViewItem invoiceViewItem = Mapper.Map<InvoiceViewItem>(result.Data);
                RefreshView(invoiceViewItem);
                Messenger.Send(new InvoiceMessage(result.Data, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo($"Накладная №{Invoice.Id.ToString(CultureInfo.InvariantCulture)} успешно приехала");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                ShowValidationResultView("Ошибки при сохранении накладной", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                ShowValidationResultView("Ошибки при сохранении накладной", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                Logger.LogError(exception, "Failed to arrive invoice");
            }
        }

        private async Task ReceiveInvoiceAsync()
        {
            if (IsChanged)
            {
                MessageFacadeService.ShowNotificationWarning("Сначала сохраните изменения");
                return;
            }

            List<ProductComparisonResult> items = Invoice.InvoiceProducts
                .Where(x => x.Quantity != x.QuantityReal)
                .Select(x => new ProductComparisonResult(x.GetLocalName(LocalizableNameType.Ukr), x.Quantity, x.QuantityReal ?? 0))
                .Where(x => x.DeviationQuantity != 0)
                .OrderBy(x => x.ProductName)
                .ToList();

            if (items.Any())
            {
                ProductComparisonResultViewModel comparisonResultViewModel = DialogDocumentManagerService.ShowView<ProductComparisonResultViewModel>(
                    new object[] { items, true },
                    this);

                if (!comparisonResultViewModel.IsOk)
                {
                    return;
                }
            }
            else if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            LockResponse<InvoiceDto> lockResponse = await TryLockAndNotifyAsync();

            if (lockResponse == null)
            {
                return;
            }

            EditorsNames.Clear();
            RaisePropertyChanged(() => EditorsNames);
            RefreshView(Mapper.Map(lockResponse.Dto, InvoiceViewItem.Create()));

            if (!lockResponse.Success)
            {
                return;
            }

            try
            {
                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new ReceiveInvoice(Invoice.Id));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Накладная №{Invoice.Id.ToString(CultureInfo.InvariantCulture)} принята с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Накладная №{Invoice.Id.ToString(CultureInfo.InvariantCulture)} успешно принята");
                }

                RefreshView(Mapper.Map<InvoiceViewItem>(result.Data));

                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                ShowValidationResultView("Ошибки при сохранении накладной", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                ShowValidationResultView("Ошибки при сохранении накладной", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(Invoice.Id));
                Logger.LogError(exception, "Failed to receive invoice");
            }

            LockResponse<InvoiceDto> unlockResponse = await UnlockInvoiceAsync(Invoice.Id);

            if (unlockResponse?.Success == true)
            {
                RefreshView(Mapper.Map(unlockResponse.Dto, InvoiceViewItem.Create()));
                Messenger.Send(new InvoiceMessage(unlockResponse.Dto, MessageType.Changed));
            }
        }

        private async Task DontReceiveInvoiceAsync()
        {
            if (IsChanged)
            {
                MessageFacadeService.ShowNotificationWarning("Сначала сохраните изменения");
                return;
            }

            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            (await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new DontReceiveInvoice(Invoice.Id)),
                    "закрытии накладной",
                    "Накладная закрыта",
                    this,
                    true))
                .IfNotNull(x =>
                {
                    RefreshView(Mapper.Map<InvoiceViewItem>(x.Data));

                    Close();
                });
        }

        private async Task NotifySupplierInTelegramAsync()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.ContractorSendMessageToTelegramChat))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            const string ProductIdColumn = "Код товара";
            const string SupplierProductIdColumn = "Код товара поставщика";
            const string PnColumn = "Артикул";
            const string ProductNameColumn = "Название";
            const string QuantityColumn = "Количество";

            try
            {
                StringBuilder caption = new();

                caption.AppendLine($"Дата получения: {Invoice.DateArrive}");
                caption.AppendLine($"Склад отправитель: {Invoice.SupplierWarehouseName}");
                caption.AppendLine($"Склад получатель: {Invoice.WarehouseCityName}, {Invoice.WarehouseAddress}");
                caption.AppendLine($"Способ доставки: {Invoice.CarryType.Name}");

                if (Invoice.SupplierAllowDocuments)
                {
                    caption.AppendLine("Договор: безналичный расчет");
                }

                IXlExporter exporter = XlExport.CreateExporter(XlDocumentFormat.Xlsx);

                await using MemoryStream stream = new();

                using (IXlDocument document = exporter.CreateDocument(stream))
                {
                    XlCellFormatting cellFormatting = new()
                    {
                        Font = new XlFont { Name = "Times New Roman", SchemeStyle = XlFontSchemeStyles.None }
                    };

                    XlCellFormatting headerRowFormatting = new();

                    headerRowFormatting.CopyFrom(cellFormatting);
                    headerRowFormatting.Font.Bold = true;

                    using (IXlSheet sheet = document.CreateSheet())
                    {
                        string[] headers = { ProductIdColumn, SupplierProductIdColumn, ProductNameColumn, PnColumn, QuantityColumn };

                        sheet.Name = "Товары";

                        foreach (string header in headers)
                        {
                            using IXlColumn column = sheet.CreateColumn();

                            switch (header)
                            {
                                case ProductNameColumn:
                                    column.WidthInCharacters = 80;
                                    break;
                                case SupplierProductIdColumn:
                                case ProductIdColumn:
                                case PnColumn:
                                    column.WidthInCharacters = 40;
                                    break;
                                case QuantityColumn:
                                    column.WidthInCharacters = 20;
                                    break;
                            }
                        }

                        using (IXlRow row = sheet.CreateRow())
                        {
                            foreach (string header in headers)
                            {
                                using IXlCell cell = row.CreateCell();

                                cell.Value = header;
                                cell.ApplyFormatting(headerRowFormatting);
                            }
                        }

                        foreach (InvoiceProductViewItem product in Invoice.InvoiceProducts)
                        {
                            using IXlRow row = sheet.CreateRow();

                            foreach (string header in headers)
                            {
                                using IXlCell cell = row.CreateCell();

                                cell.Value = GetValueByHeader(header, product);
                                cell.ApplyFormatting(cellFormatting);
                            }
                        }
                    }
                }

                TelegramBotSendDocumentDto dto = new(stream.ToArray(), "products.xlsx", caption.ToString(), false, chatId: Invoice.SupplierTelegramChatId);

                await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteTelegramApiRequestAsync(new ContractorBotSendDocument(dto)), "отправке сообщения", "Сообщение успешно отправлено", this, true);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while sending message to contractor");
                MessageFacadeService.ShowNotificationError("Непредвиденная ошибка");
            }

            static string GetValueByHeader(string header, InvoiceProductViewItem product)
            {
                string value = header switch
                {
                    ProductNameColumn => product.ProductName,
                    PnColumn => product.ProductPn,
                    QuantityColumn => Math.Max(product.Quantity, product.OrderQuantity).ToString(),
                    SupplierProductIdColumn => product.SupplierProductId,
                    ProductIdColumn => product.ProductId.ToString(),
                    _ => string.Empty
                };

                return value;
            }
        }

        private async Task CancelInvoiceAsync()
        {
            if (IsChanged)
            {
                MessageFacadeService.ShowNotificationWarning("Сначала сохраните изменения");
                return;
            }

            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            LockResponse<InvoiceDto> lockResponse = await TryLockAndNotifyAsync();

            if (lockResponse == null)
            {
                return;
            }

            EditorsNames.Clear();
            RaisePropertyChanged(() => EditorsNames);
            RefreshView(Mapper.Map(lockResponse.Dto, InvoiceViewItem.Create()));

            if (!lockResponse.Success)
            {
                return;
            }

            try
            {
                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new CancelInvoice(Invoice.Id));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Накладная №{Invoice.Id.ToString(CultureInfo.InvariantCulture)} отменена с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Накладная №{Invoice.Id.ToString(CultureInfo.InvariantCulture)} успешно отменена");
                }

                RefreshView(Mapper.Map<InvoiceViewItem>(result.Data));

                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError($"Ошибка при отмене накладной №{Invoice.Id}");
                ShowValidationResultView("Ошибки при отмене накладной", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError($"Ошибка при отмене накладной №{Invoice.Id}");
                ShowValidationResultView("Ошибки при отмене накладной", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError($"Ошибка при отмене накладной №{Invoice.Id}");
                Logger.LogError(exception, "Failed to cancel invoice");
            }

            LockResponse<InvoiceDto> unlockResponse = await UnlockInvoiceAsync(Invoice.Id);

            if (unlockResponse?.Success == true)
            {
                RefreshView(Mapper.Map(unlockResponse.Dto, InvoiceViewItem.Create()));
                Messenger.Send(new InvoiceMessage(unlockResponse.Dto, MessageType.Changed));
            }
        }

        private void CellValueChanged(CellValueChangedEventArgs args)
        {
            if (args.Column.FieldName == nameof(InvoiceProductViewItem.Currency))
            {
                if (args.Row is InvoiceProductViewItem product
                    && args.OldValue is Currency oldCurrency
                    && Invoice?.State.Id == InvoiceState.Received.Id
                    && Invoice.CurrencyRates.Any(x => x.FromCurrencyId == product.Currency?.Id)
                    && product.BillsQuantity > 0
                    && product.Currency is not null
                    && product.PriceConverter is not null)
                {
                    product.Price = product.PriceConverter.Convert(product.Price, oldCurrency.Id, product.Currency.Id, CurrencyTypeIds.UsdMinusId);
                }
            }
        }

        private void RefreshView(InvoiceViewItem invoiceViewItem)
        {
            SetInvoice(invoiceViewItem);
            RaisePropertiesChanged(GetPropertyNamesDependentOnState().ToArray());
            RefreshSummaryItems();
        }

        private void CreateBill()
        {
            CreateSupplierBillModel model = new CreateSupplierBillModel
            {
                ContractorId = Invoice.SupplierId,
                InvoiceId = Invoice.Id
            };

            WizardDialogViewModel<CreateSupplierBillModel> wizardDialogViewModel = new WizardDialogViewModel<CreateSupplierBillModel>(
                typeof(CreateSupplierBillContractorPageViewModel),
                model,
                this);

            WizardDialogService.ShowDialog(MessageButton.OKCancel, "Создание счета поставщика", wizardDialogViewModel);
        }

        private async Task<LockResponse<InvoiceDto>> TryLockAndNotifyAsync()
        {
            LockResponse<InvoiceDto> lockResponse = null;

            try
            {
                lockResponse = await WebClient.ExecuteApiRequestAsync(new LockInvoice(Invoice.Id));

                if (!lockResponse.Success)
                {
                    MessageFacadeService.ShowNotificationWarning($"Накладная уже заблокирована пользователем: {lockResponse.Dto?.EmployeeLock.Name}");
                }
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

        private async Task CompareProductsAsync()
        {
            try
            {
                LockResponse<InvoiceDto> lockResponse = await TryLockAndNotifyAsync();

                if (lockResponse == null)
                {
                    return;
                }

                EditorsNames.Clear();
                RaisePropertyChanged(() => EditorsNames);
                RefreshView(Mapper.Map(lockResponse.Dto, InvoiceViewItem.Create()));

                if (!lockResponse.Success)
                {
                    return;
                }

                ProductsComparisonViewModel productsComparisonViewModel = SizeableDialogDocumentManagerService.ShowView<ProductsComparisonViewModel>(
                    Mapper.Map<InvoiceViewItem>(Invoice),
                    this);

                if (productsComparisonViewModel.IsOk)
                {
                    Invoice = Mapper.Map<InvoiceViewItem>(productsComparisonViewModel.InvoiceFromServer);
                    MessageFacadeService.ShowNotificationInfo("Данные сверки успешно сохранены");
                }

                LockResponse<InvoiceDto> unlockResponse = await UnlockInvoiceAsync(Invoice.Id);

                if (unlockResponse?.Success == true)
                {
                    RefreshView(Mapper.Map(unlockResponse.Dto, InvoiceViewItem.Create()));
                    Messenger.Send(new InvoiceMessage(unlockResponse.Dto, MessageType.Changed));
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "error while opening invoice for comparison");
                MessageFacadeService.ShowNotificationError("Ошибка при открытии сверки накладной");
            }
        }

        private async Task ShowDimensionsAsync()
        {
            int[] invoiceProductIds = Invoice.InvoiceProducts
                .Where(x => x.QuantityReal > 0)
                .Select(x => x.ProductId)
                .ToArray();

            List<ProductAttributesDto> products = await WebClient.ExecuteApiRequestAsync(new QueryInvoiceProductAttributes(Invoice.Id));

            products = products
                .Where(x => !x.DimmensionsIsValid()
                    && invoiceProductIds.Contains(x.ProductId)).ToList();

            if (!products.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Всем товарам уже заполнены ВГХ");
            }
            else
            {
                SizeableDialogDocumentManagerService.ShowView<InvoiceProductsDimensionsViewModel>(products, this);
            }
        }

        private async Task SetCurrencyRateAsync()
        {
            InvoiceCurrencyRatesParameter parameter = new InvoiceCurrencyRatesParameter(Invoice.Id, Invoice.CurrencyRates);

            InvoiceCurrencyRatesViewModel viewModel = DialogDocumentManagerService.ShowView<InvoiceCurrencyRatesViewModel>(parameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                InvoiceCurrencyRateDto[] saveDtos = viewModel.CurrencyRates.Where(x => x.ConversionRate.HasValue).Select(x => Mapper.Map<InvoiceCurrencyRateDto>(x)).ToArray();

                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new SetInvoiceCurrencyRate(Invoice.Id, saveDtos));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Курс присвоен с предупреждениями");
                    ShowValidationResultView("Предупреждения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Курс успешно присвоен");
                }

                Invoice.CurrencyRates = result.Data.CurrencyRates.ToObservableCollection();

                ConversionRate[] conversionRates = Invoice.CurrencyRates.Select(x => x.CreateConversionRate()).ToArray();

                IPriceConverter newPriceConverter = await PriceConverterFactory.CreateForInvoiceAsync(Invoice.SupplierId, conversionRates);

                Invoice.InvoiceProducts.ForEach(x => x.PriceConverter = newPriceConverter.Copy());

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

        private void Delay()
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
                Invoice.DateArrive,
                nowIsMinTime: true);

            GetDateTimeFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetDateTimeFromUserViewModel>(fromUserParameter, this);

            if (fromUserViewModel.IsOk)
            {
                InvoiceDelayViewModel delayViewModel = DialogDocumentManagerService.ShowView<InvoiceDelayViewModel>(new InvoiceDelayParameter(Invoice.Id, fromUserViewModel.DateTime!.Value, Invoice.WarehouseId), this);

                if (delayViewModel.IsOk)
                {
                    RefreshView(Mapper.Map(delayViewModel.Result, InvoiceViewItem.Create()));
                }
            }
        }

        private bool CanAssign()
        {
            return Invoice != null &&
                Invoice.State != InvoiceState.Closed &&
                SelectedInvoiceProductViewItems?.Count > 0 &&
                SelectedInvoiceProductViewItems.All(invoiceProduct => invoiceProduct.Employee?.Id != CurrentUser?.Id) &&
                CanEditPrice;
        }

        private async Task AssignSelectedInvoicesToCurrentUserAsync()
        {
            if (SelectedInvoiceProductViewItems == null ||
                !SelectedInvoiceProductViewItems.Any() ||
                !MessageFacadeService.Confirm("Вы будете закупать этот товар?"))
            {
                return;
            }

            List<int> invoiceProductsIds = SelectedInvoiceProductViewItems
                .Where(x => x.Employee.Id != CurrentUser.Id)
                .Select(x => x.Id)
                .ToList();

            InvoiceDto invoiceFromServer = await WebClient.ExecuteApiRequestAsync(new AssignInvoiceProductsToCurrentUser(Invoice.Id, invoiceProductsIds));

            List<InvoiceProductDto> notUpdatedInvoices = invoiceFromServer.InvoiceProducts.Where(x => invoiceProductsIds.Contains(x.Id) && x.EmployeeId != CurrentUser.Id).ToList();

            if (notUpdatedInvoices.Count > 0)
            {
                ShowNotUpdatedInvoicesWarning(notUpdatedInvoices);
                SelectedInvoiceProductViewItems.RemoveAll(x => notUpdatedInvoices.All(y => y.Id != x.Id));
            }

            InvoiceProductViewItem[] addedInvoiceProducts = Invoice.InvoiceProducts.Where(x => x.Id == 0).ToArray(); // will be removed by mapper
            Invoice = Mapper.Map(invoiceFromServer, InvoiceViewItem.Create());
            AddInvoiceProducts(addedInvoiceProducts);

            SetIsEditableForCurrentUser(Invoice);
            Sort();

            SelectedInvoiceProductViewItems = new ObservableCollection<InvoiceProductViewItem>(Invoice.InvoiceProducts.Where(x => notUpdatedInvoices.Any(y => y.Id == x.Id)));
        }

        private void ShowNotUpdatedInvoicesWarning(IReadOnlyCollection<InvoiceProductDto> notUpdatedInvoices)
        {
            StringBuilder warningText = new StringBuilder($"Не удалось изменить ответственного: {Environment.NewLine}");

            notUpdatedInvoices.Take(2).ForEach(x => warningText.Append($"{x.ProductName}{Environment.NewLine}"));

            if (notUpdatedInvoices.Count > 2)
            {
                warningText.Append("...");
            }

            MessageFacadeService.ShowNotificationWarning(warningText.ToString());
        }

        private async Task SwitchIgnoreTransitAsync()
        {
            (await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new SwitchIgnoreTransitInvoice(Invoice.Id)), "изменении учета в роботе", "Учет изменен", this, true))
                .IfNotNull(x =>
                {
                    Invoice.IgnoreTransit = x.Data.IgnoreTransit;
                    RefreshSummaryItems();
                });
        }

        private async Task SetAutoReserveAsync()
        {
            if (Invoice.SupplierAutoReserve.HasValue && MessageFacadeService.Confirm("Вы уверены что хотите изменить авто-резерв?"))
            {
                (await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new SetInvoiceAutoReserve(Invoice.Id, !Invoice.SupplierAutoReserve.Value)), "изменении флага авто-резерв", "Авто-резерв изменен", this, true))
                    .IfNotNull(x =>
                    {
                        Invoice.SupplierAutoReserve = x.Data.SupplierAutoReserve;
                        RefreshSummaryItems();
                    });
            }
        }

        private async Task RefreshInvoiceAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(RefreshIntervalSeconds), cancellationToken);

                if (Invoice == null ||
                    OkCommand.IsExecuting ||
                    OpenInvoiceCommand.IsExecuting ||
                    CloseInvoiceCommand.IsExecuting ||
                    ArriveInvoiceCommand.IsExecuting ||
                    ReceiveInvoiceCommand.IsExecuting)
                {
                    continue;
                }

                try
                {
                    InvoiceEditingInfoDto result = await WebClient.ExecuteApiRequestAsync(new QueryInvoiceInfo(Invoice.Id));

                    List<ProductDto> products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(GetQueryProductsCatalogRequest(Invoice.SupplierId, Invoice.InvoiceProducts)));

                    ConversionRate[] invoiceConversionRates = Invoice.CurrencyRates
                        .Select(x => x.CreateConversionRate())
                        .ToArray();

                    IPriceConverter priceConverter = await PriceConverterFactory.CreateForInvoiceAsync(Invoice.SupplierId, invoiceConversionRates);

                    ApplyEditingInfo(result, products, priceConverter);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to refresh invoice");
                }

                RefreshSummaryItems();

                if (AnalyzeCommand.IsExecuting || Invoice.InvoiceProducts?.Any() != true)
                {
                    continue;
                }

                try
                {
                    await AnalyzeAsync(false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to analyze invoice");
                }
            }
        }

        private void SetIsEditableForCurrentUser(InvoiceViewItem invoiceViewItem)
        {
            foreach (InvoiceProductViewItem invoiceProductViewItem in invoiceViewItem.InvoiceProducts)
            {
                invoiceProductViewItem.IsEditableForCurrentUser = (CurrentUser.Id == invoiceProductViewItem.EmployeeId || Invoice.State == InvoiceState.Received)
                                                                  && !IsLocked
                                                                  && invoiceProductViewItem.BillsQuantity == 0;

                invoiceProductViewItem.CanEditCurrency = (CurrentUser.Id == invoiceProductViewItem.EmployeeId || Invoice.State == InvoiceState.Received)
                                                         && CanEditPrice
                                                         && !IsLocked
                                                         && (Invoice.CurrencyRates?.Any() == true || invoiceProductViewItem.BillsQuantity == 0);
            }
        }

        private void OnInvoiceAdditionalCostMessage(EntityMessage<InvoiceAdditionalCostDto> message)
        {
            Invoice.AdditionalCosts ??= new ObservableCollection<InvoiceAdditionalCostViewItem>();

            switch (message.MessageType)
            {
                case MessageType.Added:

                    InvoiceAdditionalCostViewItem additionalCost = Mapper.Map<InvoiceAdditionalCostViewItem>(message.Entity);
                    Invoice.AdditionalCosts.Add(additionalCost);

                    break;
                case MessageType.Changed:

                    Invoice.AdditionalCosts.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }

            RefreshSummaryItems();
        }

        private void ExportToXlsx(TableView tableView)
        {
            if (!CheckConditionsAndConfirmExport(tableView))
            {
                return;
            }

            string fileName = $"Invoice_{Invoice.Id}_{DateTime.Now:yyyy-MM-dd}";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SaveFileDialogService.ShowDialog(
                _ =>
                {
                    string filePath = SaveFileDialogService.File.GetFullName();
                    tableView.ExportToXlsx(filePath, new XlsxExportOptionsEx(TextExportMode.Value));
                    MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");
                },
                folderPath,
                fileName);
        }

        private bool CheckConditionsAndConfirmExport(TableView tableView)
        {
            if (tableView?.Grid == null)
            {
                return false;
            }

            if (tableView.Grid.SelectedItem == null)
            {
                MessageFacadeService.ShowMessageBox(
                    "Не выбран ни один товар.",
                    "Невозможно выполнить экспорт",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (tableView.Grid.SelectedItems.Count < Invoice.InvoiceProducts.Count)
            {
                if (!MessageFacadeService.Confirm("Выбраны не все товары. Продолжить?"))
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyEditingInfo(InvoiceEditingInfoDto info, IReadOnlyCollection<ProductDto> products, IPriceConverter priceConverter)
        {
            EditorsNames.Clear();
            EditorsNames.AddRange(info.Editors);
            RaisePropertyChanged(() => EditorsNames);

            IsLocked = info.EmployeeLock != null && info.EmployeeLock.Id != CurrentUser.Id;
            LockedBy = info.EmployeeLock;

            SetIsEditableForCurrentUser(Invoice);

            Invoice.InvoiceProducts.ForEach(x => x.SetPriceConverter(priceConverter));

            Invoice.IgnoreTransit = info.IgnoreTransit;

            foreach (InvoiceProductViewItem invoiceProduct in Invoice.InvoiceProducts)
            {
                invoiceProduct.PriceTelemartUah = products.FirstOrDefault(x => x.Id == invoiceProduct.ProductId)?.Price;
            }

            if (info.StateId != Invoice.State.Id)
            {
                Invoice.State = Dictionaries.GetItemById<InvoiceState>(info.StateId);
                RaisePropertiesChanged(GetPropertyNamesDependentOnState().ToArray());
                MessageFacadeService.ShowNotificationWarning($"Статус накладной изменился на \"{Invoice.State.Name}\"");
            }

            BeforeClosingTime = info.BeforeClosingTime;
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems();

            IEnumerable<SummaryViewItem> GetSummaryItems()
            {
                const string TimeFormat = "hh\\:mm\\:ss";

                contractors.TryGetValue(Invoice.SupplierId, out string contractor);
                warehouses.TryGetValue(Invoice.WarehouseId, out string warehouse);
                supplerWarehouses.TryGetValue(Invoice.SupplierWarehouseId ?? -1, out string supplierWarehouse);

                yield return new SummaryViewItem("Поставщик", contractor);
                yield return new SummaryViewItem("Закупка до", Invoice.DateClose.ToString(DateFormattingRules.FullDateTimeFormat));

                if (Invoice.State == InvoiceState.Open && BeforeClosingTime.HasValue)
                {
                    const string Key = "До закрытия";

                    if (BeforeClosingTime.Value <= TimeSpan.Zero)
                    {
                        yield return new SummaryViewItem(Key, TimeSpan.Zero.ToString(TimeFormat), SummaryViewItem.RedLevel);
                    }
                    else if (BeforeClosingTime <= TimeSpan.FromMinutes(10))
                    {
                        yield return new SummaryViewItem(Key, BeforeClosingTime.Value.ToString(TimeFormat), SummaryViewItem.RedLevel);
                    }
                }

                yield return new SummaryViewItem("Прибытие", Invoice.DateArrive.ToString(DateFormattingRules.FullDateTimeFormat));

                if (Invoice.ArrivedOn.HasValue)
                {
                    yield return new SummaryViewItem("Прибыла", Invoice.ArrivedOn.Value.ToString(DateFormattingRules.FullDateTimeFormat));
                }

                yield return new SummaryViewItem("Получение", Invoice.DateGet.ToString(DateFormattingRules.FullDateTimeFormat));
                yield return new SummaryViewItem("Доставка", Invoice.CarryType.Name);
                yield return new SummaryViewItem("Со склада", supplierWarehouse);
                yield return new SummaryViewItem("На склад", warehouse);
                yield return new SummaryViewItem("Статус", Invoice.State.Name);
                yield return new SummaryViewItem("Вес", $"{Invoice.ProductsWeight:N1}");
                yield return new SummaryViewItem("Учет в роботе", (!Invoice.IgnoreTransit).ToStringAlt());

                Prices prices = new Prices(
                    Invoice.AdditionalCosts.Where(x => x.CurrencyId == Currency.UahId).Sum(x => x.Amount!.Value),
                    Invoice.AdditionalCosts.Where(x => x.CurrencyId == Currency.UsdId).Sum(x => x.Amount!.Value),
                    Invoice.AdditionalCosts.Where(x => x.CurrencyId == Currency.EurId).Sum(x => x.Amount!.Value));

                yield return new SummaryViewItem("Доп. расходы", prices.ToString());

                InvoiceCurrencyRateDto usdCurrency = Invoice.CurrencyRates?
                    .FirstOrDefault(x => x.FromCurrencyId == Currency.UsdId);

                if (usdCurrency is not null)
                {
                    yield return new SummaryViewItem("Курс доллара", $"{usdCurrency.ConversionRate:N2}");
                }

                InvoiceCurrencyRateDto eurCurrency = Invoice.CurrencyRates?
                    .FirstOrDefault(x => x.FromCurrencyId == Currency.EurId);

                if (eurCurrency is not null)
                {
                    yield return new SummaryViewItem("Курс евро", $"{eurCurrency.ConversionRate:N2}");
                }

                if (Invoice.InvoiceTtns?.Any() == true)
                {
                    yield return new SummaryViewItem("ТТН", string.Join(",\n", Invoice.InvoiceTtns.Select(x => x.Ttn)));
                }

                if (Invoice.ReceivedBy.HasValue && Invoice.ReceivedOn.HasValue)
                {
                    employees.TryGetValue(Invoice.ReceivedBy ?? 0, out string receivedByEmployee);
                    yield return new SummaryViewItem("Принял", $"{receivedByEmployee} ({Invoice.ReceivedOn.Value.ToString(DateFormattingRules.FullDateTimeFormat)})");
                }

                if (!string.IsNullOrWhiteSpace(Invoice.Comment))
                {
                    yield return new SummaryViewItem("Комментарий", Invoice.Comment);
                }

                if (Invoice.SupplierAutoReserve.HasValue)
                {
                    yield return new SummaryViewItem("Авто-резерв", Invoice.SupplierAutoReserve.Value.ToStringAlt());
                }
            }
        }

        private void InvoiceChanged()
        {
            // Model property created for reflection in Discussions
            Model = new { Invoice?.Id };
            RaisePropertyChanged(nameof(NotifySupplierInTelegramVisible));
        }

        private void SetInvoice(InvoiceViewItem invoiceViewItem)
        {
            Invoice = invoiceViewItem;

            IsLocked = Invoice.EmployeeLock != null && Invoice.EmployeeLock.Id != CurrentUser.Id;
            LockedBy = Invoice.EmployeeLock;

            BeforeClosingTime = null;

            SetIsEditableForCurrentUser(Invoice);
            Sort();
        }

        private void SelectedInvoiceProductViewItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            AllocatedQuantityStr = $"{SelectedInvoiceProductViewItems?.Count}/{SelectedInvoiceProductViewItems?.Sum(x => x.Quantity)}";
        }

        private IEnumerable<string> GetPropertyNamesDependentOnState()
        {
            yield return nameof(Invoice);
            yield return nameof(CanEditPrice);
            yield return nameof(CanEditQuantity);
            yield return nameof(OkVisible);
            yield return nameof(OpenVisible);
            yield return nameof(CloseVisible);
            yield return nameof(ArrivedVisible);
            yield return nameof(CompareVisible);
            yield return nameof(ReceiveVisible);
            yield return nameof(CancelVisible);
            yield return nameof(CreateBillVisible);
            yield return nameof(DelayVisible);
            yield return nameof(PurchaseVisible);
            yield return nameof(DontReceiveVisible);
        }

        private void AddInvoiceProducts(params InvoiceProductViewItem[] products)
        {
            foreach (InvoiceProductViewItem product in products)
            {
                product.PropertyChanged += Invoice.OnProductPropertyChanged;
                Invoice.InvoiceProducts.Add(product);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cancellationTokenSource.Dispose();
                }

                disposedValue = true;
            }
        }

        #endregion
    }
}