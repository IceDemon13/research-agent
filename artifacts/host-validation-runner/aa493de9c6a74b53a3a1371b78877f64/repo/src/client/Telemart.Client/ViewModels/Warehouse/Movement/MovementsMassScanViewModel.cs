using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business;
using Telemart.Client.Business.PriceConversion;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Movement.Actions;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.AssemblyService;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public sealed class MovementsMassScanViewModel : TelemartDialogViewModelBase
    {
        private const string OtherConfigurationStr = "🖥️ Остальные конфигурации ПК";
        private const string ResetQuantityConfirmMsg = "Очистить текущее значение?";
        private readonly IPriceConverterFactory _priceConverterFactory;
        private readonly IMapper _mapper;
        private readonly IErrorHandler _errorHandler;

        private ScanSerialMode _scanSerialMode = ScanSerialMode.Single;
        private IReadOnlyDictionary<int, List<string>> _accountingSystemSerials;
        private IReadOnlyCollection<(int productId, string nomenclatureSeries)> _movementAssembledComputersNomenclatureSeries;
        private IReadOnlyCollection<MovementDto> _movements;
        private ISet<string> _anotherOrdersNomenclatureSeries;
        private IReadOnlyCollection<MovementAdditionalServiceProductDto> additionalServiceProducts;
        private IReadOnlyCollection<ProductAttributesDto> _productAttributes;
        private IReadOnlyDictionary<string, int> _nomenclatureSeriesWithOrders;
        private Dictionary<int, int> _productParentCategories;
        private IReadOnlyDictionary<int, CategoryDto> _categories;

        public MovementsMassScanViewModel(
            IPriceConverterFactory priceConverterFactory,
            IWebClient webClient,
            IDictionaries dictionaries,
            IMapper mapper,
            IMessageFacadeService messageFacadeService,
            ProductInformationViewModel productInformationViewModel,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _priceConverterFactory = priceConverterFactory;
            _mapper = mapper;
            RecognizeBarcodeViewModel = new RecognizeBarcodeViewModel(webClient, dictionaries, messageFacadeService);
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;

            ResetQuantityOutCommand = new DelegateCommand<MovementProductViewItem>(ResetQuantityOut, x => x != null);
            ResetQuantityInCommand = new DelegateCommand<MovementProductViewItem>(ResetQuantityIn, x => x != null);
            ShowQuantityInSerialsCommand = new DelegateCommand<MovementProductViewItem>(ShowQuantityInSerials);
            ShowQuantityOutSerialsCommand = new DelegateCommand<MovementProductViewItem>(ShowQuantityOutSerials);
            HandleRowDoubleClickCommand = new DelegateCommand(HandleRowDoubleClick);

            ProductInformation = productInformationViewModel;
            _errorHandler = errorHandler;
        }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public IDelegateCommand ShowQuantityInSerialsCommand { get; }

        public IDelegateCommand ShowQuantityOutSerialsCommand { get; }

        public IDelegateCommand ResetQuantityOutCommand { get; }

        public IDelegateCommand ResetQuantityInCommand { get; }

        #region DialogSettings

        public override int Width => 900;

        public override int Height => 506;

        public override int MinWidth => 746;

        public override int MinHeight => 420;

        #endregion DialogSettings

        public RecognizeBarcodeViewModel RecognizeBarcodeViewModel
        {
            get { return GetProperty(() => RecognizeBarcodeViewModel); }
            private init { SetProperty(() => RecognizeBarcodeViewModel, value); }
        }

        public ObservableCollection<MovementProductViewItem> MovementProducts
        {
            get { return GetProperty(() => MovementProducts); }
            private set { SetProperty(() => MovementProducts, value); }
        }

        public MovementProductViewItem SelectedMovementProduct
        {
            get { return GetProperty(() => SelectedMovementProduct); }
            set { SetProperty(() => SelectedMovementProduct, value, OnSelectedProductChanged); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private init { SetProperty(() => ProductInformation, value); }
        }

        public int MovementsStateId
        {
            get { return GetProperty(() => MovementsStateId); }
            private set { SetProperty(() => MovementsStateId, value); }
        }

        public bool HasNewState => MovementsStateId == MovementState.NewId;

        public bool HasLeftState => MovementsStateId == MovementState.LeftId;

        public bool HasArrivedState => MovementsStateId == MovementState.ArrivedId;

        public bool HasReceivedState => MovementsStateId == MovementState.ReceivedId;

        public bool IsNewAndWarehouseFromAllowed =>
            HasNewState && _movements?.All(x => IsWarehouseAllowed(x.WarehouseFromId)) == true;

        public bool IsArrivedAndWarehouseToAllowed =>
            HasArrivedState && _movements?.All(x => IsWarehouseAllowed(x.WarehouseToId)) == true;

        protected override async Task HandleLoadedAsync()
        {
            var parameters = (MovementsMassScanParameter)Parameter;

            if (parameters.Movements.DistinctBy(x => x.StateId).Count() != 1)
            {
                MessageFacadeService.ShowNotificationError("Перемещения должны быть в одном статусе");
                Close();
            }

            MovementsStateId = parameters.Movements.First().StateId;

            _movements = parameters.Movements;

            MovementProducts = _movements
                .SelectMany(x => x.MovementProducts)
                .Select(x => _mapper.Map<MovementProductViewItem>(x))
                .ToObservableCollection();

            MovementProducts = MovementProducts.GroupBy(x => x.ProductId)
                .Select(x =>
                {
                    MovementProductViewItem movementProduct = x.First();

                    movementProduct.QuantityIn = x.Sum(y => y.QuantityIn);
                    movementProduct.QuantityOut = x.Sum(y => y.QuantityOut);
                    movementProduct.Quantity = x.Sum(y => y.Quantity);
                    movementProduct.SerialNumbers = x.SelectMany(y => y.SerialNumbers).ToList();

                    return movementProduct;
                })
                .ToObservableCollection();

            await TaskExt.WhenAll(FetchAssemblyServicesAsync(), FetchCategoriesAsync());

            SplitAssemblyServices();
            SplitAssembledComputers();

            MovementProducts = MovementProducts
                .OrderByDescending(x => x.TypeId == ProductType.AssemblyServiceId)
                .ThenBy(x => x.ProductParentCategoryLeft)
                .ThenBy(x => x.ProductName)
                .ToObservableCollection();

            SplitAdditionalServices();

            MovementProducts.Where(x => x.GroupString is null).ForEach(x => x.GroupString = "Товары");

            MovementProducts = MovementProducts.ToObservableCollection();

            int[] assembledComputerOrderIds =
                _movements.SelectMany(x => x.AssembledComputers).Select(x => x.OrderId).ToArray();

            _anotherOrdersNomenclatureSeries = _nomenclatureSeriesWithOrders
                .Where(x => !assembledComputerOrderIds.Contains(x.Value))
                .Select(x => x.Key)
                .ToHashSet();

            await TaskExt.WhenAll(FetchProductAttributesAsync(), RefreshSummaryItemsAsync());

            await base.HandleLoadedAsync();

            foreach (MovementProductViewItem movementProductViewItem in MovementProducts)
            {
                movementProductViewItem.ProductParentCategoryId = _productParentCategories[movementProductViewItem.ProductId];

                CategoryDto parentCategory = _categories[movementProductViewItem.ProductParentCategoryId];

                movementProductViewItem.ProductParentCategoryName = parentCategory.Name;
                movementProductViewItem.ProductParentCategoryLeft = parentCategory.Left;
            }

            RecognizeBarcodeViewModel.Init(
                new RecognizeBarcodeSettings(
                    true,
                    true,
                    allowOurAssemblyService: true,
                    additionalServiceProductIds: additionalServiceProducts.Select(x => x.AdditionalServiceProductId).ToArray()),
                _productAttributes);

            Title = $"Массовая сверка товаров из перемещений ({string.Join(", ", _movements.Select(x => x.Id))})";
        }

        protected override async Task HandleOkAsync()
        {
            MovementProductMassScanDto[] products = MovementProducts
                .Where(x => x.AdditionalServiceProductId is null)
                .GroupBy(x => x.ProductId)
                .Select(
                    x => new MovementProductMassScanDto()
                    {
                        ProductId = x.Key,
                        Quantity = x.Sum(y => y.Quantity),
                        QuantityOut = x.Sum(y => y.QuantityOut),
                        QuantityIn = x.Sum(y => y.QuantityIn),
                        SerialNumbers = x
                            .Where(z => z.SerialNumbers != null)
                            .SelectMany(z => z.SerialNumbers)
                            .Select(
                                z => new MovementProductSnDto()
                                {
                                    Id = z.Id,
                                    ScannedIn = z.ScannedIn,
                                    ScannedOut = z.ScannedOut,
                                    Sn = z.Sn,
                                    NomenclatureSeries = z.NomenclatureSeries
                                }).ToArray()
                    })
                .ToArray();

            MovementAdditionalServiceProductDto[] additionalServiceProductsToSave = MovementProducts
                .Where(x => x.AdditionalServiceProductId != null)
                .Select(
                    x => new MovementAdditionalServiceProductDto()
                    {
                        ScannedIn = x.QuantityIn > 0,
                        ScannedOut = x.QuantityOut > 0,
                        AdditionalServiceProductId = x.AdditionalServiceProductId.Value,
                        ProductId = x.ProductId
                    })
                .ToArray();

            MovementAssemblyServiceProductDto[] assemblyServiceProductsToSave = MovementProducts
                .Where(x => x.AssemblyServiceId.HasValue && x.AssemblyServiceProductId.HasValue)
                .Select(
                    x => new MovementAssemblyServiceProductDto()
                    {
                        Quantity = x.Quantity,
                        ProductId = x.ProductId,
                        ScannedIn = x.QuantityIn > 0,
                        ScannedOut = x.QuantityOut > 0,
                        AssemblyServiceProductId = x.AssemblyServiceProductId.Value,
                        AssemblyServiceId = x.AssemblyServiceId.Value
                    })
                .ToArray();

            MovementsMassScanDto massScanDto = new MovementsMassScanDto()
            {
                MovementIds = _movements.Select(x => x.Id).ToArray(),
                Products = products,
                AssemblyServiceProducts = assemblyServiceProductsToSave,
                AdditionalServiceProducts = additionalServiceProductsToSave
            };

            Result<Result> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new MassScanMovements(massScanDto)),
                "при сохранении массового сканирования перемещений",
                "Массовое сканирование сохранено успешно",
                this,
                false,
                showNotification: false);

            if (result?.IsSuccess == true)
            {
                CloseOk();
            }
        }

        private async Task<Result> FetchAssemblyServicesAsync()
        {
            AssemblyServicesFilteringItem filteringItem = new AssemblyServicesFilteringItem()
            {
                SubdivisionId = Subdivision.Telemart.Id,
                StateIds = string.Join(",", new[] { AssemblyServiceState.Completed.Id }),
                OrderStateIds = string.Join(
                    ",",
                    new[] { OrderStatus.Received.Id, OrderStatus.Confirmed.Id, OrderStatus.Packed.Id })
            };

            PagedResult<AssemblyServiceDto> assemblyServices =
                await WebClient.ExecuteApiRequestAsync(new QueryAssemblyServices(filteringItem));

            _nomenclatureSeriesWithOrders = assemblyServices.Data
                .Where(x => !string.IsNullOrWhiteSpace(x.NomenclatureSeries))
                .DistinctBy(x => x.NomenclatureSeries)
                .ToDictionary(x => x.NomenclatureSeries, x => x.OrderId);

            return Result.Success();
        }

        private async Task<Result> FetchCategoriesAsync()
        {
            PagedResult<CategoryDto> categoriesResult = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            _categories = categoriesResult.Data.ToDictionary(x => x.Id);

            return Result.Success();
        }

        private async Task<Result> FetchProductAttributesAsync()
        {
            int[] productIds = MovementProducts.Select(x => x.ProductId).Distinct().ToArray();

            PagedResult<ProductAttributesDto> productAttributes =
                await WebClient.ExecuteApiRequestAsync(new QueryProductsAttributesByIds(productIds, true));

            _productAttributes = productAttributes.Data;

            _productParentCategories = productAttributes.Data.ToDictionary(x => x.ProductId, x => x.ParentCategoryId);

            _accountingSystemSerials = productAttributes.Data.ToDictionary(x => x.ProductId, x => x.Serials);

            return Result.Success();
        }

        private void SplitAdditionalServices()
        {
            additionalServiceProducts = _movements.SelectMany(x => x.AdditionalServiceProducts).ToArray();

            if (!additionalServiceProducts.Any())
            {
                return;
            }

            foreach (MovementAdditionalServiceProductDto additionalServiceProductDto in additionalServiceProducts)
            {
                MovementProductViewItem movementProductViewItem = MovementProducts
                    .OrderBy(x => x.AdditionalServicesQuantity)
                    .ThenByDescending(x => x.Quantity)
                    .FirstOrDefault(
                        x => x.ProductId == (additionalServiceProductDto.ParentOrderProductProductId
                                             ?? additionalServiceProductDto.ProductId)
                             && (x.OrderIds?.Any() != true || x.OrderIds.Contains(additionalServiceProductDto.OrderId)));

                if (movementProductViewItem == null)
                {
                    continue;
                }

                MovementProductViewItem splittedMovementProduct;

                if (movementProductViewItem.Quantity > 1)
                {
                    if (additionalServiceProductDto.ScannedIn && movementProductViewItem.QuantityIn == 0)
                    {
                        var movementProductToGrabQuantity = MovementProducts
                            .FirstOrDefault(x => x.QuantityIn > 0 && x.ProductId == movementProductViewItem.ProductId);
                        if (movementProductToGrabQuantity != null)
                        {
                            movementProductViewItem.QuantityIn++;
                            movementProductToGrabQuantity.QuantityIn--;
                        }
                    }

                    if (additionalServiceProductDto.ScannedOut && movementProductViewItem.QuantityOut == 0)
                    {
                        var movementProductToGrabQuantity = MovementProducts
                            .FirstOrDefault(x => x.QuantityOut > 0 && x.ProductId == movementProductViewItem.ProductId);
                        if (movementProductToGrabQuantity != null)
                        {
                            movementProductViewItem.QuantityOut++;
                            movementProductToGrabQuantity.QuantityOut--;
                        }
                    }

                    splittedMovementProduct = movementProductViewItem.SplitAdditionalService(
                        additionalServiceProductDto.ScannedIn,
                        additionalServiceProductDto.ScannedOut);

                    MovementProducts.Add(splittedMovementProduct);
                }
                else
                {
                    splittedMovementProduct = movementProductViewItem;
                }

                splittedMovementProduct.AdditionalServicesQuantity++;

                MovementProductViewItem additionalServiceMovementItem = new MovementProductViewItem()
                {
                    ProductId = additionalServiceProductDto.ProductIdFromAdditionalService,
                    ProductName = additionalServiceProductDto.AdditionalServiceName,
                    ProductNameUa = additionalServiceProductDto.AdditionalServiceNameUa,
                    ProductNameEn = additionalServiceProductDto.AdditionalServiceNameEn,
                    AssemblyServiceId = splittedMovementProduct.AssemblyServiceId,
                    AdditionalServiceProductId = additionalServiceProductDto.AdditionalServiceProductId,
                    AdditionalServiceProductToolTip =
                        $"Оказанная услуга по заказу №{additionalServiceProductDto.OrderId}",
                    ParentRowRef = splittedMovementProduct.RowRef,
                    Quantity = 1,
                    QuantityIn = additionalServiceProductDto.ScannedIn ? 1 : 0,
                    QuantityOut = additionalServiceProductDto.ScannedOut ? 1 : 0,
                    GroupString = splittedMovementProduct.GroupString,
                    AdditionalServiceProductSerialNumber = additionalServiceProductDto.SerialNumber,
                    PrimaryAdditionalServiceProductSerialNumber = additionalServiceProductDto.PrimaryAdditionalServiceProductSn
                };

                int additionalServiceIndex = MovementProducts.IndexOf(splittedMovementProduct) + 1;

                if (additionalServiceIndex > MovementProducts.Count)
                {
                    MovementProducts.Add(additionalServiceMovementItem);
                }
                else
                {
                    MovementProducts.Insert(additionalServiceIndex, additionalServiceMovementItem);
                }

                if (additionalServiceProductDto.ConsumableProducts?.Any() == true)
                {
                    foreach (AdditionalServiceProductConsumableDto additionalServiceProductConsumableDto in
                             additionalServiceProductDto.ConsumableProducts)
                    {
                        MovementProductViewItem additionalServiceConsumableViewItem = MovementProducts
                            .Where(x => !x.IsConsumableAdditionalServiceProduct)
                            .OrderByDescending(x => x.Quantity)
                            .FirstOrDefault(x => x.ProductId == additionalServiceProductConsumableDto.ProductId);

                        if (additionalServiceConsumableViewItem is null)
                        {
                            continue;
                        }

                        MovementProductViewItem splittedConsumableMovementProduct =
                            additionalServiceConsumableViewItem.SplitConsumableAdditionalServiceProduct(
                                additionalServiceMovementItem.QuantityIn > 0,
                                additionalServiceMovementItem.QuantityOut > 0,
                                additionalServiceMovementItem.GroupString,
                                additionalServiceMovementItem.RowRef);

                        splittedConsumableMovementProduct.AssemblyServiceId =
                            additionalServiceMovementItem.AssemblyServiceId;

                        if (ReferenceEquals(splittedConsumableMovementProduct, additionalServiceConsumableViewItem))
                        {
                            MovementProducts.Remove(splittedConsumableMovementProduct);
                        }

                        int additionalServiceConsumableIndex = MovementProducts.IndexOf(splittedMovementProduct) + 2;

                        if (additionalServiceConsumableIndex > MovementProducts.Count)
                        {
                            MovementProducts.Add(splittedConsumableMovementProduct);
                        }
                        else
                        {
                            MovementProducts.Insert(
                                additionalServiceConsumableIndex,
                                splittedConsumableMovementProduct);
                        }
                    }
                }
            }
        }

        private void SplitAssemblyServices()
        {
            MovementAssemblyServiceProductDto[] assemblyServiceProducts =
                _movements.SelectMany(x => x.AssemblyServiceProducts).ToArray();

            if (!assemblyServiceProducts.Any())
            {
                return;
            }

            foreach (MovementAssemblyServiceProductDto assemblyServiceProduct in assemblyServiceProducts)
            {
                MovementProductViewItem movementProductViewItem = MovementProducts
                    .FirstOrDefault(
                        x => x.ProductId == assemblyServiceProduct.ProductId && x.AssemblyServiceId == null);

                if (movementProductViewItem == null)
                {
                    continue;
                }

                MovementProductViewItem splittedMovementProduct = movementProductViewItem.SplitAssemblyService(
                    assemblyServiceProduct.Quantity,
                    assemblyServiceProduct.ScannedIn,
                    assemblyServiceProduct.ScannedOut);

                splittedMovementProduct.GroupString =
                    $"🖥️ Сборка №{assemblyServiceProduct.AssemblyServiceId}, Заказ №{assemblyServiceProduct.OrderId}";
                splittedMovementProduct.OrderIds = new[] { assemblyServiceProduct.OrderId };
                splittedMovementProduct.AssemblyServiceId = assemblyServiceProduct.AssemblyServiceId;
                splittedMovementProduct.AssemblyServiceProductId = assemblyServiceProduct.AssemblyServiceProductId;

                if (!ReferenceEquals(splittedMovementProduct, movementProductViewItem))
                {
                    MovementProducts.Add(splittedMovementProduct);
                }
            }
        }

        private void SplitAssembledComputers()
        {
            MovementAssembledComputerDto[] assembledComputers =
                _movements.SelectMany(x => x.AssembledComputers).ToArray();

            if (!assembledComputers.Any())
            {
                return;
            }

            _movementAssembledComputersNomenclatureSeries = assembledComputers
                .Select(x => (x.ProductId, x.NomenclatureSeries))
                .ToArray();

            StringBuilder orderAssembledComputersToolTip = new StringBuilder();

            MovementProductViewItem[] assembledComputerProducts =
                MovementProducts.Where(x => x.TypeId == ProductType.AssembledComputerRuleId).ToArray();

            foreach (MovementProductViewItem assembledComputerMovementProduct in assembledComputerProducts)
            {
                MovementAssembledComputerDto[] currentViewItemAssembledComputers = assembledComputers
                    .Where(x => x.ProductId == assembledComputerMovementProduct.ProductId)
                    .ToArray();

                if (currentViewItemAssembledComputers.Any())
                {
                    MovementProductViewItem splittedMovementProduct =
                        assembledComputerMovementProduct.SplitAssembledComputer(
                            currentViewItemAssembledComputers.Select(x => x.NomenclatureSeries).ToArray(),
                            MovementsStateId);

                    splittedMovementProduct.GroupString = "🖥️ Конфигурации ПК под заказы";
                    splittedMovementProduct.AssembledComputerForOrder = true;
                    splittedMovementProduct.OrderIds = currentViewItemAssembledComputers.Select(x => x.OrderId).ToArray();

                    orderAssembledComputersToolTip.AppendLine(
                        string.Join(
                            "\n",
                            currentViewItemAssembledComputers.Select(
                                x => $"заказ: {x.OrderId} SN: {x.NomenclatureSeries}")));

                    if (!ReferenceEquals(splittedMovementProduct, assembledComputerMovementProduct))
                    {
                        MovementProducts.Add(splittedMovementProduct);

                        assembledComputerMovementProduct.GroupString = OtherConfigurationStr;
                    }
                }
                else
                {
                    assembledComputerMovementProduct.GroupString = OtherConfigurationStr;
                }
            }

            string assembledComputerForOrderToolTip = orderAssembledComputersToolTip.ToString();

            MovementProducts
                .Where(x => x.AssembledComputerForOrder)
                .ForEach(x => x.AssembledComputerForOrderToolTip = assembledComputerForOrderToolTip);
        }

        private void ShowQuantityInSerials(MovementProductViewItem item)
        {
            List<string> quantityInSerialNumbers =
                item.SerialNumbers.Where(x => x.ScannedIn).Select(x => x.Sn).ToList();

            DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
                new ProductEditSerialsParameter(quantityInSerialNumbers, true),
                this);
        }

        private void ShowQuantityOutSerials(MovementProductViewItem item)
        {
            List<string> quantityOutSerialNumbers =
                item.SerialNumbers.Where(x => x.ScannedOut).Select(x => x.Sn).ToList();

            DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
                new ProductEditSerialsParameter(quantityOutSerialNumbers, true),
                this);
        }

        private void ResetQuantityIn(MovementProductViewItem viewItem)
        {
            if (MessageFacadeService.Confirm(ResetQuantityConfirmMsg))
            {
                MovementProductViewItem[] itemsToClear = GetScanGroupItems(viewItem).ToArray();

                foreach (MovementProductViewItem itemToClear in itemsToClear)
                {
                    itemToClear.QuantityIn = 0;

                    if (itemToClear.SerialNumbers?.Any() == true)
                    {
                        itemToClear.SerialNumbers.ForEach(x => x.ScannedIn = false);

                        MovementProductSnViewItem[] serialNumbersToRemove = itemToClear.SerialNumbers
                            .Where(x => x.NotSaved)
                            .ToArray();

                        serialNumbersToRemove.ForEach(x => itemToClear.SerialNumbers.Remove(x));
                    }
                }
            }
        }

        private void ResetQuantityOut(MovementProductViewItem viewItem)
        {
            if (MessageFacadeService.Confirm(ResetQuantityConfirmMsg))
            {
                MovementProductViewItem[] itemsToClear = GetScanGroupItems(viewItem).ToArray();

                foreach (MovementProductViewItem itemToClear in itemsToClear)
                {
                    itemToClear.QuantityOut = 0;
                    itemToClear.SerialNumbers = new List<MovementProductSnViewItem>();
                }
            }
        }

        private void HandleRowDoubleClick()
        {
            if (SelectedMovementProduct is null
                || SelectedMovementProduct.TypeId != ProductType.AssembledComputerRuleId
                || SelectedMovementProduct.SerialNumbers?.Any() != true)
            {
                return;
            }

            ProductEditSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
                new ProductEditSerialsParameter(
                    SelectedMovementProduct.SerialNumbers.Select(x => x.NomenclatureSeries).ToList()),
                this);

            if (!viewModel.IsOk || viewModel.SerialNumbers.Count == SelectedMovementProduct.SerialNumbers.Count)
            {
                return;
            }

            SelectedMovementProduct.RefreshSerialNumbersAfterEditing(viewModel.SerialNumbers, MovementsStateId);
        }

        private void OnSelectedProductChanged()
        {
            ProductInformation.ClearProduct();

            if (SelectedMovementProduct != null && SelectedMovementProduct.ProductId != 0)
            {
                ProductInformation.ProductId = new ProductInfoId(SelectedMovementProduct.ProductId, Currency.UahId);
            }
        }

        private bool IsWarehouseAllowed(int warehouseId)
        {
            return WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(warehouseId);
        }

        private void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeBarcodeResultEventArgs e)
        {
            if (!IsNewAndWarehouseFromAllowed && !IsArrivedAndWarehouseToAllowed)
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на склад");
                return;
            }

            if (MovementProducts.Any(x => x.ManualAdded))
            {
                e.Message = new RecognizeBarcodeMessage(
                    RecognizeBarcodeMessageType.Error,
                    "Некоторые товары добавлены вручную. Для сканирования необходимо сохранить перемещение");
                return;
            }

            switch (e.Result)
            {
                case RecognizeBarcodeResult.FoundInSupplier:
                case RecognizeBarcodeResult.Found:
                {
                    MovementProductViewItem[] products = MovementProducts
                        .Where(x => x.ProductId == e.ProductId)
                        .ToArray();

                    MovementProductViewItem firstProduct = products
                        .OrderBy(x => x.QuantityIn >= x.Quantity && x.QuantityOut >= x.Quantity)
                        .ThenBy(x => x.AssemblyServiceId.HasValue)
                        .ThenBy(x => x.AdditionalServiceProductId.HasValue)
                        .ThenBy(x => x.IsConsumableAdditionalServiceProduct)
                        .ThenBy(x => x.ParentRowRef.HasValue)
                        .FirstOrDefault();

                    if (firstProduct is null)
                    {
                        e.Message = new RecognizeBarcodeMessage(
                            RecognizeBarcodeMessageType.Warning,
                            "Такого товара нет в перемещении");
                        return;
                    }

                    if (firstProduct.AssemblyServiceId.HasValue)
                    {
                        e.Message = new RecognizeBarcodeMessage(
                            RecognizeBarcodeMessageType.Warning,
                            "Сканирование товара внутри сборки необходимо производить путем сканирования всей сборки");
                        return;
                    }

                    if (firstProduct.AdditionalServiceProductId.HasValue)
                    {
                        e.Message = new RecognizeBarcodeMessage(
                            RecognizeBarcodeMessageType.Warning,
                            "Сканирование товаров-услуг необходимо производить путем сканирования кодов услуг");
                        return;
                    }

                    if (MovementProducts.Any(z => z.ParentRowRef == firstProduct.RowRef)
                        || MovementProducts.Any(
                            z => z.RowRef == firstProduct.ParentRowRef && z.AdditionalServiceProductId.HasValue))
                    {
                        e.Message = new RecognizeBarcodeMessage(
                            RecognizeBarcodeMessageType.Warning,
                            "Необходимо начать сканирование с услуг");
                        return;
                    }

                    if (firstProduct.TypeId == ProductType.AssembledComputerRuleId)
                    {
                        ProcessScanForAssembledComputers(firstProduct, products);
                    }
                    else
                    {
                        firstProduct.Scan(MovementsStateId, e.Quantity);
                    }

                    SelectedMovementProduct = firstProduct;

                    break;
                }

                case RecognizeBarcodeResult.FoundAdditionalServiceProduct:
                {
                    MovementProductViewItem additionalServiceMovementItem = MovementProducts
                        .OrderByDescending(x => x.Quantity - x.QuantityIn)
                        .ThenByDescending(x => x.Quantity - x.QuantityOut)
                        .First(x => x.AdditionalServiceProductId == e.AdditionalServiceProductId);

                    if ((additionalServiceMovementItem.QuantityOut > 0 && (HasNewState || HasLeftState))
                        || (additionalServiceMovementItem.QuantityIn > 0 && (HasReceivedState || HasArrivedState)))
                    {
                        e.Message = new RecognizeBarcodeMessage(
                            RecognizeBarcodeMessageType.Warning,
                            "Услуга уже просканирована");
                        break;
                    }

                    additionalServiceMovementItem.Scan(MovementsStateId, e.Quantity);

                    MovementProductViewItem[] childConsumableProducts = MovementProducts
                        .Where(x => x.ParentRowRef == additionalServiceMovementItem.RowRef)
                        .ToArray();

                    foreach (MovementProductViewItem childConsumableProduct in childConsumableProducts)
                    {
                        childConsumableProduct.Scan(MovementsStateId, e.Quantity);
                    }

                    MovementProductViewItem parentProduct =
                        MovementProducts.FirstOrDefault(x => x.RowRef == additionalServiceMovementItem.ParentRowRef);

                    if (parentProduct is not null)
                    {
                        MovementProductViewItem[] parentProductAdditionalServices = MovementProducts
                            .Where(x => x.ParentRowRef == parentProduct.RowRef && x.AdditionalServiceProductId != null)
                            .ToArray();

                        if (parentProductAdditionalServices.All(x => x.IsScanned(MovementsStateId)))
                        {
                            parentProduct.Scan(MovementsStateId, e.Quantity);

                            if (parentProduct.TypeId == ProductType.AssembledComputerRuleId)
                            {
                                string sn = string.IsNullOrEmpty(
                                    additionalServiceMovementItem.AdditionalServiceProductSerialNumber)
                                    ? additionalServiceMovementItem.PrimaryAdditionalServiceProductSerialNumber
                                    : additionalServiceMovementItem.AdditionalServiceProductSerialNumber;

                                parentProduct.SerialNumbers.Add(new MovementProductSnViewItem
                                {
                                    Id = 0,
                                    NomenclatureSeries = sn,
                                    Sn = sn,
                                    NotSaved = true,
                                    ScannedOut = MovementsStateId == MovementState.New.Id,
                                    ScannedIn = MovementsStateId == MovementState.Arrived.Id
                                });
                            }
                        }

                        if (parentProduct.AssemblyServiceId.HasValue)
                        {
                            MovementProductViewItem[] assemblyItems = MovementProducts
                                .Where(x => x.AssemblyServiceId == parentProduct.AssemblyServiceId)
                                .ToArray();

                            Guid[] assemblyItemsRowRefs = assemblyItems.Select(x => x.RowRef).ToArray();

                            MovementProductViewItem[] assemblyAdditionalServiceItems = MovementProducts
                                .Where(
                                    x => x.ParentRowRef.HasValue && assemblyItemsRowRefs.Contains(x.ParentRowRef.Value)
                                                                 && x.AdditionalServiceProductId.HasValue)
                                .ToArray();

                            if (assemblyAdditionalServiceItems.All(x => x.IsScanned(MovementsStateId)))
                            {
                                foreach (MovementProductViewItem item in assemblyItems)
                                {
                                    item.ScanAssemblyProduct(MovementsStateId);
                                }
                            }
                        }
                    }

                    break;
                }

                case RecognizeBarcodeResult.FoundAssembly:
                {
                    MovementProductViewItem[] assemblyItems = MovementProducts
                        .Where(x => x.AssemblyServiceId == e.AssemblyServiceId)
                        .ToArray();

                    if (assemblyItems.Length == 0)
                    {
                        e.Message = new RecognizeBarcodeMessage(
                            RecognizeBarcodeMessageType.Warning,
                            "Такой сборки нет в перемещении");
                        return;
                    }

                    if (assemblyItems.Any(x => x.IsScanned(MovementsStateId)))
                    {
                        e.Message = new RecognizeBarcodeMessage(
                            RecognizeBarcodeMessageType.Warning,
                            "Сборка уже просканирована");
                        return;
                    }

                    if (assemblyItems.Any(
                            x => MovementProducts.Any(
                                z => z.ParentRowRef == x.RowRef && z.AdditionalServiceProductId.HasValue)))
                    {
                        e.Message = new RecognizeBarcodeMessage(
                            RecognizeBarcodeMessageType.Warning,
                            "Сканировать сборку с услугами необходимо через сканирование услуг");
                        return;
                    }

                    foreach (MovementProductViewItem item in assemblyItems)
                    {
                        item.Scan(MovementsStateId, item.Quantity);
                    }

                    break;
                }

                case RecognizeBarcodeResult.NotFound:
                {
                    e.Message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "Товар не найден");
                    break;
                }
            }
        }

        private ProductScanSerialsViewModel ShowScanSerialsViewForAssembledComputer(int productId)
        {
            string[] barcodes = RecognizeBarcodeViewModel.GetBarcodesById(productId).ToArray();

            string[] existingNomenclaturesSeries = MovementProducts
                .Where(x => x.SerialNumbers?.Any() == true)
                .SelectMany(x => x.SerialNumbers)
                .Where(x => (HasArrivedState && x.ScannedIn) || (HasNewState && x.ScannedOut))
                .Select(x => x.NomenclatureSeries)
                .ToArray();

            ProductSerialsViewModelParameter parameter = new ProductSerialsViewModelParameter(
                productId,
                existingNomenclaturesSeries,
                barcodes,
                null,
                _scanSerialMode,
                _accountingSystemSerials[productId],
                title: "Сканирование SN конфигурации ПК",
                ignoreLengthValidation: true);

            ProductScanSerialsViewModel viewModel =
                DialogDocumentManagerService.ShowView<ProductScanSerialsViewModel>(parameter, this);

            return viewModel;
        }

        private void ProcessScanForAssembledComputers(
            MovementProductViewItem firstProduct,
            MovementProductViewItem[] items)
        {
            ProductScanSerialsViewModel viewModel = ShowScanSerialsViewForAssembledComputer(firstProduct.ProductId);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (viewModel.SerialNumbers.Any(x => x is null || !Regex.IsMatch(x, @"^[0-9-]*$") || x.Length < 8))
            {
                MessageFacadeService.ShowNotificationError("Неверный формат серии номенклатур");
                return;
            }

            foreach (string nomenclatureSeries in viewModel.SerialNumbers)
            {
                MovementProductViewItem assembledComputerProduct;

                if (items.Any(x => x.AssembledComputerForOrder)
                    && _movementAssembledComputersNomenclatureSeries.Any(
                        x => x.nomenclatureSeries == nomenclatureSeries && x.productId == firstProduct.ProductId))
                {
                    assembledComputerProduct = items.First(x => x.AssembledComputerForOrder);
                }
                else
                {
                    assembledComputerProduct = items.FirstOrDefault(x => !x.AssembledComputerForOrder);

                    if (_anotherOrdersNomenclatureSeries.Contains(nomenclatureSeries))
                    {
                        MessageFacadeService.ShowMessageBoxWarning("Конфигурация ПК относится к другому заказу");
                    }

                    if (assembledComputerProduct is null)
                    {
                        if (!HasArrivedState)
                        {
                            MessageFacadeService.ShowNotificationError("Такой конфигурации ПК нет в перемещении");
                            return;
                        }

                        assembledComputerProduct = new MovementProductViewItem()
                        {
                            AssembledComputerForOrder = false,
                            AssemblyServiceId = null,
                            AssembledComputerForOrderToolTip = null,
                            ProductId = firstProduct.ProductId,
                            CreatedBy = WebClient.AuthenticatedEmployee.Id,
                            GroupString = OtherConfigurationStr,
                            ProductName = firstProduct.ProductName,
                            ProductNameUa = firstProduct.ProductNameUa,
                            ProductNameEn = firstProduct.ProductNameEn,
                            ProductPrefixRus = firstProduct.ProductPrefixRus,
                            ProductPrefixUa = firstProduct.ProductPrefixUa,
                            Quantity = 0,
                            Price = firstProduct.Price,
                            ProductParentCategoryName = firstProduct.ProductParentCategoryName,
                            ProductParentCategoryId = firstProduct.ProductParentCategoryId,
                            TypeId = firstProduct.TypeId,
                            Weight = firstProduct.Weight,
                            UsdCurrency = firstProduct.UsdCurrency,
                            ProductNameBase = firstProduct.ProductNameBase,
                            SerialNumbers = new List<MovementProductSnViewItem>()
                        };

                        MovementProducts.Add(assembledComputerProduct);

                        MovementProducts = MovementProducts.ToObservableCollection();
                    }
                }

                assembledComputerProduct.SerialNumbers ??= new List<MovementProductSnViewItem>();

                MovementProductSnViewItem productSn =
                    assembledComputerProduct.SerialNumbers.FirstOrDefault(
                        x => x.NomenclatureSeries == nomenclatureSeries);

                if (productSn is null)
                {
                    productSn = new MovementProductSnViewItem()
                    {
                        Id = 0,
                        NomenclatureSeries = nomenclatureSeries,
                        Sn = nomenclatureSeries,
                        NotSaved = true
                    };

                    assembledComputerProduct.SerialNumbers.Add(productSn);
                }

                productSn.Scan(MovementsStateId);

                assembledComputerProduct.Scan(MovementsStateId, 1);

                assembledComputerProduct.RaiseProperties();
            }

            _scanSerialMode = viewModel.ScanMode;
        }

        private IEnumerable<MovementProductViewItem> GetScanGroupItems(MovementProductViewItem viewItem)
        {
            if (viewItem.AssemblyServiceId.HasValue)
            {
                foreach (MovementProductViewItem assemblyServiceProductViewItem in MovementProducts.Where(x => x.AssemblyServiceId == viewItem.AssemblyServiceId))
                {
                    yield return assemblyServiceProductViewItem;
                }

                yield break;
            }

            if (viewItem.IsConsumableAdditionalServiceProduct)
            {
                MovementProductViewItem additionalServiceProduct = MovementProducts.First(x => x.RowRef == viewItem.ParentRowRef);
                MovementProductViewItem product = MovementProducts.First(x => x.RowRef == additionalServiceProduct.ParentRowRef);

                yield return additionalServiceProduct;
                yield return product;

                IEnumerable<MovementProductViewItem> allConsumables = MovementProducts
                    .Where(x => x.ParentRowRef == additionalServiceProduct.RowRef);

                foreach (MovementProductViewItem consumable in allConsumables)
                {
                    yield return consumable;
                }

                yield break;
            }

            if (viewItem.IsAdditionalServiceProduct)
            {
                MovementProductViewItem parentProduct = MovementProducts.First(x => x.RowRef == viewItem.ParentRowRef);
                MovementProductViewItem[] consumableAdditionalServiceProducts = MovementProducts
                    .Where(x => x.ParentRowRef == viewItem.RowRef && x.IsConsumableAdditionalServiceProduct)
                    .ToArray();

                foreach (MovementProductViewItem consumableAdditionalServiceProduct in consumableAdditionalServiceProducts)
                {
                    yield return consumableAdditionalServiceProduct;
                }

                yield return parentProduct;
                yield return viewItem;
                yield break;
            }

            MovementProductViewItem childAdditionalServiceProduct = MovementProducts
                .FirstOrDefault(x => x.ParentRowRef == viewItem.RowRef && x.IsAdditionalServiceProduct);

            if (childAdditionalServiceProduct is not null)
            {
                yield return childAdditionalServiceProduct;
                yield return viewItem;

                MovementProductViewItem[] consumableAdditionalServiceProducts = MovementProducts
                    .Where(x => x.ParentRowRef == childAdditionalServiceProduct.RowRef && x.IsConsumableAdditionalServiceProduct)
                    .ToArray();

                foreach (MovementProductViewItem consumableAdditionalServiceProduct in consumableAdditionalServiceProducts)
                {
                    yield return consumableAdditionalServiceProduct;
                }

                yield break;
            }

            yield return viewItem;
        }

        private async Task<Result> RefreshSummaryItemsAsync()
        {
            IPriceConverter priceConverter = await _priceConverterFactory.CreateAsync();

            SummaryItems = GetSummaryItems();

            return Result.Success();

            IEnumerable<SummaryViewItem> GetSummaryItems()
            {
                DateTime minDepartureDate = _movements.Min(x => x.DateDeparture);
                DateTime maxDepartureDate = _movements.Max(x => x.DateDeparture);

                yield return new SummaryViewItem(
                    "Отправка",
                    $"{minDepartureDate.ToString(DateFormattingRules.FullDateTimeFormat)} - {maxDepartureDate.ToString(DateFormattingRules.FullDateTimeFormat)}");

                DateTime minDateArrive = _movements.Min(x => x.DateArrive);
                DateTime maxDateArrive = _movements.Max(x => x.DateArrive);

                yield return new SummaryViewItem(
                    "Прибытие (план)",
                    $"{minDateArrive.ToString(DateFormattingRules.FullDateTimeFormat)} - {maxDateArrive.ToString(DateFormattingRules.FullDateTimeFormat)}");

                DateTime[] arrivedDates = _movements.Select(x => x.ArrivedOn).Where(x => x is not null).Select(x => x.Value).ToArray();

                var minArriavedDate = arrivedDates.Min();
                var maxArriavedDate = arrivedDates.Max();

                if (arrivedDates.Any())
                {
                    yield return new SummaryViewItem(
                        "Прибытие",
                        $"{minArriavedDate.ToString(DateFormattingRules.FullDateTimeFormat)} - {maxArriavedDate.ToString(DateFormattingRules.FullDateTimeFormat)}");
                }

                DateTime minDateIn = _movements.Min(x => x.DateIn);
                DateTime maxDateIn = _movements.Max(x => x.DateIn);

                yield return new SummaryViewItem(
                    "Получение",
                    $"{minDateIn.ToString(DateFormattingRules.FullDateTimeFormat)} - {maxDateIn.ToString(DateFormattingRules.FullDateTimeFormat)}");

                string[] carries = _movements
                    .Where(x => x.CarryId.HasValue)
                    .Select(x => x.CarryId)
                    .Distinct()
                    .Select(x => Dictionaries.GetItemById<CarryType>(x!.Value).Name)
                    .ToArray();

                if (carries.Any())
                {
                    yield return new SummaryViewItem("Доставка", string.Join(", ", carries));
                }

                string[] trackNumbers = _movements.Select(x => x.TrackNumber).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                if (trackNumbers.Any())
                {
                    yield return new SummaryViewItem("ТТН", string.Join(", ", trackNumbers));
                }

                int places = _movements.Select(x => x.Places ?? 0).Sum();

                yield return new SummaryViewItem("Мест", places.ToString());

                yield return new SummaryViewItem("Статус", $"{Dictionaries.GetItemById<MovementState>(MovementsStateId)}");
                yield return new SummaryViewItem("Вес", $"{MovementProducts.Sum(x => x.Weight * x.QuantityOut):N1}");

                yield return new SummaryViewItem(
                    "План",
                    $"{Math.Round(MovementProducts.Sum(x => priceConverter.Convert(x.Price ?? 0, Currency.UsdId, Currency.UahId, x.UsdCurrency, false) * x.Quantity))}");
                yield return new SummaryViewItem(
                    "Факт",
                    $"{Math.Round(MovementProducts.Sum(x => priceConverter.Convert(x.Price ?? 0, Currency.UsdId, Currency.UahId, x.UsdCurrency, false) * x.QuantityOut))}");
            }
        }
    }
}