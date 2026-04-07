using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using MediatR;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions;
using Telemart.Client.Data.Requests.Features.ConstantQueries;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.OrderDocuments;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Store;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.AdditionalServiceProduct
{
    public sealed class AdditionalServiceProductViewModel : TelemartEditorViewModelBase<AdditionalServiceProductDto, AdditionalServiceProductParameter, AdditionalServiceProductViewItem>
    {
        private IReadOnlyDictionary<int, List<string>> _accountingSystemSerials;
        private AdditionalServiceProductDto _primaryAdditionalServiceProduct;
        private int? _additionalServiceWarehouseId;

        public AdditionalServiceProductViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMediator mediator,
            IMessenger messenger,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            Mediator = mediator;
            ErrorHandler = errorHandler;

            EditProductSerialsCommand = new DelegateCommand<AdditionalServiceProductSnViewItem>(EditProductSerials, x => IsLockedByCurrentEmployee || !string.IsNullOrWhiteSpace(x?.SerialNumber));
            HandleRowDoubleClickCommand = new DelegateCommand<RowDoubleClickInfo>(HandleRowDoubleClick, x => x != null);
            OpenOrderCommand = new DelegateCommand<int?>(OpenOrder, x => x.HasValue);
            PrintBarcodeCommand = new AsyncCommand(PrintBarcodeAsync);
            ClearScannedQuantityCommand = new DelegateCommand<AdditionalServiceProductSnViewItem>(ClearScannedQuantity, _ => IsLockedByCurrentEmployee);

            StartDoingAdditionalServiceCommand = new AsyncCommand(StartDoingAsync, () => Model != null && Model.StateId == AdditionalServiceProductState.WarehouseId && !IsLockedByEmployee);
            StopDoingAdditionalServiceCommand = new AsyncCommand(StopDoingAsync, () => Model != null && Model.StateId == AdditionalServiceProductState.DoingId && !IsLockedByCurrentEmployee);
            CompleteAdditionalServiceCommand = new AsyncCommand(CompleteAdditionalServiceProductAsync, () => Model != null && Model.StateId == AdditionalServiceProductState.DoingId && !IsLockedByCurrentEmployee);

            OpenAdditionalServiceProductCommand = new DelegateCommand<int?>(OpenAdditionalServiceProduct, x => Model != null && x.HasValue);

            RecognizeBarcodeViewModel = new RecognizeBarcodeViewModel(webClient, dictionaries, messageFacadeService);
            RecognizeBarcodeViewModel.OnStarted += RecognizeBarcodeViewModelOnStarted;
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;
            RecognizeBarcodeViewModel.OnFinishCommand += RecognizeBarcodeViewModelOnFinishCommand;

            Warehouses = new ObservableRangeCollection<WarehouseDto>();
        }

        public AdditionalServiceProductViewModel()
        {
        }

        public IDelegateCommand EditProductSerialsCommand { get; }

        public IDelegateCommand OpenOrderCommand { get; }

        public IDelegateCommand OpenAdditionalServiceProductCommand { get; }

        public IDelegateCommand ClearScannedQuantityCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public IAsyncCommand StartDoingAdditionalServiceCommand { get; }

        public IAsyncCommand StopDoingAdditionalServiceCommand { get; }

        public IAsyncCommand CompleteAdditionalServiceCommand { get; }

        public IAsyncCommand PrintBarcodeCommand { get; }

        public RecognizeBarcodeViewModel RecognizeBarcodeViewModel { get; }

        #region Properties

        public ObservableRangeCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<AdditionalServiceProductState> States
        {
            get { return GetProperty(() => States); }
            private set { SetProperty(() => States, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public IEnumerable<SummaryViewItem> AdditionalServiceProductSummaryItems
        {
            get { return GetProperty(() => AdditionalServiceProductSummaryItems); }
            private set { SetProperty(() => AdditionalServiceProductSummaryItems, value); }
        }

        public bool IsRecognitionInProgress
        {
            get { return GetProperty(() => IsRecognitionInProgress); }
            private set { SetProperty(() => IsRecognitionInProgress, value); }
        }

        public string PrimaryAdditionalServiceProductName
        {
            get { return GetProperty(() => PrimaryAdditionalServiceProductName); }
            private set { SetProperty(() => PrimaryAdditionalServiceProductName, value); }
        }

        #endregion

        #region DialogSettings

        public bool VisibleGuestProductColumn => Model?.ProductWithConsumables?.Any(x => x.IsGuestProduct) == true;

        public override int Width => 800;

        public override int MinWidth => 700;

        public override int MaxWidth => 950;

        public override int Height => 500;

        public override int MinHeight => 500;

        public override int MaxHeight => 540;

        #endregion

        #region BaseSettings

        protected override string CreatedActionMessage => string.Empty;

        protected override string EntityName => "Оказание услуги";

        protected override string UpdatedActionMessage => "сохранено";

        #endregion

        private IMediator Mediator { get; }

        private IErrorHandler ErrorHandler { get; }

        #region BaseMethods

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);
            Warehouses.AddRange(warehouses.Data);

            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees
                .Where(x => x.Active)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                .ToReadOnlyObservableCollection();

            States = Dictionaries.GetItems<AdditionalServiceProductState>().ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            int[] productIds = Model.ProductWithConsumables.Select(x => x.ProductId).Distinct().ToArray();

            PagedResult<ProductAttributesDto> attributesResult = await WebClient.ExecuteApiRequestAsync(new QueryProductsAttributesByIds(productIds, true));

            RecognizeBarcodeViewModel.Init(new RecognizeBarcodeSettings(true, true), attributesResult?.Data);

            _accountingSystemSerials = attributesResult?.Data.ToDictionary(x => x.ProductId, x => x.Serials);

            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(Model.OrderId));

            _additionalServiceWarehouseId = order.AdditionalServiceWarehouseId;

            await LoadPrimaryAdditionalServiceProductAsync();
        }

        protected override void AfterSetData()
        {
            RaisePropertyChanged(nameof(VisibleGuestProductColumn));

            AdditionalServiceProductSummaryItems = GetSummaryItems();
        }

        protected override Task<AdditionalServiceProductDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceProduct(id));
        }

        protected override Task<LockResponse<AdditionalServiceProductDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockAdditionalServiceProduct(id));
        }

        protected override Task<LockResponse<AdditionalServiceProductDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockAdditionalServiceProduct(id));
        }

        protected override void SetEditTitle()
        {
            Title = $"Оказание услуги ({Model.Id})";
        }

        protected override Task<Result<AdditionalServiceProductDto>> UpdateEntityAsync()
        {
            AdditionalServiceProductSaveDto saveDto = MapSaveDto(Model);

            return WebClient.ExecuteApiRequestAsync(new UpdateAdditionalServiceProduct(Model.Id, saveDto));
        }

        protected override async Task<bool> SaveAsync()
        {
            bool success = await base.SaveAsync();

            if (success)
            {
                AdditionalServiceProductDto additionalServiceProductDto = await GetEntityAsync(Model.Id);

                Messenger.Send(new AdditionalServiceProductEntityMessage(additionalServiceProductDto, MessageType.Changed));
            }

            return success;
        }

        protected override bool CanEdit()
        {
            return Model?.StateId == AdditionalServiceProductState.WarehouseId;
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override Task<Result<AdditionalServiceProductDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        #endregion

        private static AdditionalServiceProductSaveDto MapSaveDto(AdditionalServiceProductViewItem source)
        {
            AdditionalServiceProductSnViewItem product = source.ProductWithConsumables.First(x => x.ConsumableId is null);

            AdditionalServiceProductSaveDto target = new AdditionalServiceProductSaveDto()
            {
                Id = source.Id,
                SerialNumber = product.SerialNumber,
                Scanned = product.Scanned,
                EmployeeId = source.EmployeeId,
                GuestProduct = product.GuestProduct,
                ConsumableProducts = source.ProductWithConsumables
                    .Where(x => x.ConsumableId.HasValue)
                    .Select(
                        x => new AdditionalServiceProductConsumableSaveDto()
                        {
                            SerialNumber = x.SerialNumber,
                            Scanned = x.Scanned,
                            Id = x.ConsumableId.Value,
                            GuestProduct = x.GuestProduct
                        })
                    .ToArray()
            };
            return target;
        }

        private void ClearScannedQuantity(AdditionalServiceProductSnViewItem item)
        {
            item.Scanned = false;
        }

        private void HandleRowDoubleClick(RowDoubleClickInfo e)
        {
            if (e.FieldName.Equals(nameof(AdditionalServiceProductSnViewItem.GuestProduct)) || e.FieldName.Equals(nameof(AdditionalServiceProductViewItem.GuestProduct)))
            {
                if (e.Data is AdditionalServiceProductSnViewItem consumble && consumble.IsGuestProduct)
                {
                    SetAdditionalServiceProductClientProduct(consumble);
                }

                return;
            }

            EditProductSerialsCommand.Execute((AdditionalServiceProductSnViewItem)e.Data);
        }

        private void RecognizeBarcodeViewModelOnStarted(object sender, EventArgs e)
        {
            IsRecognitionInProgress = true;
        }

        private void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeBarcodeResultEventArgs e)
        {
            IsRecognitionInProgress = false;

            switch (e.Result)
            {
                case RecognizeBarcodeResult.Found:
                case RecognizeBarcodeResult.FoundInSupplier:
                    e.Message = AddProduct(e.Product);
                    break;
                case RecognizeBarcodeResult.NotFound:
                    e.Message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "ШК не найден в БД");
                    break;
            }
        }

        private void RecognizeBarcodeViewModelOnFinishCommand(object sender, EventArgs e)
        {
            OkCommand.Execute(null);
        }

        private void EditProductSerials(AdditionalServiceProductSnViewItem productViewItem)
        {
            ProductEditSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
               new ProductEditSerialsParameter(new[] { productViewItem.SerialNumber }.Where(x => !string.IsNullOrEmpty(x)).ToList(), !IsLockedByCurrentEmployee), this);

            if (!IsLockedByCurrentEmployee)
            {
                return;
            }

            if (viewModel.IsOk)
            {
                if (viewModel.SerialNumbers.Any())
                {
                    productViewItem.SerialNumber = viewModel.SerialNumbers.First();
                }
                else
                {
                    productViewItem.SerialNumber = null;

                    if (productViewItem.KeepSerialOverriden)
                    {
                        productViewItem.Scanned = false;
                    }
                }

                productViewItem.SerialNumber = viewModel.SerialNumbers.Any()
                    ? viewModel.SerialNumbers.First() : null;
            }
        }

        private async Task PrintBarcodeAsync()
        {
            PrintAdditionalServiceBarcodeReportRequest request;

            if (Model.PrimaryAdditionalServiceProductId.HasValue)
            {
                request = new PrintAdditionalServiceBarcodeReportRequest(
                    Model.Id,
                    Model.OrderId,
                    null,
                    Model.Date);
            }
            else
            {
                request = new PrintAdditionalServiceBarcodeReportRequest(
                    Model.Id,
                    Model.OrderId,
                    Model.OrderDeliveryTimeTo,
                    Model.CompletedOn);
            }

            await Mediator.Send(request);
        }

        private RecognizeBarcodeMessage AddProduct(ProductAttributesDto product)
        {
            AdditionalServiceProductSnViewItem productViewItem = Model.ProductWithConsumables
                .OrderBy(x => x.Scanned)
                .FirstOrDefault(x => x.ProductId == product.ProductId);

            if (productViewItem is null)
            {
                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, $"Товар {product.FullName} не найден в услуге.");
            }

            if (productViewItem.Scanned)
            {
                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, $"Товар {product.FullName} уже просканирован");
            }

            if (productViewItem.KeepSerialOverriden)
            {
                string[] barcodes = RecognizeBarcodeViewModel.GetBarcodesById(productViewItem.ProductId).ToArray();

                ProductSerialsViewModelParameter parameter = new ProductSerialsViewModelParameter(
                    productViewItem.ProductId,
                    Model.ProductWithConsumables.Where(x => !string.IsNullOrWhiteSpace(x.SerialNumber)).Select(x => x.SerialNumber).ToArray(),
                    barcodes,
                    product.SerialNumberLength,
                    ScanSerialMode.Single,
                    _accountingSystemSerials[productViewItem.ProductId]);

                ProductScanSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductScanSerialsViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    string serial = viewModel.SerialNumbers.FirstOrDefault();

                    if (Model.PrimaryAdditionalServiceProductId.HasValue)
                    {
                        if (serial != _primaryAdditionalServiceProduct.SerialNumber && !productViewItem.ConsumableId.HasValue)
                        {
                            return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "SN товара не соответсвует товару в первичной услуге");
                        }

                        if (productViewItem.ConsumableId.HasValue && _primaryAdditionalServiceProduct.ConsumableProducts?.Any(x => x.SerialNumber == serial) != true)
                        {
                            return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "SN товара не соответсвует товару для оказания в первичной услуге");
                        }
                    }

                    productViewItem.SerialNumber = serial;

                    if (!string.IsNullOrWhiteSpace(productViewItem.SerialNumber))
                    {
                        productViewItem.Scanned = true;
                    }
                }
            }
            else
            {
                productViewItem.Scanned = true;
            }

            return null;
        }

        private async Task StartDoingAsync()
        {
            if (!WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(Model.WarehouseId))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на склад оказания услуги");
                return;
            }

            object checkValue = await WebClient.ExecuteApiRequestAsync(new QueryConstant(ConstantKeys.GuestProductCheckPinnedScan));

            if (short.TryParse(checkValue.ToString(), out short checkPinnedScanForGuestProduct) && checkPinnedScanForGuestProduct > 0)
            {
                if (Model.ProductWithConsumables.Any(x => x.ProductTypeId == ProductType.GuestProductId && x.GuestProduct?.KeepProduct == true))
                {
                    List<OrderDocumentSimpleDto> documentDtos = await WebClient.ExecuteApiRequestAsync(new QueryOrderDocuments(Model.OrderId));

                    if (documentDtos is null || documentDtos.All(x => x.TypeId != (int)OrderDocumentTypeIds.ActIncomeId && x.TypeId != (int)OrderDocumentTypeIds.ActOutcomeId))
                    {
                        MessageFacadeService.ShowNotificationError($"Необходимо прикрепить Акт принятия{Environment.NewLine}гос. товара");
                        return;
                    }
                }
            }

            Result<AdditionalServiceProductDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new StartDoingAdditionalServiceProduct(Model.Id)),
                "при старте оказания услуги",
                "Оказание услуги начато",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                SetData(result.Data);
                AdditionalServiceProductSummaryItems = GetSummaryItems();
                Messenger.Send(new EntityMessage<AdditionalServiceProductDto>(result.Data, MessageType.Changed));
            }
        }

        private async Task StopDoingAsync()
        {
            if (!WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(Model.WarehouseId))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на склад оказания услуги");
                return;
            }

            Result<AdditionalServiceProductDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new StopDoingAdditionalServiceProduct(Model.Id)),
                "при остановке оказания услуги",
                "Оказание услуги остановлено",
                this,
                true);

            if (result.IsSuccess)
            {
                SetData(result.Data);
                AdditionalServiceProductSummaryItems = GetSummaryItems();
                Messenger.Send(new EntityMessage<AdditionalServiceProductDto>(result.Data, MessageType.Changed));
            }
        }

        private async Task CompleteAdditionalServiceProductAsync()
        {
            if (!WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(Model.WarehouseId))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на склад оказания услуги");
                return;
            }

            DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>("Вы уверены, что хотите завершить услугу?", this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Result<AdditionalServiceProductDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CompleteAdditionalServiceProduct(Model.Id)),
                "при завершении оказания услуг",
                "Оказываемая услуга завершена",
                this,
                true);

            if (result.IsSuccess)
            {
                if (Model.ControlInMovements && Model.PrimaryAdditionalServiceProductId == null)
                {
                    await PrintBarcodeAsync();
                }

                Messenger.Send(new EntityMessage<AdditionalServiceProductDto>(result.Data, MessageType.Changed));
                Close();
            }
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            WarehouseDto warehouse = Warehouses.FirstOrDefault(x => x.Id == Model.WarehouseId);
            WarehouseDto orderWarehouse = Warehouses.FirstOrDefault(x => x.Id == Model.OrderWarehouseId);
            AdditionalServiceProductState state = States.FirstOrDefault(x => x.Id == Model.StateId);
            OrderStatus orderState = Dictionaries.GetItems<OrderStatus>().FirstOrDefault(x => x.Id == Model.OrderStateId);
            ComboBoxItem employee = Employees.FirstOrDefault(x => x.Id == Model.EmployeeId);

            yield return new SummaryViewItem("Склад услуги", warehouse?.Name);
            yield return new SummaryViewItem("Склад заказа", orderWarehouse?.Name);
            yield return new SummaryViewItem("Статус услуги", state?.Name);
            yield return new SummaryViewItem("Статус заказа", orderState?.Name);
            yield return new SummaryViewItem("Ответственный", employee.DisplayValue);

            if (Model.Date.HasValue)
            {
                yield return new SummaryViewItem("Дата (план)", Model.Date?.ToString(DateFormattingRules.DateFormat));
            }

            if (Model.CompletedOn.HasValue)
            {
                yield return new SummaryViewItem("Дата (факт)", Model.CompletedOn.Value.ToString(DateFormattingRules.FullDateTimeFormat));
                yield return new SummaryViewItem("Завершил", Employees.FirstOrDefault(x => x.Id == Model.CompletedBy).DisplayValue);
            }

            if (Model.OrderDeliveryTimeTo.HasValue)
            {
                yield return new SummaryViewItem("Дата X", Model.OrderDeliveryTimeTo.Value.ToString(DateFormattingRules.FullDateTimeFormat));
            }
        }

        private void OpenOrder(int? orderId)
        {
            Messenger.Send(new OrderEditViewMessage(orderId!.Value));
        }

        private void OpenAdditionalServiceProduct(int? additionalServiceProduct)
        {
            Messenger.Send(new AdditionalServiceProductViewMessage(additionalServiceProduct!.Value));
        }

        private void SetAdditionalServiceProductClientProduct(AdditionalServiceProductSnViewItem consumble)
        {
            string product = consumble.GuestProduct?.Product;
            bool keepProduct = consumble.GuestProduct?.KeepProduct ?? false;
            string sn = consumble.GuestProduct?.SerialNumber;
            string description = consumble.GuestProduct?.Description;
            int? additionalServiceWarehouseId = _additionalServiceWarehouseId;

            AdditionalServiceProductClientProductViewModel model =
                DialogDocumentManagerService.ShowView<AdditionalServiceProductClientProductViewModel>(
                    new AdditionalServiceProductClientProductParameter(
                        product,
                        sn,
                        description,
                        IsLockedByCurrentEmployee,
                        additionalServiceWarehouseId,
                        keepProduct),
                    this);

            if (!model.IsOk)
            {
                return;
            }

            consumble.GuestProduct ??= new GuestProductDto();

            consumble.GuestProduct.Product = model.Product;
            consumble.GuestProduct.KeepProduct = model.KeepGuestProduct;
            consumble.GuestProduct.SerialNumber = model.SerialNumber;
            consumble.GuestProduct.Description = model.Description;
        }

        private async Task LoadPrimaryAdditionalServiceProductAsync()
        {
            if (Model.PrimaryAdditionalServiceProductId.HasValue)
            {
                _primaryAdditionalServiceProduct = await WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceProduct(Model.PrimaryAdditionalServiceProductId));

                PrimaryAdditionalServiceProductName = _primaryAdditionalServiceProduct.Name;
            }
        }
    }
}