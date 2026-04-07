using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Validation;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Customer;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Customer;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Common.Constants;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.TradeIn
{
    public class TradeInCreateViewModel : TelemartDialogViewModelBase
    {
        private readonly IMessenger _messenger;
        private readonly IErrorHandler _errorHandler;
        private readonly IMapper _mapper;
        private readonly Dictionary<int, List<OrderProductSnDto>> _productSerials = new Dictionary<int, List<OrderProductSnDto>>();

        private ReadOnlyObservableCollection<CategoryDto> _allCategories;
        private OrderDto _order;
        private ReadOnlyObservableCollection<ProductDto> _products;
        private CustomerDto _customer;

        public TradeInCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IErrorHandler errorHandler,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            CustomerSearchCommand = new AsyncCommand<string>(CustomerSearchAsync, x => !string.IsNullOrEmpty(x));
            SearchOrderCommand = new AsyncCommand(SearchOrderAsync, () => OrderId.HasValue);
            RemoveOrderCommand = new DelegateCommand(RemoveOrder);
            RemoveProductCommand = new DelegateCommand(RemoveProduct, () => ProductId.HasValue);
            SelectProductCommand = new DelegateCommand(SelectProduct);
            ShowPhoneHistoryClientCommand = new DelegateCommand<string>(ShowPhoneHistory, CanShowPhoneHistory);
            SelectDeliveryAddressCommand = new DelegateCommand(SelectDeliveryAddress, () => CarryId != null && CarryId != CarryType.PickupId);
        }

        #region INPC

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value, OnPhoneNumberChanged); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public int? CustomerId
        {
            get { return GetProperty(() => CustomerId); }
            set { SetProperty(() => CustomerId, value); }
        }

        public string FirstName
        {
            get { return GetProperty(() => FirstName); }
            set { SetProperty(() => FirstName, value); }
        }

        public string LastName
        {
            get { return GetProperty(() => LastName); }
            set { SetProperty(() => LastName, value); }
        }

        public string MiddleName
        {
            get { return GetProperty(() => MiddleName); }
            set { SetProperty(() => MiddleName, value); }
        }

        public string Inn
        {
            get { return GetProperty(() => Inn); }
            set { SetProperty(() => Inn, value); }
        }

        public bool BoughtInTelemart
        {
            get { return GetProperty(() => BoughtInTelemart); }
            set { SetProperty(() => BoughtInTelemart, value, BoughtInTelemartChanged); }
        }

        public int? OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value, () => RaisePropertiesChanged(nameof(OrderFound))); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value, OnProductChanged); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int? OrderProductId
        {
            get { return GetProperty(() => OrderProductId); }
            set { SetProperty(() => OrderProductId, value); }
        }

        public int? TradeInSegmentId
        {
            get { return GetProperty(() => TradeInSegmentId); }
            private set { SetProperty(() => TradeInSegmentId, value); }
        }

        public string TradeInSegmentName
        {
            get { return GetProperty(() => TradeInSegmentName); }
            set { SetProperty(() => TradeInSegmentName, value); }
        }

        public CategoryDto SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value, ChangedCategory); }
        }

        public string Brand
        {
            get { return GetProperty(() => Brand); }
            set { SetProperty(() => Brand, value); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public string ModelOrPn
        {
            get { return GetProperty(() => ModelOrPn); }
            set { SetProperty(() => ModelOrPn, value); }
        }

        public int? ClassId
        {
            get { return GetProperty(() => ClassId); }
            set { SetProperty(() => ClassId, value, () => RaisePropertiesChanged(nameof(ToolTipClass))); }
        }

        public int? WarrantyId
        {
            get { return GetProperty(() => WarrantyId); }
            set { SetProperty(() => WarrantyId, value); }
        }

        public int? PackId
        {
            get { return GetProperty(() => PackId); }
            set { SetProperty(() => PackId, value); }
        }

        public WarehouseDto Warehouse
        {
            get { return GetProperty(() => Warehouse); }
            set { SetProperty(() => Warehouse, value, () => OnWarehouseChanged()); }
        }

        public string CustomerDescription
        {
            get { return GetProperty(() => CustomerDescription); }
            set { SetProperty(() => CustomerDescription, value); }
        }

        public string CustomerComment
        {
            get { return GetProperty(() => CustomerComment); }
            set { SetProperty(() => CustomerComment, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> TradeInClassValues
        {
            get { return GetProperty(() => TradeInClassValues); }
            set { SetProperty(() => TradeInClassValues, value); }
        }

        public ReadOnlyObservableCollection<TradeInIndicatorValueDto> TradeInWarrantyValues
        {
            get { return GetProperty(() => TradeInWarrantyValues); }
            set { SetProperty(() => TradeInWarrantyValues, value); }
        }

        public ReadOnlyObservableCollection<TradeInIndicatorValueDto> TradeInPackageValues
        {
            get { return GetProperty(() => TradeInPackageValues); }
            set { SetProperty(() => TradeInPackageValues, value); }
        }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
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

        public IEnumerable<IDictionaryItem> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            set { SetProperty(() => CarryTypes, value); }
        }

        public ReadOnlyObservableCollection<IDictionaryItem> Cities
        {
            get { return GetProperty(() => Cities); }
            set { SetProperty(() => Cities, value); }
        }

        public string ToolTipClass => ClassId.HasValue
            ? TradeInClassValues.FirstOrDefault(x => x.Id == ClassId).DisplayValue
            : string.Empty;

        public int? CarryId
        {
            get { return GetProperty(() => CarryId); }
            set { SetProperty(() => CarryId, value, CarryIdChanged); }
        }

        public int? CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value); }
        }

        public DeliveryDataDto DeliveryData
        {
            get { return GetProperty(() => DeliveryData); }
            set { SetProperty(() => DeliveryData, value); }
        }

        public int TradeInId { get; private set; }

        public bool CustomerUpdated { get; private set; }

        public bool CopyCustomer { get; protected set; }

        public bool OrderFound => OrderId.HasValue;

        public bool OrderFoundProductChanged => OrderFound && SerialNumbers?.Count >= 1;

        public bool IsNpWarehouseCarryType => CarryId == CarryType.NpWarehouseId;

        #endregion

        #region Commands

        public IAsyncCommand CustomerSearchCommand { get; }

        public IAsyncCommand SearchOrderCommand { get; }

        public IDelegateCommand RemoveOrderCommand { get; }

        public IDelegateCommand RemoveProductCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        public IDelegateCommand ShowPhoneHistoryClientCommand { get; }

        public IDelegateCommand SelectDeliveryAddressCommand { get; }

        #endregion

        public override int MinWidth => 400;

        public override int Height => 600;

        public static void BuildMetadata(MetadataBuilder<TradeInCreateViewModel> builder)
        {
            builder.Property(x => x.Phone).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.LastName)
                .MaxLength(20, () => "Значение не может быть длиннее 20 символов")
                .ApplyClientNameRusUkrValidationRules()
                .MatchesInstanceRule((x, y) => !string.IsNullOrWhiteSpace(x) || y.CarryId != CarryType.NpWarehouseId, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.MiddleName)
                .MaxLength(20, () => "Значение не может быть длиннее 20 символов")
                .ApplyClientNameRusUkrValidationRules()
                .MatchesInstanceRule((x, y) => !string.IsNullOrWhiteSpace(x) || y.CarryId != CarryType.NpWarehouseId, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.FirstName)
                .MaxLength(20, () => "Значение не может быть длиннее 20 символов")
                .ApplyClientNameRusUkrValidationRules()
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ModelOrPn).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.OrderId).MatchesInstanceRule((x, y) => !y.BoughtInTelemart || x.HasValue, () => "Введите номер заказа");
            builder.Property(x => x.ProductId).MatchesInstanceRule((x, y) => !y.BoughtInTelemart || x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedCategory).MatchesInstanceRule((x, y) => y.BoughtInTelemart || x != null, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.ClassId).MatchesRule(x => x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.PackId).MatchesRule(x => x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Warehouse).MatchesRule(x => x != null, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.DeliveryData).MatchesRule(x => x != null, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.WarrantyId).MatchesRule(x => x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.SerialNumber).MatchesInstanceRule((x, y) => y.SerialNumbers?.Any() != true || !string.IsNullOrEmpty(x), () => "Поле обязательно к заполнению")
                .MatchesRule(x => x is null || !x.Contains(' '), () => "SN не должен содержать пробелы");
            builder.Property(x => x.Email).MatchesRegularExpression(RegexConstants.EmailRegex, () => Resources.TradeInViewModel_Email);
        }

        protected override async Task HandleLoadedAsync()
        {
            CreateTradeInParameter parameter = Parameter as CreateTradeInParameter;

            CarryTypes = Dictionaries.GetItems<CarryType>()
                .Where(x => x.Id is CarryType.PickupId or CarryType.NpWarehouseId or CarryType.NpDeliveryId or CarryType.NpPostBoxId)
                .Cast<IDictionaryItem>()
                .ToReadOnlyObservableCollection();

            await Task.WhenAll(
                LoadCategoriesAsync(),
                LoadTradeInIndicatorValuesAsync(),
                LoadWarehousesAsync(),
                LoadManufactorsAsync(),
                LoadCitiesAsync());

            if (parameter != null)
            {
                await InitPropertiesAsync(parameter.TradeInCopy);
            }

            CustomerUpdated = true;
            CopyCustomer = parameter != null;

            Title = "Создание заявки на Trade-In";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (CustomerUpdated == false)
            {
                MessageFacadeService.ShowMessageBoxWarning("Номер телефона был изменен. Для актуализации данных клиента нажмите кнопку \"Найти клиента по номеру\"");
                return;
            }

            TradeInCreateDto createDto = new TradeInCreateDto()
            {
                CustomerId = CustomerId,
                OrderId = OrderId,
                OrderProductId = OrderProductId,
                ProductId = ProductId,
                Phone = Phone,
                FirstName = FirstName,
                LastName = LastName,
                MiddleName = MiddleName,
                WarehouseId = Warehouse.Id,
                CategoryId = SelectedCategory.Id,
                ClassId = ClassId!.Value,
                WarrantyId = WarrantyId!.Value,
                PackId = PackId!.Value,
                ModelOrPn = ModelOrPn,
                CustomerComment = CustomerComment,
                CustomerDescription = CustomerDescription,
                Comment = Comment,
                SerialNumber = SerialNumber,
                Email = Email,
                Brand = Brand,
                CityId = CityId,
                CarryId = CarryId,
                DeliveryDataOut = DeliveryData,
                Inn = Inn,
                TradeInSegmentId = TradeInSegmentId
            };

            Result<TradeInDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateTradeIn(createDto)),
                "создании Trade-In заявки",
                "Trade-In заявка создана",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                TradeInId = result.Data.Id;

                _messenger.Send(new TradeInMessage(result.Data, MessageType.Added));

                CloseOk();
            }
        }

        private async Task LoadTradeInIndicatorValuesAsync()
        {
            List<TradeInIndicatorValueDto> dtos = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryTradeInIndicatorValues()),
                "получении параметров",
                null,
                this,
                true);

            if (dtos?.Count > 0)
            {
                TradeInWarrantyValues = dtos.Where(x => x.IndicatorId == TradeInIndicator.Warranty.Id)
                    .ToReadOnlyObservableCollection();
                TradeInPackageValues = dtos.Where(x => x.IndicatorId == TradeInIndicator.Package.Id)
                    .ToReadOnlyObservableCollection();
                TradeInClassValues = dtos.Where(x => x.IndicatorId == TradeInIndicator.Class.Id)
                    .Select(x => new ComboBoxItem(x.Id, $"{x.Name} ({x.Description})"))
                    .ToReadOnlyObservableCollection();
            }
        }

        private async Task LoadCategoriesAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            _allCategories = categories?.ToReadOnlyObservableCollection();

            Categories = categories?
                .Where(x => x.Active > 0 && x.UseInTradeIn)
                .OrderBy(x => x.Position)
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadWarehousesAsync()
        {
            HashSet<int> allowWarehouseIds = WebClient.AuthenticatedEmployee.AllowWarehouses;

            List<WarehouseDto> warehouseDtos = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true)
                .GetPagedResultDataAsync();

            int[] warehouseTypeIds = { WarehouseKind.ServiceId, WarehouseKind.PickupId };

            Warehouses = warehouseDtos?
                .Where(x => x.Active == 1 && warehouseTypeIds.Contains(x.TypeId) && allowWarehouseIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();

            HashSet<WarehouseDto> allowServiceWarehouses = warehouseDtos?
                .Where(x => WebClient.AuthenticatedEmployee.AllowWarehouses?.Contains(x.Id) == true && x.TypeId == WarehouseKind.Service.Id)
                .ToHashSet();

            Warehouse = allowServiceWarehouses?.Count == 1 ? allowServiceWarehouses.First() : null;
        }

        private async Task LoadManufactorsAsync()
        {
            ManufactorsDto manufactorsData = await WebClient.ExecuteApiRequestAsync(new QueryAllManufactors());

            if (manufactorsData?.Manufactors?.Any() == true)
            {
                AllManufactors = manufactorsData.Manufactors.ToReadOnlyObservableCollection();
            }
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
                    .Cast<IDictionaryItem>()
                    .ToReadOnlyObservableCollection();
            }
        }

        private async Task CustomerSearchAsync(string phone)
        {
            if (_customer?.Phone1 != phone && _customer?.Phone2 != phone)
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

                    _customer = customer;

                    CustomerId = customer.Id;
                    LastName = customer.LastName;
                    FirstName = customer.FirstName;
                    MiddleName = customer.MiddleName;
                    Email = customer.Email;
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Клиент не найден");
                }

                CustomerUpdated = true;
            }
        }

        private async Task SearchOrderAsync()
        {
            if (_order?.Id != OrderId)
            {
                OrderDto order = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new QueryOrder(OrderId!.Value)),
                    "получении заказа",
                    null,
                    this,
                    false,
                    showDialog: false,
                    showNotification: false,
                    showError: false);

                if (order == null)
                {
                    MessageFacadeService.ShowNotificationWarning($"Заказ № {OrderId} не найден");
                    return;
                }

                ClearProperties();

                _order = order;
                OrderId = order.Id;
                Products = order.Products
                    .DistinctBy(x => x.Product.Id)
                    .Where(x => Categories.Any(z => z.Id == x.Product.ParentCategoryId))
                    .Select(x => new ComboBoxItem(x.Product.Id, x.Product.Name))
                    .ToReadOnlyObservableCollection();

                if (Products.Any())
                {
                    int[] productIds = Products.Select(x => x.Id).ToArray();

                    List<ProductDto> products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(order.ClientId, productIds));

                    _products = products?.ToReadOnlyObservableCollection();

                    SetSerialNumbers(_order.Products);

                    MessageFacadeService.ShowMessageBoxInfo($"Заказ №{OrderId} найден успешно. Выберите товар из выпадающего списка.");
                }
                else
                {
                    MessageFacadeService.ShowMessageBoxWarning($"Заказ №{OrderId} найден успешно. В заказе отсутствуют товары, которые можно принимать по Trade-In");
                }
            }
        }

        private void RemoveOrder()
        {
            ClearProperties();

            RaisePropertiesChanged(nameof(OrderId), nameof(ProductId), nameof(SerialNumber));
        }

        private void ClearProperties()
        {
            OrderId = null;
            Products = null;
            ProductId = null;
            ProductName = null;
            OrderProductId = null;
            SelectedCategory = null;
            ModelOrPn = null;
            SerialNumber = null;
            WarrantyId = null;
            Brand = null;
            SerialNumbers = null;
            _products = null;
            _order = null;
            _productSerials.Clear();

            RaisePropertiesChanged(nameof(OrderId), nameof(ProductId), nameof(SerialNumber));
        }

        private void RemoveProduct()
        {
            ProductId = null;
            ProductName = null;
            ModelOrPn = null;
            SelectedCategory = null;
            Brand = null;
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

                ProductId = product.Id;
                ProductName = product.GetLocalName(LocalizableNameType.Ukr);
                ModelOrPn = product.ProductPn;
                SelectedCategory = _allCategories.FirstOrDefault(x => x.Id == product.ParentCategoryId);
                Brand = product.Manufactor;
                TradeInSegmentId = product.TradeInSegmentId;
                TradeInSegmentName = product.TradeInSegmentName;
            }
        }

        private async void OnProductChanged()
        {
            try
            {
                if (!ProductId.HasValue || _order == null)
                {
                    return;
                }

                OrderProductDto orderProductDto = _order.Products.FirstOrDefault(x => x.Product.Id == ProductId);

                if (orderProductDto is null)
                {
                    return;
                }

                if (orderProductDto.Product.TypeId == ProductType.TradeInId)
                {
                    MessageFacadeService.ShowNotificationError("Запрещено выбирать товар типа Trade-In");

                    ProductId = null;
                    return;
                }

                OrderProductId = orderProductDto.Id;
                ModelOrPn = orderProductDto.Product?.PartNumber;

                ProductDto productDto = _products?.FirstOrDefault(x => x.Id == orderProductDto.Product.Id);

                if (productDto is not null)
                {
                    SelectedCategory = _allCategories?.FirstOrDefault(x => x.Id == productDto.ParentCategoryId);
                    Brand = productDto.Manufactor;
                    TradeInSegmentId = productDto.TradeInSegmentId;
                    TradeInSegmentName = productDto.TradeInSegmentName;

                    CalculateOrderProductWarrantyEndResponseDto warrantyResponse = await WebClient
                        .ExecuteApiRequestAsync(
                            new CalculateOrderProductWarranty(
                                new CalculateOrderProductWarrantyEndDto(DateTime.Now, null, _order.Id, productDto.Id)));

                    if (warrantyResponse.WarrantyEnd < DateTime.Now)
                    {
                        WarrantyId = TradeInIndicator.NoWarrantyId;
                    }
                    else if (warrantyResponse.WarrantyEnd < DateTime.Now.AddYears(1))
                    {
                        WarrantyId = TradeInIndicator.LessThan1YearId;
                    }
                    else
                    {
                        WarrantyId = TradeInIndicator.MoreThan1YearId;
                    }
                }

                if (_productSerials.TryGetValue(productDto?.Id ?? 0, out List<OrderProductSnDto> serials))
                {
                    SerialNumbers = serials.ToReadOnlyObservableCollection();

                    if (SerialNumbers.Count == 1)
                    {
                        SerialNumber = SerialNumbers.First().SerialNumber;
                    }
                }
                else
                {
                    SerialNumbers = null;
                }

                RaisePropertiesChanged(nameof(OrderFoundProductChanged), nameof(SerialNumber));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to handle product change");
                MessageFacadeService.ShowNotificationError("Ошибка при загрузке данных");
            }
        }

        private void OnPhoneNumberChanged()
        {
            CustomerId = null;

            if (CopyCustomer)
            {
                CustomerUpdated = false;
            }
        }

        private void ChangedCategory()
        {
            if (SelectedCategory != null)
            {
                CurrentCategoryManufactors = AllManufactors
                    .Where(x => x.CategoryId == SelectedCategory.Id)
                    .Select(x => x.Manufactor)
                    .ToReadOnlyObservableCollection();
            }
        }

        private void BoughtInTelemartChanged()
        {
            if (!BoughtInTelemart)
            {
                OrderId = null;
                Brand = null;
                ModelOrPn = null;
                SerialNumber = null;
                SelectedCategory = null;
            }

            RaisePropertiesChanged(nameof(OrderId), nameof(ProductId), nameof(SerialNumber));
        }

        private bool CanShowPhoneHistory(string obj)
        {
            return !string.IsNullOrWhiteSpace(obj);
        }

        private void ShowPhoneHistory(string phone)
        {
            PhoneHistoryViewMessage message = new PhoneHistoryViewMessage(phone);

            _messenger.Send(message);
        }

        private void SetSerialNumbers(IEnumerable<OrderProductDto> orderProducts)
        {
            _productSerials.Clear();

            foreach (OrderProductDto orderProduct in orderProducts)
            {
                int productId = orderProduct.Product.Id;

                List<OrderProductSnDto> serials = new List<OrderProductSnDto>();

                serials.AddRange(orderProduct.SerialNumbers);

                _productSerials.Add(productId, serials);
            }
        }

        private void CarryIdChanged()
        {
            OnWarehouseChanged();

            RaisePropertiesChanged(nameof(IsNpWarehouseCarryType), nameof(CityId), nameof(LastName), nameof(MiddleName));
        }

        private void OnWarehouseChanged()
        {
            if (CarryId != CarryType.PickupId)
            {
                return;
            }

            if (Warehouse != null)
            {
                DeliveryData = new DeliveryDataDto
                {
                    CityId = Warehouse.CityId.ToString(),
                    PlaceId = Warehouse.Id.ToString(),
                    Street = null,
                    House = null,
                    Flat = null,
                    Extra = null,
                    MaxAllowedWeight = Warehouse.MaxPackageWeight,
                    Address = Warehouse.Address,
                    AddressUkr = Warehouse.AddressUa,
                    AddressEn = Warehouse.AddressEn
                };
            }
            else
            {
                DeliveryData = null;
            }
        }

        private async Task InitPropertiesAsync(int tradeInIdCopy)
        {
            TradeInDto tradeIn = await WebClient.ExecuteApiRequestAsync(new QueryTradeIn(tradeInIdCopy));

            Phone = tradeIn.Phone;
            FirstName = tradeIn.FirstName;
            LastName = tradeIn.LastName;
            MiddleName = tradeIn.MiddleName;
            Email = tradeIn.Email;
            Inn = tradeIn.Inn;
        }

        private void SelectDeliveryAddress()
        {
            if (CityId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите город");
                return;
            }

            if (CarryId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите способ доставки");
                return;
            }

            CarryType carryType = Dictionaries.GetItemById<CarryType>(CarryId.Value);

            SelectDeliveryDataParameter parameter = new SelectDeliveryDataParameter(
                carryType.Id,
                CityId.Value,
                DeliveryData?.Address,
                DeliveryData);

            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)App.Current.MainWindow.DataContext;

            if (carryType.Kind == CarryTypeKind.Courier)
            {
                SelectDeliveryAddressViewModel viewModel = mainWindowViewModel.WorkspaceViewModel.DialogDocumentManagerService.ShowView<SelectDeliveryAddressViewModel>(
                    parameter,
                    mainWindowViewModel);

                if (viewModel.IsOk)
                {
                    DeliveryData = viewModel.GetDeliveryServiceData();
                }
            }
            else if (carryType.Kind == CarryTypeKind.Pickup)
            {
                SelectDeliveryWarehouseViewModel viewModel = mainWindowViewModel.WorkspaceViewModel.SizeableDialogDocumentManagerService.ShowView<SelectDeliveryWarehouseViewModel>(parameter, mainWindowViewModel);

                if (viewModel.IsOk)
                {
                    DeliveryData = viewModel.GetDeliveryServiceData();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}