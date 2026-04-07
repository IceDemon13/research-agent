using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using Humanizer;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Complaint;
using Telemart.Client.Data.Requests.Features.Customer;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.Requests.Features.TradeIn.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.Reports.TradeIn;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Complaint;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Common.PrintBarcodeParameters;
using Telemart.Client.ViewModels.Complaint;
using Telemart.Client.ViewModels.Customer;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Dialogs.AddDocuments;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInEditViewModel : TelemartEditorViewModelBase<TradeInDto, TradeInViewMessage, TradeInViewItem>
    {
        private readonly IErrorHandler _errorHandler;
        private readonly TelegramBotOptions _telegramBotOptions;

        private IReadOnlyDictionary<int, IReadOnlyCollection<OrderProductSnDto>> _productSerials;
        private ReadOnlyObservableCollection<CategoryDto> _allCategories;
        private ReadOnlyObservableCollection<ProductDto> _products;
        private ReadOnlyObservableCollection<TradeInCoefDto> _coefs;
        private ObservableCollection<TradeInDocumentSimpleDto> _tradeInDocuments;
        private ObservableCollection<TradeInEDocumentSimpleDto> _tradeInEDocuments;
        private OrderDto _order;
        private TradeInMaxPriceDto _tradeInMaxPrice;

        public TradeInEditViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IErrorHandler errorHandler,
            TelegramBotOptions telegramBotOptions)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            _errorHandler = errorHandler;
            _telegramBotOptions = telegramBotOptions;

            CustomerSearchCommand = new AsyncCommand<string>(CustomerSearchAsync, CanSearchClient);
            SearchOrderCommand = new AsyncCommand<int?>(SearchOrderAsync, CanSearchOrder);
            RemoveOrderCommand = new DelegateCommand(RemoveOrder, CanRemoveOrder);
            RemoveProductCommand = new DelegateCommand(RemoveProduct, () => Model?.ProductId.HasValue == true && CanChangeProduct());
            SelectProductCommand = new DelegateCommand(SelectProduct, CanChangeProduct);

            EvaluateRequestCommand = new AsyncCommand(EvaluateRequestAsync, CanEvaluate);
            ОverEvaluateRequestCommand = new AsyncCommand(ОverEvaluateRequestAsync, CanOverEvaluate);
            CompleteRequestCommand = new AsyncCommand(CompleteRequestAsync, CanComplete);
            CancelRequestCommand = new AsyncCommand(CancelRequestAsync, CanCancel);
            TestRequestCommand = new AsyncCommand(TestRequestAsync, CanTest);
            CreateTaskRequestCommand = new AsyncCommand(CreateTaskAsync, CanCreateTask);
            ProductEditValueChangedCommand = new DelegateCommand<EditValueChangedEventArgs>(ProductEditValueChanged);
            HandleSelectionChangedCommand = new DelegateCommand<ValueChangedEventArgs<FrameworkElement>>(HandleSelectionChanged);
            OrderIdChangedCommand = new DelegateCommand<EditValueChangedEventArgs>(OrderIdChanged);
            EditValueChangedCommand = new DelegateCommand(ReCalculate);
            ReceiveCommand = new AsyncCommand(ReceiveAsync, () => Model?.StateId == TradeInState.Evaluated.Id && Model.EmployeeLockId is null);
            PrintActCommand = new AsyncCommand(PrintActAsync, () => Model != null && (Model.StateId == TradeInState.Received.Id || Model.StateId == TradeInState.Completed.Id));
            RefreshDocumentsCommand = new AsyncCommand(RefreshDocumentsAsync);
            AddDocumentCommand = new DelegateCommand(AddDocument, () => Model != null && WebClient.IsOperationAllowed(BusinessOperation.CreateTradeInDocumment));
            RemoveDocumentCommand = new AsyncCommand<TradeInDocumentViewItem>(RemoveDocumentAsync, x => x != null && WebClient.IsOperationAllowed(BusinessOperation.RemoveTradeInDocument));
            PrintDocumentCommand = new AsyncCommand<TradeInDocumentViewItem>(PrintDocumentAsync, x => x != null);
            RefreshEDocumentsCommand = new AsyncCommand(RefreshEDocumentsAsync);
            AddEDocumentCommand = new AsyncCommand(AddEDocumentAsync, () => Model != null && WebClient.IsOperationAllowed(BusinessOperation.CreateTradeInEDocument));
            RemoveEDocumentCommand = new AsyncCommand<TradeInEDocumentViewItem>(RemoveEDocumentAsync, x => x != null && !x.AcceptedClient && (Model?.StateId == TradeInState.New.Id || Model?.StateId == TradeInState.Evaluated.Id || Model?.StateId == TradeInState.Received.Id) && WebClient.IsOperationAllowed(BusinessOperation.RemoveTradeInEDocument));
            PrintEDocumentCommand = new AsyncCommand<TradeInEDocumentViewItem>(PrintEDocumentAsync, x => x != null);
            SendEDocumentToClientCommand = new AsyncCommand<TradeInEDocumentViewItem>(SendEDocumentToClientAsync, x => x != null && !x.AcceptedClient && WebClient.IsOperationAllowed(BusinessOperation.SendTradeInEDocument));
            SignEDocumentByTelemartCommand = new AsyncCommand<TradeInEDocumentViewItem>(SignEDocumentByTelemartAsync, x => x != null && x.AcceptedClient && !x.AcceptedTelemart);
            ShowPhoneHistoryClientCommand = new DelegateCommand<string>(ShowPhoneHistory, CanShowPhoneHistory);
            OpenServiceRequestCommand = new DelegateCommand<int?>(OpenServiceRequest, x => x > 0);
            RefreshCrmCommand = new AsyncCommand(RefreshCrmAsync);
            HandleDocumentsChangedCommand = new DelegateCommand(HandleDocumentsChanged);
            ShowDocumentsBotQrCommand = new DelegateCommand(ShowDocumentsBotQr);
            RefreshComplaintsCommand = new AsyncCommand(RefreshComplaintsAsync);
            AddComplaintCommand = new DelegateCommand(AddComplaint);
            Messenger.Register<TradeInCreateDocumentMessage>(this, OnDocumentMessage);
            Messenger.Register<ComplaintMessage>(this, OnComplaintMessage);
            SelectDeliveryAddressCommand = new DelegateCommand(SelectDeliveryAddress, () => Model != null && Model.CarryOutId != null && Model.CarryOutId != CarryType.PickupId);
        }

        #region INPC

        public CategoryDto SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value, ChangedCategory); }
        }

        public string CustomerClass
        {
            get { return GetProperty(() => CustomerClass); }
            set { SetProperty(() => CustomerClass, value); }
        }

        public string CustomerWarranty
        {
            get { return GetProperty(() => CustomerWarranty); }
            set { SetProperty(() => CustomerWarranty, value); }
        }

        public string CustomerPack
        {
            get { return GetProperty(() => CustomerPack); }
            set { SetProperty(() => CustomerPack, value); }
        }

        public int? DocumentsCount
        {
            get { return GetProperty(() => DocumentsCount); }
            private set { SetProperty(() => DocumentsCount, value); }
        }

        public int? EDocumentsCount
        {
            get { return GetProperty(() => EDocumentsCount); }
            private set { SetProperty(() => EDocumentsCount, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public ObservableCollection<TradeInDocumentViewItem> TradeInDocumentItems
        {
            get { return GetProperty(() => TradeInDocumentItems); }
            private set { SetProperty(() => TradeInDocumentItems, value); }
        }

        public ObservableCollection<TradeInEDocumentViewItem> TradeInEDocumentViewItems
        {
            get { return GetProperty(() => TradeInEDocumentViewItems); }
            private set { SetProperty(() => TradeInEDocumentViewItems, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> TradeInClassValues
        {
            get { return GetProperty(() => TradeInClassValues); }
            private set { SetProperty(() => TradeInClassValues, value); }
        }

        public ReadOnlyObservableCollection<TradeInIndicatorValueDto> TradeInWarrantyValues
        {
            get { return GetProperty(() => TradeInWarrantyValues); }
            private set { SetProperty(() => TradeInWarrantyValues, value); }
        }

        public ReadOnlyObservableCollection<TradeInIndicatorValueDto> TradeInPackageValues
        {
            get { return GetProperty(() => TradeInPackageValues); }
            private set { SetProperty(() => TradeInPackageValues, value); }
        }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<TradeInState> Statuses
        {
            get { return GetProperty(() => Statuses); }
            private set { SetProperty(() => Statuses, value); }
        }

        public ReadOnlyObservableCollection<TradeInDocumentType> TradeInDocumentTypes
        {
            get { return GetProperty(() => TradeInDocumentTypes); }
            private set { SetProperty(() => TradeInDocumentTypes, value); }
        }

        public ReadOnlyObservableCollection<string> CurrentCategoryManufactors
        {
            get { return GetProperty(() => CurrentCategoryManufactors); }
            private set { SetProperty(() => CurrentCategoryManufactors, value); }
        }

        public ReadOnlyObservableCollection<ManufactorDto> AllManufactors
        {
            get { return GetProperty(() => AllManufactors); }
            private set { SetProperty(() => AllManufactors, value); }
        }

        public ReadOnlyObservableCollection<OrderProductSnDto> SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            private set { SetProperty(() => SerialNumbers, value); }
        }

        public ReadOnlyObservableCollection<CityDto> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<ClientContactViewItem> ClientContactsHistoryItems
        {
            get { return GetProperty(() => ClientContactsHistoryItems); }
            private set { SetProperty(() => ClientContactsHistoryItems, value); }
        }

        public ObservableCollection<ComplaintViewItem> Complaints
        {
            get { return GetProperty(() => Complaints); }
            private set { SetProperty(() => Complaints, value); }
        }

        public bool CustomerUpdated { get; private set; }

        public bool TradeInHandleButtonsIsVisible => Model != null && Model.EmployeeLockId == null && Model.StateId != TradeInState.Completed.Id && Model.StateId != TradeInState.Canceled.Id;

        public string ToolTipClass => Model != null ? TradeInClassValues.FirstOrDefault(x => x.Id == Model.ClassId).DisplayValue : string.Empty;

        public bool OrderFoundProductChanged => Model?.OrderFound == true && SerialNumbers?.Count >= 1;

        #endregion

        #region Commands

        public IDelegateCommand ShowDocumentsBotQrCommand { get; }

        public IAsyncCommand CustomerSearchCommand { get; }

        public IAsyncCommand SearchOrderCommand { get; }

        public IDelegateCommand RemoveOrderCommand { get; }

        public IAsyncCommand PrintActCommand { get; }

        public IDelegateCommand RemoveProductCommand { get; }

        public IAsyncCommand RefreshCrmCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        public IAsyncCommand EvaluateRequestCommand { get; }

        public IAsyncCommand ReceiveCommand { get; }

        public IAsyncCommand ОverEvaluateRequestCommand { get; }

        public IAsyncCommand CompleteRequestCommand { get; }

        public IAsyncCommand CancelRequestCommand { get; }

        public IAsyncCommand TestRequestCommand { get; }

        public IDelegateCommand ProductEditValueChangedCommand { get; }

        public IDelegateCommand OrderIdChangedCommand { get; }

        public IDelegateCommand EditValueChangedCommand { get; }

        public IDelegateCommand HandleSelectionChangedCommand { get; }

        public IAsyncCommand RefreshDocumentsCommand { get; }

        public IDelegateCommand AddDocumentCommand { get; }

        public IAsyncCommand RemoveDocumentCommand { get; }

        public IAsyncCommand PrintDocumentCommand { get; }

        public IAsyncCommand RefreshEDocumentsCommand { get; }

        public IAsyncCommand AddEDocumentCommand { get; }

        public IAsyncCommand RemoveEDocumentCommand { get; }

        public IAsyncCommand PrintEDocumentCommand { get; }

        public IAsyncCommand SendEDocumentToClientCommand { get; }

        public IAsyncCommand SignEDocumentByTelemartCommand { get; }

        public IAsyncCommand CreateTaskRequestCommand { get; }

        public IDelegateCommand ShowPhoneHistoryClientCommand { get; }

        public IDelegateCommand OpenServiceRequestCommand { get; }

        public IDelegateCommand HandleDocumentsChangedCommand { get; }

        public IDelegateCommand SelectDeliveryAddressCommand { get; }

        public IAsyncCommand RefreshComplaintsCommand { get; }

        public IDelegateCommand AddComplaintCommand { get; }

        #endregion

        #region DialogSettings

        public override int Height => 690;

        public override int MinHeight => 690;

        public override int MinWidth => 1084;

        public override int Width => 1084;

        #endregion

        protected override string CreatedActionMessage => string.Empty;

        protected override string EntityName => "Trade-In заявка";

        protected override string UpdatedActionMessage => "сохранено";

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>();

        private IDispatcherService DispatcherService => GetService<IDispatcherService>();

        protected override async Task HandleLoadedAsync()
        {
            TradeInViewMessage message = (TradeInViewMessage)Parameter;

            CarryTypes = Dictionaries.GetItems<CarryType>()
                .Where(x => x.Id is CarryType.PickupId or CarryType.NpWarehouseId or CarryType.NpDeliveryId or CarryType.NpPostBoxId)
                .ToReadOnlyObservableCollection();

            Statuses = Dictionaries.GetItems<TradeInState>().ToReadOnlyObservableCollection();
            TradeInDocumentTypes = Dictionaries.GetItems<TradeInDocumentType>().ToReadOnlyObservableCollection();

            await Task.WhenAll(
                LoadCategoriesAsync(),
                LoadTradeInIndicatorValuesAsync(),
                LoadWarehousesAsync(),
                LoadEmployeesAsync(),
                LoadManufactorsAsync(),
                LoadDocumentsAsync(message.Id),
                LoadEDocumentsAsync(message.Id),
                LoadTradeInCoefAsync(),
                LoadCitiesAsync());

            await base.HandleLoadedAsync();

            if (Model?.OrderId > 0)
            {
                _order = await GetOrderAsync(Model.OrderId.Value);

                await RefreshProductsAsync(_order);

                InitSerialNumbers();
            }

            if (Model?.ProductId.HasValue == true)
            {
                _tradeInMaxPrice = await GetProductMaxPriceAsync(Model.ProductId.Value);
            }

            RaisePropertiesChanged(
                nameof(TradeInHandleButtonsIsVisible),
                nameof(ToolTipClass),
                nameof(OrderFoundProductChanged));
        }

        protected override Task<bool> SaveAsync()
        {
            if (IsChanged && Model.Phone != ModelOriginal.Phone && CustomerUpdated == false)
            {
                MessageFacadeService.ShowMessageBoxWarning("Номер телефона был изменен. Для актуализации данных клиента нажмите кнопку \"Найти клиента по номеру\"");
                return Task.FromResult(false);
            }

            return base.SaveAsync();
        }

        protected override void AfterSetData()
        {
            SummaryItems = GetSummaryItems();

            CustomerClass = TradeInClassValues.FirstOrDefault(x => x.Id == Model.CustomerClassId).DisplayValue;

            CustomerWarranty = TradeInWarrantyValues.FirstOrDefault(x => x.Id == Model.CustomerWarrantyId)?.Name;

            CustomerPack = TradeInPackageValues.FirstOrDefault(x => x.Id == Model.CustomerPackId)?.Name;

            SelectedCategory = _allCategories.FirstOrDefault(x => x.Id == Model.CategoryId);

            CustomerUpdated = true;

            base.AfterSetData();
        }

        protected override Task<Result<TradeInDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override Task<TradeInDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryTradeIn(id));
        }

        protected override Task<LockResponse<TradeInDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockTradeIn(id));
        }

        protected override Task<LockResponse<TradeInDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockTradeIn(id));
        }

        protected override object CreateEntityMessage(TradeInDto taskDto, MessageType messageType)
        {
            return new TradeInMessage(taskDto, messageType);
        }

        protected override void SetCreateTitle()
        {
        }

        protected override void SetEditTitle()
        {
            Title = $"Trade-In заявка  №{Model?.Id} от {Model?.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)}";
        }

        protected override Task<Result<TradeInDto>> UpdateEntityAsync()
        {
            TradeInUpdateDto dto = new TradeInUpdateDto()
            {
                Id = Model.Id,
                OrderId = Model.OrderId,
                OrderProductId = Model.OrderProductId,
                CustomerId = Model.CustomerId,
                WarehouseId = Model.WarehouseId,
                ProductId = Model.ProductId,
                ClassId = Model.ClassId,
                PackId = Model.PackId,
                Brand = Model.Brand,
                CategoryId = Model.CategoryId,
                WarrantyId = Model.WarrantyId,
                FirstName = Model.FirstName,
                LastName = Model.LastName,
                MiddleName = Model.MiddleName,
                ModelOrPn = Model.ModelOrPn,
                Description = Model.Description,
                Comment = Model.Comment,
                Phone = Model.Phone,
                SerialNumber = Model.SerialNumber,
                Email = Model.Email,
                RealBuyoutAmount = Model.RealBuyoutAmount ?? 0,
                CarryInId = Model.CarryInId,
                CarryOutId = Model.CarryOutId,
                CityOutId = Model.CityOutId,
                DeliveryDataOut = Model.DeliveryDataOut,
                TtnIn = Model.TtnIn,
                Inn = Model.Inn,
                TradeInSegmentId = Model.TradeInSegmentId,
                Documents = TradeInDocumentItems?.Select(x => new TradeInDocumentEditDto { Id = x.Id, TypeId = x.TypeId }).ToArray()
            };

            return WebClient.ExecuteApiRequestAsync(new UpdateTradeIn(Model.Id, dto));
        }

        protected override bool CanEdit()
        {
            return base.CanEdit()
                   && Model?.StateId != TradeInState.Canceled.Id
                   && Model?.StateId != TradeInState.Completed.Id
                   && WebClient.IsOperationAllowed(BusinessOperation.UpdateTradeIn);
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TradeInViewItem.ClassId):
                    RaisePropertyChanged(nameof(ToolTipClass));
                    break;
                case nameof(TradeInViewItem.ProductId):
                    InitSerialNumbers();
                    RaisePropertyChanged(nameof(OrderFoundProductChanged));
                    break;
                case nameof(TradeInViewItem.Phone):
                    Model.CustomerId = null;
                    CustomerUpdated = false;
                    break;
                case nameof(TradeInViewItem.WarehouseId):
                    OnWarehouseChanged();
                    break;
                case nameof(TradeInViewItem.CarryOutId):
                case nameof(TradeInViewItem.CityOutId):
                    Model.DeliveryDataOut = null;
                    OnWarehouseChanged();
                    break;
            }
        }

        private async Task RefreshCrmAsync()
        {
            List<ClientContactDto> clientContacts = await WebClient.ExecuteApiRequestAsync(new QueryTradeInClientContacts(Model.Id));

            ClientContactsHistoryItems = clientContacts
                .Select(x => Mapper.Map<ClientContactViewItem>(x))
                .OrderByDescending(x => x.Date)
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadTradeInIndicatorValuesAsync()
        {
            List<TradeInIndicatorValueDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryTradeInIndicatorValues());

            if (dtos?.Count > 0)
            {
                TradeInWarrantyValues = dtos.Where(x => x.IndicatorId == TradeInIndicator.Warranty.Id).ToReadOnlyObservableCollection();
                TradeInPackageValues = dtos.Where(x => x.IndicatorId == TradeInIndicator.Package.Id).ToReadOnlyObservableCollection();
                TradeInClassValues = dtos.Where(x => x.IndicatorId == TradeInIndicator.Class.Id)
                    .Select(x => new ComboBoxItem(x.Id, $"{x.Name} ({x.Description})"))
                    .ToReadOnlyObservableCollection();
            }
        }

        private async Task LoadCategoriesAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            if (categories?.Count > 0)
            {
                _allCategories = categories.ToReadOnlyObservableCollection();

                Categories = categories
                    .Where(x => x.UseInTradeIn)
                    .OrderBy(x => x.Position)
                    .ToReadOnlyObservableCollection();
            }
        }

        private async Task LoadWarehousesAsync()
        {
            List<WarehouseDto> warehouseDtos = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouseDtos
                .Where(warehouse => warehouse.Active == 1 && (warehouse.TypeId == WarehouseKind.Pickup.Id || warehouse.TypeId == WarehouseKind.Service.Id))
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees.ToReadOnlyObservableCollection();
        }

        private async Task LoadManufactorsAsync()
        {
            ManufactorsDto manufactorsData = await WebClient.ExecuteApiRequestAsync(new QueryAllManufactors());

            if (manufactorsData.Manufactors?.Any() == true)
            {
                AllManufactors = manufactorsData.Manufactors.ToReadOnlyObservableCollection();
            }
        }

        private async Task LoadDocumentsAsync(int tradeInId)
        {
            List<TradeInDocumentSimpleDto> documentDtos = await WebClient.ExecuteApiRequestAsync(new QueryTradeInDocuments(tradeInId));

            _tradeInDocuments = documentDtos.ToObservableCollection();

            TradeInDocumentItems = documentDtos?
                .Select(x => Mapper.Map<TradeInDocumentViewItem>(x)).ToObservableCollection()
                                   ?? Array.Empty<TradeInDocumentViewItem>().ToObservableCollection();

            DocumentsCount = documentDtos?.Count ?? 0;
        }

        private async Task LoadEDocumentsAsync(int tradeInId)
        {
            await _errorHandler.HandleErrorsAsync(
                async _ => await WebClient.ExecuteApiRequestAsync(new QueryTradeInEDocuments(tradeInId)),
                "получении Trade-In E-документов",
                null,
                this,
                true,
                onSuccess: (documentDtosResult, _) => {
                    _tradeInEDocuments = documentDtosResult.Data.ToObservableCollection();

                    TradeInEDocumentViewItems = documentDtosResult.Data?
                                               .Select(x => Mapper.Map<TradeInEDocumentViewItem>(x)).ToObservableCollection()
                                           ?? Array.Empty<TradeInEDocumentViewItem>().ToObservableCollection();

                    EDocumentsCount = documentDtosResult.Data?.Count ?? 0;

                    return Task.CompletedTask;
                });
        }

        private async Task LoadTradeInCoefAsync()
        {
            List<TradeInCoefDto> coefs = await WebClient.ExecuteApiRequestAsync(new QueryTradeInCoefs());

            _coefs = coefs.ToReadOnlyObservableCollection();
        }

        private async Task LoadCitiesAsync()
        {
            if (Cities == null)
            {
                PagedResult<CityDto> citiesResult = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true);

                Cities = citiesResult.Data
                    .Where(x => x.Active)
                    .OrderBy(x => x.Position)
                    .ThenBy(x => x.Name)
                    .ToReadOnlyObservableCollection();
            }
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            if (Employees == null || Statuses == null || Model == null)
            {
                yield break;
            }

            yield return new SummaryViewItem("Trade-In", Model.Id.ToString());

            if (Model.BitrixId.HasValue)
            {
                yield return new SummaryViewItem("Битрикс", Model.BitrixId.ToString());
            }

            string nameCreatedEmployee = Employees.FirstOrDefault(x => x.Id == Model.CreatedBy)?.Name;

            yield return new SummaryViewItem("Создал", $"{nameCreatedEmployee} ({Model.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");

            if (Model.EvaluatedBy.HasValue)
            {
                string evaluatedEmployeeName = Employees.FirstOrDefault(x => x.Id == Model.EvaluatedBy)?.Name;

                yield return new SummaryViewItem("Оценил", $"{evaluatedEmployeeName} ({Model.EvaluatedOn?.ToString(DateFormattingRules.FullDateTimeFormat)})");
            }

            if (Model.ReceivedBy.HasValue)
            {
                string receivedEmployeeName = Employees.FirstOrDefault(x => x.Id == Model.ReceivedBy)?.Name;

                yield return new SummaryViewItem("Принял", $"{receivedEmployeeName} ({Model.ReceivedOn?.ToString(DateFormattingRules.FullDateTimeFormat)})");
            }

            if (Model.CompletedBy.HasValue)
            {
                string nameCompletedEmployee = Employees.FirstOrDefault(x => x.Id == Model.CompletedBy)?.Name;

                yield return new SummaryViewItem("Завершил", $"{nameCompletedEmployee} ({Model.CompletedOn?.ToString(DateFormattingRules.FullDateTimeFormat)})");
            }

            yield return new SummaryViewItem("Статус", Statuses.FirstOrDefault(x => x.Id == Model.StateId)?.Name);

            yield return new SummaryViewItem("Протестирован", Model?.Tested.ToStringAlt());

            if (Model.BuyoutAmount.HasValue)
            {
                yield return new SummaryViewItem("Предв. оценка", $"{Model.BuyoutAmount.Value:0.0#}");
            }

            if (Model.RealBuyoutAmount > 0)
            {
                yield return new SummaryViewItem("Факт. оценка", $"{Model.RealBuyoutAmount.Value:0.0#}");
            }

            if (Model.ReadyForComplete)
            {
                yield return new SummaryViewItem("Готов к завершению", Model.ReadyForComplete.ToStringAlt());
            }
        }

        private async Task CustomerSearchAsync(string phone)
        {
            if (!string.IsNullOrEmpty(phone))
            {
                CustomerFilteringItem item = new CustomerFilteringItem(phone);

                PagedResult<CustomerDto> result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new QueryCustomers(item)),
                    "получении информации о клиенте",
                    null,
                    this,
                    true,
                    showNotification: false);

                if (result.Data?.Any() == true)
                {
                    CustomerDto customer = result.Data.First();

                    if (!MessageFacadeService.Confirm($"ФИО: {customer.Fio}\nE-mail: {customer.Email}", "Верно?"))
                    {
                        return;
                    }

                    Model.CustomerId = customer.Id;
                    Model.LastName = customer.LastName;
                    Model.FirstName = customer.FirstName;
                    Model.MiddleName = customer.MiddleName;
                    Model.Email = customer.Email;
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Клиент не найден");
                }

                CustomerUpdated = true;
            }
        }

        private async Task SearchOrderAsync(int? orderId)
        {
            if (orderId.HasValue && _order?.Id != orderId)
            {
                _order = await GetOrderAsync(orderId.Value);

                if (_order == null)
                {
                    MessageFacadeService.ShowNotificationWarning($"Заказ № {orderId} не найден");
                    return;
                }

                Model.ProductId = null;
                Model.Name = Model.NameUkr = Model.NameEn = null;
                SerialNumbers = null;

                await RefreshProductsAsync(_order);

                MessageFacadeService.ShowMessageBoxInfo($"Заказ №{orderId} добавлен успешно. Выберите товар из выпадающего списка.");
            }
        }

        private void RemoveOrder()
        {
            Model.OrderId = null;
            Model.ProductId = null;
            Model.Name = Model.NameUkr = Model.NameEn = null;
            Model.OrderProductId = null;
            Model.CategoryId = null;
            Model.ModelOrPn = null;
            Model.SerialNumber = null;
            Model.Brand = null;
            SelectedCategory = null;
            SerialNumbers = null;
            Products = null;
            _products = null;
            _order = null;
            _productSerials = null;
        }

        private async Task<OrderDto> GetOrderAsync(int orderId)
        {
            OrderDto order = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId)),
                "получении заказа",
                null,
                this,
                true,
                showNotification: false,
                showDialog: false,
                showError: false);

            return order;
        }

        private async Task EvaluateRequestAsync()
        {
            await ExecuteLockableOperationAsync(
                async lockEntity =>
                {
                    await EvaluateAsync(lockEntity);
                });
        }

        private async Task EvaluateAsync(TradeInDto tradeIn)
        {
            decimal? maxPrice = tradeIn.ProductId.HasValue ? _tradeInMaxPrice?.MaxPrice : tradeIn.TradeInMaxPrice;
            DateTime? lastDateMaxPrice = tradeIn.ProductId.HasValue ? _tradeInMaxPrice?.CreatedOn : tradeIn.LastDateTradeInMaxPrice;

            TradeInEvaluateParameter parameter = new TradeInEvaluateParameter(tradeIn.Id, tradeIn.Brand, tradeIn.ModelOrPn, maxPrice, lastDateMaxPrice);

            TradeInEvaluateViewModel viewModel = DialogDocumentManagerService.ShowView<TradeInEvaluateViewModel>(parameter, this);

            if (viewModel.IsOk)
            {
                EvaluateTradeInDto evaluateTradeInDto = new EvaluateTradeInDto()
                {
                    Id = tradeIn.Id,
                    MaxPrice = viewModel.MaxEvaluate!.Value,
                    NotifyClient = viewModel.NotifyClient
                };

                Result<TradeInDto> result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new EvaluateTradeIn(evaluateTradeInDto)),
                    "оценке товара",
                    "Oценка товара выполнена",
                    this,
                    true);

                if (result?.IsSuccess == true)
                {
                    Messenger.Send(new TradeInMessage(result.Data, MessageType.Changed));

                    SetData(result.Data);

                    if (result.Data.ProductId.HasValue)
                    {
                        _tradeInMaxPrice = await GetProductMaxPriceAsync(result.Data.ProductId.Value);
                    }
                }
            }
        }

        private bool CanEvaluate()
        {
            return Model != null
                   && Model.EmployeeLockId == null
                   && Model.StateId == TradeInState.New.Id
                   && WebClient.IsOperationAllowed(BusinessOperation.EvaluateTradeIn);
        }

        private bool CanOverEvaluate()
        {
            return Model != null
                   && Model.EmployeeLockId == null
                   && (Model.StateId == TradeInState.Evaluated.Id || Model.StateId == TradeInState.Received.Id);
        }

        private bool CanSearchClient(string phone)
        {
            return !string.IsNullOrEmpty(phone)
                   && IsLockedByCurrentEmployee
                   && Model.StateId != TradeInState.Canceled.Id
                   && Model.StateId != TradeInState.Completed.Id;
        }

        private bool CanSearchOrder(int? orderId)
        {
            return orderId.HasValue
                   && IsLockedByCurrentEmployee
                   && Model.StateId == TradeInState.New.Id;
        }

        private bool CanRemoveOrder()
        {
            return Model != null
                   && Model.OrderId.HasValue
                   && IsLockedByCurrentEmployee
                   && Model.StateId == TradeInState.New.Id;
        }

        private bool CanChangeProduct()
        {
            return IsLockedByCurrentEmployee && Model.StateId == TradeInState.New.Id;
        }

        private async Task CreateTaskAsync()
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Комментарий",
                "Комментарий к задаче",
                @"^.{3,1000}$",
                errorMessage: "Длина должна быть в диапазоне 3..1000 символов",
                isMultiline: true,
                required: false);

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (!fromUserViewModel.IsOk)
            {
                return;
            }

            Result<TradeInDto> resultDto = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new TradeInNomenclatureTask(Model.Id, new TradeInCreateNomenclatureTaskDto(fromUserViewModel.Content))),
                "при создании задачи",
                "Задача создана",
                this,
                false);

            if (resultDto?.IsSuccess != true)
            {
                return;
            }

            Model.BitrixId = resultDto.Data.BitrixId;

            SummaryItems = GetSummaryItems();
        }

        private bool CanCreateTask()
        {
            return Model != null
                   && Model.ProductId == null
                   && Model.EmployeeLockId == null
                   && (Model.StateId == TradeInState.Evaluated.Id || Model.StateId == TradeInState.Received.Id)
                   && WebClient.IsOperationAllowed(BusinessOperation.CreateTradeInNomenclatureTask);
        }

        private async Task ОverEvaluateRequestAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            Result<TradeInDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new ОverEvaluate(Model.Id)),
                "при переоценке",
                "Переоценка выполнена",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                Messenger.Send(new TradeInMessage(result.Data, MessageType.Changed));
                SetData(result.Data);
            }
        }

        private async Task CompleteRequestAsync()
        {
            if (Model.WarehouseId is null)
            {
                MessageFacadeService.ShowNotificationError("Склад должен быть заполнен");
                return;
            }

            if (Model.ProductId is null)
            {
                MessageFacadeService.ShowNotificationError("Код нашего товара не заполнен");
                return;
            }

            if (!Model.Tested && !MessageFacadeService.Confirm("Бонусы будут начислены только после тестирование сервисом.Вы уверены, что хотите завершить заявку без тестирования?"))
            {
                return;
            }

            Result<TradeInDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CompleteTradeIn(Model.Id)),
                "завершении",
                "Заявка завершена",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                Messenger.Send(new TradeInMessage(result.Data, MessageType.Changed));
                CloseOk();
            }
        }

        private bool CanComplete()
        {
            return Model != null
                   && WebClient.IsOperationAllowed(BusinessOperation.CompleteTradeIn)
                   && Model.EmployeeLockId == null
                   && Model.StateId == TradeInState.Received.Id;
        }

        private async Task CancelRequestAsync()
        {
            TradeInCancelReasonViewModel viewModel = DialogDocumentManagerService.ShowView<TradeInCancelReasonViewModel>(null, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            string trackNumber = null;

            if ((Model.CarryOutId ?? CarryType.PickupId) != CarryType.PickupId && (Model.StateId == TradeInState.Evaluated.Id
                                                 || Model.StateId == TradeInState.Completed.Id
                                                 || Model.StateId == TradeInState.Received.Id))
            {
                TradeInCreateTtnViewModel tradeInCreateTtnViewModel = DialogDocumentManagerService.ShowView<TradeInCreateTtnViewModel>(Model.Id, this);

                if (!tradeInCreateTtnViewModel.IsOk)
                {
                    return;
                }

                trackNumber = tradeInCreateTtnViewModel.TrackNumber;
            }

            TradeInCancelDto cancelDto = new TradeInCancelDto()
            {
                Id = Model.Id,
                CancelReasonId = viewModel.ReasonCancelType.Id,
                TrackNumber = trackNumber
            };

            Result<TradeInDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CancelTradeIn(cancelDto)),
                "при отмене Trade-In заявки",
                "Отмена Trade-In заявки выполнена",
                this,
                false);

            if (result?.IsSuccess == true)
            {
                Messenger.Send(new TradeInMessage(result.Data, MessageType.Changed));

                SetData(result.Data);
            }
        }

        private bool CanCancel()
        {
            return Model != null
                   && Model.EmployeeLockId == null
                   && Model.StateId != TradeInState.Canceled.Id
                   && Model.StateId != TradeInState.Completed.Id
                   && WebClient.IsOperationAllowed(BusinessOperation.CancelTradeIn);
        }

        private async Task TestRequestAsync()
        {
            if (Model.WarehouseId is null)
            {
                MessageFacadeService.ShowNotificationError("Склад должен быть заполнен");
                return;
            }

            TradeInTestParameter parameter = new TradeInTestParameter(Model.Id, Model.Tested);

            TradeInTestViewModel viewModel =
                DialogDocumentManagerService.ShowView<TradeInTestViewModel>(parameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Result<TradeInDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new TestTradeIn(Model.Id, viewModel.Tested)),
                " при выполнении сохранения",
                "Сохранение выполнено",
                this,
                false);

            if (result?.IsSuccess == true)
            {
                Messenger.Send(new TradeInMessage(result.Data, MessageType.Changed));
                SetData(result.Data);
            }
        }

        private bool CanTest()
        {
            return Model != null
                   && Model.EmployeeLockId == null
                   && Model.StateId == TradeInState.Received.Id
                   && WebClient.IsOperationAllowed(BusinessOperation.TestTradeIn);
        }

        private void RemoveProduct()
        {
            Model.ProductId = null;
            Model.Name = Model.NameUkr = Model.NameEn = null;
            Model.CategoryId = null;
            SelectedCategory = null;
            Model.Brand = null;
            Model.ModelOrPn = null;
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

                if (Categories.All(x => product.ParentCategoryId != x.Id))
                {
                    MessageFacadeService.ShowNotificationError("В категории для товара не включен признак использования в Trade-In");
                    return;
                }

                if (product.TypeId == ProductType.TradeInId)
                {
                    MessageFacadeService.ShowNotificationError("Запрещено выбирать товар типа Trade-In");
                    return;
                }

                Model.ProductId = product.Id;
                Model.TradeInSegmentId = product.TradeInSegmentId;
                Model.TradeInSegmentName = product.TradeInSegmentName;
                Model.Name = product.Name;
                Model.NameUkr = product.NameUkr;
                Model.NameEn = product.NameEn;
                Model.CategoryId = product.ParentCategoryId;
                SelectedCategory = _allCategories.FirstOrDefault(x => x.Id == Model.CategoryId);
                Model.Brand = product.Manufactor;
                Model.ModelOrPn = product.ProductPn;
            }
        }

        private void ChangedCategory()
        {
            if (SelectedCategory != null)
            {
                Model.CategoryId = SelectedCategory.Id;

                CurrentCategoryManufactors = AllManufactors
                    .Where(x => x.CategoryId == SelectedCategory.Id)
                    .Select(x => x.Manufactor)
                    .ToReadOnlyObservableCollection();
            }
        }

        private void ProductEditValueChanged(EditValueChangedEventArgs eventArgs)
        {
            if (IsLockedByCurrentEmployee)
            {
                if (Model?.ProductId.HasValue == true && _order != null)
                {
                    OrderProductDto orderProductDto = _order.Products.FirstOrDefault(x => x.Product.Id == Model.ProductId);
                    Model.OrderProductId = orderProductDto?.Id;
                    ProductDto productDto = _products?.FirstOrDefault(x => x.Id == orderProductDto?.Product.Id);
                    Model.CategoryId = productDto?.ParentCategoryId;
                    SelectedCategory = _allCategories?.FirstOrDefault(x => x.Id == Model.CategoryId);
                    Model.Brand = productDto?.Manufactor;
                }

                UpdateMaxPrice();
            }
        }

        private void OrderIdChanged(EditValueChangedEventArgs eventArgs)
        {
            if (Model.OrderId == null)
            {
                _products = null;
                _order = null;
                Model.ProductId = ModelOriginal.ProductId;
                Model.Name = ModelOriginal.Name;
                Model.NameUkr = ModelOriginal.NameUkr;
                Model.NameEn = ModelOriginal.NameEn;
                Model.Brand = ModelOriginal.Brand;
                Model.CategoryId = ModelOriginal.CategoryId;
                SelectedCategory = _allCategories?.FirstOrDefault(x => x.Id == Model.CategoryId);
            }
        }

        private async Task RefreshDocumentsAsync()
        {
            await LoadDocumentsAsync(Model.Id);
        }

        private async Task RefreshComplaintsAsync()
        {
            Complaints = null;

            try
            {
                IFilteringItem filteringItem = new ComplaintsFilteringItem { TradeInIds = Model.Id.ToString() };

                List<ComplaintDto> complaints = await WebClient.ExecuteApiRequestAsync(new QueryComplaints(filteringItem)).GetPagedResultDataAsync();
                Complaints = complaints
                    .Select(x => Mapper.Map<ComplaintViewItem>(x))
                    .ToObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get service request complaints");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void AddDocument()
        {
            const int MaxDocumentsCount = 25;
            const int MaxFileLengthMb = 10;

            if (OpenFileDialogService.ShowDialog())
            {
                if (OpenFileDialogService.Files.Count() + TradeInDocumentItems.Count > MaxDocumentsCount)
                {
                    MessageFacadeService.ShowNotificationWarning($"К оценке можно добавить не больше чем {MaxDocumentsCount} файлов");
                    return;
                }

                if (!OpenFileDialogService.Files.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Выберите хотя бы 1 файл");
                    return;
                }

                List<IFileInfo> notValidFiles = OpenFileDialogService.Files.Where(x => x.Length > MaxFileLengthMb.Megabytes().Bytes).ToList();

                if (notValidFiles.Any())
                {
                    ShowValidationResultView(
                        "Ошибки при добавлении файлов",
                        notValidFiles.Select(x => new ValidationResultItem($"Файл \"{x.GetFullName()}\" должен быть меньше {MaxFileLengthMb} MB", true)).ToArray());

                    return;
                }

                AddDocumentsParameter parameter = new AddDocumentsParameter(
                    Model.Id,
                    OpenFileDialogService.Files.Select(x => x.GetFullName()).ToList());

                TradeInAddDocumentViewModel viewModel = new TradeInAddDocumentViewModel(
                    WebClient,
                    Dictionaries,
                    MessageFacadeService,
                    Messenger,
                    _errorHandler);

                NonModalSizeableDialogDocumentManagerService.ShowView("AddDocumentsView", viewModel, parameter, this);
            }
        }

        private async Task RemoveDocumentAsync(TradeInDocumentViewItem item)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteTradeInDocument(item.Id));

                TradeInDocumentSimpleDto removingDocument = _tradeInDocuments.FirstOrDefault(x => x.Id == item.Id);
                _tradeInDocuments.Remove(removingDocument);

                TradeInDocumentItems.Remove(item);
                DocumentsCount = TradeInDocumentItems.Count;

                MessageFacadeService.ShowNotificationInfo("Документ удален успешно");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to delete Trade-In documents");

                MessageFacadeService.ShowNotificationError("Ошибка при удалении документа");
            }
        }

        private async Task PrintDocumentAsync(TradeInDocumentViewItem item)
        {
            TradeInDocumentDto document = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryTradeInDocument(item.Id)),
                "получении документов",
                null,
                this,
                true,
                false);

            await FileHelper.OpenAsFileAsync(document.Data, document.Ext);
        }

        private async Task RefreshEDocumentsAsync()
        {
            await LoadEDocumentsAsync(Model.Id);
        }

        private async Task AddEDocumentAsync()
        {
            byte[] pdf = await TradeInReportPrinter.GetTradeInActInBytesAsync(
                Model,
                TradeInWarrantyValues,
                TradeInPackageValues,
                _allCategories.Select(x => new ComboBoxItem(x.Id, x.NameUkr)).ToArray(),
                WebClient);

            Result<TradeInEDocumentDto> document = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateTradeInEDocument(Model.Id, pdf)),
                "создании документа",
                "Документ создан",
                this,
                true,
                true);

            if (document.IsSuccess)
            {
                _tradeInEDocuments.Add(document.Data);

                TradeInEDocumentViewItem item = Mapper.Map<TradeInEDocumentViewItem>(document.Data);

                TradeInEDocumentViewItems.Add(item);

                EDocumentsCount = TradeInEDocumentViewItems.Count;
            }
        }

        private async Task RemoveEDocumentAsync(TradeInEDocumentViewItem item)
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите удалить документ?"))
            {
                return;
            }

            await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new DeleteTradeInEDocument(item.Id)),
                "удалении документа",
                "Документ удален",
                this,
                true,
                true,
                onSuccess: (_, _) =>
                {
                    TradeInEDocumentSimpleDto removingDocument = _tradeInEDocuments.FirstOrDefault(x => x.Id == item.Id);
                    _tradeInEDocuments.Remove(removingDocument);

                    TradeInEDocumentViewItems.Remove(item);
                    EDocumentsCount = TradeInEDocumentViewItems.Count;

                    return Task.CompletedTask;
                });
        }

        private async Task PrintEDocumentAsync(TradeInEDocumentViewItem item)
        {
            TradeInEDocumentDto document = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new QueryTradeInEDocument(item.Id)),
                    "получении документов",
                    null,
                    this,
                    true,
                    false);

            await FileHelper.OpenAsFileAsync(document.Bytes, "pdf");
        }

        private async Task SendEDocumentToClientAsync(TradeInEDocumentViewItem item)
        {
            await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new SendTradeInEDocument(item.Id)),
                "создании сообщения",
                "Сообщение создано(добавлено в очередь на отправку соглавно рабочему графику)",
                this,
                true,
                false,
                onSuccess: (x, _) =>
                {
                    item.SendBy = x.Data.SendBy;
                    item.SendOn = x.Data.SendOn;

                    return Task.CompletedTask;
                });

            await RefreshEDocumentsAsync();
        }

        private async Task SignEDocumentByTelemartAsync(TradeInEDocumentViewItem item)
        {
            await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new SignTradeInEDocument(item.Id)),
                "подписи документа",
                "Документ подписан",
                this,
                true,
                false);

            await RefreshEDocumentsAsync();
        }

        private void OnDocumentMessage(TradeInCreateDocumentMessage message)
        {
            if (message.Entity.TradeInId == Model.Id)
            {
                switch (message.MessageType)
                {
                    case MessageType.Added:
                        _tradeInDocuments.Add(message.Entity);
                        TradeInDocumentItems?.Add(Mapper.Map<TradeInDocumentViewItem>(message.Entity));
                        DocumentsCount = TradeInDocumentItems?.Count;
                        break;
                }
            }
        }

        private void OnComplaintMessage(ComplaintMessage message)
        {
            ComplaintDto dto = message.Entity;

            if (dto.TradeInId == Model.Id)
            {
                switch (message.MessageType)
                {
                    case MessageType.Added:
                        Complaints?.Add(Mapper.Map<ComplaintViewItem>(dto));
                        break;
                    case MessageType.Changed:
                        Complaints?.DoActionWithItem(x => x.Id == dto.Id, x => Mapper.Map(dto, x));
                        break;
                }
            }

            Model.ComplaintsCount = Complaints?.Count;
            Model.NewComplaintsCount = Complaints?.Count(x => x.State == ComplaintState.New);
        }

        private async Task RefreshProductsAsync(OrderDto orderDto)
        {
            if (orderDto == null)
            {
                return;
            }

            Products = orderDto.Products
                .DistinctBy(x => x.Product.Id)
                .Where(x => Categories.Any(z => z.Id == x.Product.ParentCategoryId))
                .Select(x => new ComboBoxItem(x.Product.Id, x.Product.NameFullRu))
                .ToReadOnlyObservableCollection();

            int[] productIds = Products.Select(x => x.Id).ToArray();

            List<ProductDto> products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(_order.ClientId, productIds));

            _products = products?.ToReadOnlyObservableCollection();

            SetSerialNumbers(orderDto.Products);
        }

        private void AddComplaint()
        {
            ComplaintCreateParameter parameter = new ComplaintCreateParameter(
                null,
                null,
                Model.Id,
                null,
                new[] { new ComboBoxItem(Model.ProductId ?? 0, Model.ProductName) },
                Model.Fio,
                Model.Phone,
                null,
                Model.Email);

            NonModalDialogDocumentManagerService.ShowView<ComplaintCreateViewModel>(parameter, this);
        }

        private decimal? CalculateRealBuyoutAmount(decimal? maxPrice)
        {
            if (maxPrice == null || maxPrice <= 0)
            {
                return null;
            }

            decimal coefPack = _coefs?.FirstOrDefault(
                x => x.CategoryId == Model.CategoryId && x.IndicatorValueId == Model.PackId)?.Coef ?? 0;

            decimal coefClass = _coefs?.FirstOrDefault(
                x => x.CategoryId == Model.CategoryId && x.IndicatorValueId == Model.ClassId)?.Coef ?? 0;

            decimal coefWarranty = _coefs?.FirstOrDefault(
                x => x.CategoryId == Model.CategoryId && x.IndicatorValueId == Model.WarrantyId)?.Coef ?? 0;

            return Math.Round(maxPrice.Value * coefClass * coefPack * coefWarranty);
        }

        private void HandleSelectionChanged(ValueChangedEventArgs<FrameworkElement> e)
        {
            switch (e.NewValue.Name)
            {
                case "CrmTab":
                    if (ClientContactsHistoryItems == null)
                    {
                        RefreshCrmCommand.Execute(null);
                    }

                    break;

                case "ComplaintsTab":
                    if (Complaints == null)
                    {
                        RefreshComplaintsCommand.Execute(null);
                    }

                    break;
            }
        }

        private void ReCalculate()
        {
            if (IsLockedByCurrentEmployee)
            {
                decimal? maxPrice = Model.ProductId.HasValue ? _tradeInMaxPrice?.MaxPrice : Model.TradeInMaxPrice;

                Model.RealBuyoutAmount = CalculateRealBuyoutAmount(maxPrice);

                SummaryItems = GetSummaryItems();
            }
        }

        private bool CanShowPhoneHistory(string phone)
        {
            return !string.IsNullOrWhiteSpace(phone);
        }

        private void ShowPhoneHistory(string phone)
        {
            PhoneHistoryViewMessage message = new PhoneHistoryViewMessage(phone);

            Messenger.Send(message);
        }

        private void SetSerialNumbers(IReadOnlyCollection<OrderProductDto> orderProducts)
        {
            _productSerials = orderProducts
                .GroupBy(x => x.Product.Id)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyCollection<OrderProductSnDto>)x
                        .SelectMany(z => z.SerialNumbers)
                        .ToArray());
        }

        private void OpenServiceRequest(int? serviceRequestId)
        {
            Messenger.Send(new ServiceRequestViewMessage(serviceRequestId!.Value));
        }

        private async Task ReceiveAsync()
        {
            Result<TradeInDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new ReceiveTradeIn(Model.Id)),
                "при принятии Trade-In заявки",
                "Trade-In заявка принята",
                this,
                false);

            if (result?.IsSuccess == true)
            {
                Messenger.Send(new TradeInMessage(result.Data, MessageType.Changed));
                CloseOk();
            }
        }

        private async Task PrintActAsync()
        {
            await TradeInReportPrinter.PrintActAsync(
                Model,
                TradeInWarrantyValues,
                TradeInPackageValues,
                _allCategories.Select(x => new ComboBoxItem(x.Id, x.NameUkr)).ToArray(),
                WebClient);
        }

        private async Task<TradeInMaxPriceDto> GetProductMaxPriceAsync(int productId)
        {
           Result<TradeInMaxPriceDto> maxPriceResult = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryTradeInMaxPrice(new TradeInMaxPriceGetDto(productId))),
                null,
                null,
                this,
                false,
                showNotification: false,
                showDialog: false,
                showError: false);

           return maxPriceResult?.Data;
        }

        private void UpdateMaxPrice()
        {
            if (Model.ProductId == null)
            {
                _tradeInMaxPrice = null;

                ReCalculate();

                return;
            }

            Task.Factory.StartNew(
                async p =>
                {
                    int productId = (int)p;

                    try
                    {
                        Result<TradeInMaxPriceDto> maxPriceResult = await WebClient.ExecuteApiRequestAsync(new QueryTradeInMaxPrice(new TradeInMaxPriceGetDto(productId)));

                        if (maxPriceResult.IsSuccess)
                        {
                            await DispatcherService.BeginInvoke(
                                () =>
                                {
                                    _tradeInMaxPrice = maxPriceResult.Data;

                                    ReCalculate();
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"Failed getting trade-in max price product {productId}");
                    }
                },
                Model.ProductId.Value,
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void HandleDocumentsChanged()
        {
            Model.IsDocumentTypeChanged = TradeInDocumentItems.Any(x => _tradeInDocuments.Any(y => y.Id == x.Id && y.TypeId != x.TypeId));
        }

        private void ShowDocumentsBotQr()
        {
            QrCodeParameter parameter = new QrCodeParameter(DocumentsBotHelper.GetUrl(Model.Id, Entity.TradeInId, Dictionaries, _telegramBotOptions), "Telegram бот");

            DialogDocumentManagerService.ShowView<QrCodeViewModel>(parameter, this);
        }

        private void InitSerialNumbers()
        {
            if (_productSerials?.TryGetValue(Model?.ProductId ?? 0, out IReadOnlyCollection<OrderProductSnDto> serials) == true)
            {
                SerialNumbers = serials.ToReadOnlyObservableCollection();
            }
            else
            {
                SerialNumbers = null;
            }

            if (Model != null)
            {
                Model.NeedSerialNumber = SerialNumbers?.Any() == true;
            }
        }
        private void OnWarehouseChanged()
        {
            if (Model.CarryOutId != CarryType.PickupId)
            {
                return;
            }

            WarehouseDto warehouse = Warehouses.FirstOrDefault(x => x.Id == Model.WarehouseId);

            if (warehouse != null)
            {
                Model.DeliveryDataOut = new DeliveryDataDto
                {
                    CityId = warehouse.CityId.ToString(),
                    PlaceId = warehouse.Id.ToString(),
                    Street = null,
                    House = null,
                    Flat = null,
                    Extra = null,
                    MaxAllowedWeight = warehouse.MaxPackageWeight,
                    Address = warehouse.Address,
                    AddressUkr = warehouse.AddressUa,
                    AddressEn = warehouse.AddressEn
                };
            }
            else
            {
                Model.DeliveryDataOut = null;
            }
        }

        private void SelectDeliveryAddress()
        {
            if (Model.CityOutId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите город");
                return;
            }

            if (Model.CarryOutId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите способ доставки");
                return;
            }

            CarryType carryType = Dictionaries.GetItemById<CarryType>(Model.CarryOutId.Value);

            SelectDeliveryDataParameter parameter = new SelectDeliveryDataParameter(
                carryType.Id,
                Model.CityOutId.Value,
                Model.DeliveryDataOut?.Address,
                Model.DeliveryDataOut);

            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)App.Current.MainWindow.DataContext;

            if (carryType.Kind == CarryTypeKind.Courier)
            {
                SelectDeliveryAddressViewModel viewModel = mainWindowViewModel.WorkspaceViewModel.DialogDocumentManagerService.ShowView<SelectDeliveryAddressViewModel>(
                    parameter,
                    mainWindowViewModel);

                if (viewModel.IsOk)
                {
                    Model.DeliveryDataOut = viewModel.GetDeliveryServiceData();
                }
            }
            else if (carryType.Kind == CarryTypeKind.Pickup)
            {
                SelectDeliveryWarehouseViewModel viewModel = mainWindowViewModel.WorkspaceViewModel.SizeableDialogDocumentManagerService.ShowView<SelectDeliveryWarehouseViewModel>(parameter, mainWindowViewModel);

                if (viewModel.IsOk)
                {
                    Model.DeliveryDataOut = viewModel.GetDeliveryServiceData();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}