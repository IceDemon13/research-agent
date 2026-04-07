using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AssembledComputerRule;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public sealed class AssembledComputerRuleReserveViewModel : TelemartDialogViewModelBase
    {
        private GridControl gridControl;
        private TelemartEnumerableCompareHelper<AssembledComputerRuleReserveProductViewItem> compareHelper;

        public AssembledComputerRuleReserveViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler,
            IMessenger messenger,
            DocumentCommands documentCommands,
            ProductInformationViewModel productInformationViewModel)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            ErrorHandler = errorHandler;
            Messenger = messenger;
            DocumentCommands = documentCommands;
            ProductInformation = productInformationViewModel;

            DeleteProductCommand = new DelegateCommand(DeleteProduct, () => SelectedProduct != null);
            DeleteZeroReserveProductsCommand = new DelegateCommand(DeleteZeroReserveProducts, () => Products?.Any(x => x.ReserveQuantity == 0) == true);
            AddProductCommand = new AsyncCommand(AddProductAsync);
            ActualizeCommand = new AsyncCommand(ActualizeAsync);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            GridControlLoadedCommand = new DelegateCommand<RoutedEventArgs>(GridControlLoaded);
        }

        public AssembledComputerRuleReserveViewModel()
        {
        }

        public ObservableCollection<AssembledComputerRuleReserveProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value, ProductsChangedCallback); }
        }

        public AssembledComputerRuleReserveProductViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value, RefreshProductInfo); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            set { SetProperty(() => ProductInformation, value); }
        }

        public ObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public ObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public IAsyncCommand AddProductCommand { get; }

        public IAsyncCommand ActualizeCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand DeleteProductCommand { get; }

        public IDelegateCommand GridControlLoadedCommand { get; }

        public IDelegateCommand DeleteZeroReserveProductsCommand { get; }

        #region DialogSettings

        public override int Height => 800;

        public override int MinHeight => 600;

        public override int MinWidth => 1024;

        public override int Width => 1024;

        #endregion

        private IMapper Mapper { get; }

        private IErrorHandler ErrorHandler { get; }

        private IMessenger Messenger { get; }

        private DocumentCommands DocumentCommands { get; }

        public override void OnClose(CancelEventArgs e)
        {
            if (!IsOk && compareHelper?.IsChanged() == true && !MessageFacadeService.Confirm("Закрыть диалог без сохранения изменений?"))
            {
                e.Cancel = true;
            }
            else
            {
                base.OnClose(e);
            }
        }

        protected override async Task HandleLoadedAsync()
        {
            Result<List<AssembledComputerRuleReserveProductDto>> result = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputerRuleReserve(minReserveQuantity: 1));

            Products = result.Data
                .Select(x => Mapper.Map<AssembledComputerRuleReserveProductViewItem>(x))
                .ToObservableCollection();

            PagedResult<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

            Employees = employees.Data
                .Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToObservableCollection();

            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

            Warehouses = warehouses.Data
                .Where(x => x.Active == 1 && (x.TypeId == WarehouseKind.Main.Id || x.TypeId == WarehouseKind.Assembly.Id || x.TypeId == WarehouseKind.Pickup.Id))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToObservableCollection();

            compareHelper = new TelemartEnumerableCompareHelper<AssembledComputerRuleReserveProductViewItem>(Products);

            await base.HandleLoadedAsync();

            Title = "Резерв товаров под конфигурации ПК";
        }

        protected override void Close()
        {
            if (Products != null)
            {
                Products.CollectionChanged -= ProductsChanged;
            }

            base.Close();
        }

        protected override async Task HandleOkAsync()
        {
            if (!compareHelper.IsChanged())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            if (Products.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                MessageFacadeService.ShowNotificationWarning("Найдены ошибки в товарах");
            }

            List<string> errors = new();

            foreach (var product in Products.GroupBy(x => new { x.ProductId, x.WarehouseId }).Where(x => x.Count() > 1))
            {
                errors.Add($"Товар {product.Key.ProductId} в наличии на одинаковом складе разными строками");
            }

            if (errors.Any())
            {
                MessageFacadeService.ShowValidationResultView("Ошибки", errors.Select(x => new ValidationResultItem(x, true)).ToList(), this);
                return;
            }

            List<AssembledComputerRuleProductReserveUpdateDto> productSaveDtos = Products
                .Select(x => new AssembledComputerRuleProductReserveUpdateDto(x.ProductId, x.ReserveQuantity, x.EmployeeId, x.WarehouseId.Value))
                .ToList();

            AssembledComputerRuleReserveUpdateDto saveDto = new AssembledComputerRuleReserveUpdateDto(productSaveDtos);

            (await ErrorHandler.HandleErrorsAsync(ct => WebClient.ExecuteApiRequestAsync(new UpdateAssembledComputerRuleReserve(saveDto)), "сохранении резерва", "резерв сохранен", this, true))
                .IfNotNull(x =>
                {
                    CloseOk();
                });
        }

        private void ProductsChangedCallback(ObservableCollection<AssembledComputerRuleReserveProductViewItem> oldProducts)
        {
            if (Products != null)
            {
                Products.CollectionChanged += ProductsChanged;
            }

            if (oldProducts != null)
            {
                oldProducts.CollectionChanged -= ProductsChanged;
            }
        }

        private void DeleteProduct()
        {
            Products.Remove(SelectedProduct);
        }

        private void DeleteZeroReserveProducts()
        {
            AssembledComputerRuleReserveProductViewItem[] productsToRemove = Products
                .Where(x => x.ReserveQuantity == 0)
                .ToArray();

            if (MessageFacadeService.Confirm("Вы уверены?"))
            {
                foreach (AssembledComputerRuleReserveProductViewItem productToRemove in productsToRemove)
                {
                    Products.Remove(productToRemove);
                }
            }

            RaisePropertyChanged(nameof(Products));
        }

        private async Task AddProductAsync()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (!nomenclatureViewModel.IsOk)
            {
                return;
            }

            NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();

            AssembledComputerRuleReserveProductViewItem productViewItem;

            AssembledComputerRuleReserveProductViewItem existedProduct = Products.FirstOrDefault(x => x.ProductId == product.Id);

            if (existedProduct is null)
            {
                Result<List<AssembledComputerRuleReserveProductDto>> result = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputerRuleReserve(new[] { product.Id }));

                productViewItem = Mapper.Map<AssembledComputerRuleReserveProductViewItem>(result.Data.First());
            }
            else
            {
                productViewItem = new AssembledComputerRuleReserveProductViewItem()
                {
                    ProductId = existedProduct.ProductId,
                    ReserveQuantity = 0,
                    FactReserveQuantity = existedProduct.FactReserveQuantity,
                    AssembledComputerRuleQuantity = existedProduct.AssembledComputerRuleQuantity,
                    ParentCategoryId = existedProduct.ParentCategoryId,
                    ParentCategoryName = existedProduct.ParentCategoryName,
                    SalesQuantity = existedProduct.SalesQuantity,
                    SalesQuantityInsideAssembledComputerRule = existedProduct.SalesQuantityInsideAssembledComputerRule,
                    WarehouseQuantity = existedProduct.WarehouseQuantityFree,
                    MinLeftover = existedProduct.MinLeftover,
                    WarehouseQuantityCalculatedForWarehouseId = existedProduct.WarehouseQuantityCalculatedForWarehouseId,
                    WarehouseQuantityFree = existedProduct.WarehouseQuantityFree,
                    ProductName = existedProduct.ProductName
                };
            }

            productViewItem.EmployeeId = WebClient.AuthenticatedEmployee.Id;
            productViewItem.WarehouseId = null;

            Products.Insert(0, productViewItem);

            SelectedProduct = productViewItem;

            MessageFacadeService.ShowNotificationInfo("Товар успешно добавлен");
        }

        private async Task RefreshAsync()
        {
            if (compareHelper.IsChanged())
            {
                if (!MessageFacadeService.Confirm("Измененные данные не будут сохранены. Вы уверены?"))
                {
                    return;
                }
            }

            Result<List<AssembledComputerRuleReserveProductDto>> result = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputerRuleReserve(minReserveQuantity: 1));

            Products = result.Data
                .Select(x => Mapper.Map<AssembledComputerRuleReserveProductViewItem>(x))
                .ToObservableCollection();
        }

        private void GridControlLoaded(RoutedEventArgs args)
        {
            this.gridControl = args.Source as GridControl;
        }

        private async Task ActualizeAsync()
        {
            OrderFilteringItem orderFilter = new OrderFilteringItem(null, new[] { Subdivision.Telemart.Id }.ToList())
            {
                OrderStatuses = new[] { OrderStatus.Done.Id }.ToList(),
                OrderCompletedOnAfter = DateTime.Now.AddDays(-14),
                AnyAssembledComputerRules = true
            };

            PagedResult<OrderDto> orders = await WebClient.ExecuteApiRequestAsync(new QueryOrders(orderFilter));

            Dictionary<int, OrderFolderDto> folders = orders.Data
                .SelectMany(x => x.Folders)
                .ToDictionary(x => x.Id);

            int[] productIdsInAssembledComputerRulesInOrder = orders.Data
                .SelectMany(x => x.Products.Where(z => z.OrderFolderId.HasValue && folders[z.OrderFolderId.Value].TypeId == OrderFolderType.AssembledComputerRuleId))
                .Select(x => x.Product.Id)
                .ToArray();

            List<AssembledComputerRuleDto> assembledComputerRules = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputerRules());

            int[] productIdsInAssembledComputers = assembledComputerRules
                .Where(x => x.Active)
                .SelectMany(x => x.Products)
                .Select(x => x.ProductId)
                .Distinct()
                .ToArray();

            int[] actualizedOrderProductIds = productIdsInAssembledComputers
                .Union(productIdsInAssembledComputerRulesInOrder)
                .ToArray();

            Products.RemoveAll(x => x.EmployeeId == Constants.SystemEmployeeId && !actualizedOrderProductIds.Contains(x.ProductId));

            int[] currentProductIds = Products.Select(z => z.ProductId).ToArray();

            int[] productIdsToAdd = actualizedOrderProductIds
                .Where(x => !currentProductIds.Contains(x))
                .ToArray();

            Result<List<AssembledComputerRuleReserveProductDto>> result = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputerRuleReserve(productIdsToAdd));

            AssembledComputerRuleReserveProductViewItem[] productViewItems = result.Data
                .Select(x => Mapper.Map<AssembledComputerRuleReserveProductViewItem>(x))
                .ToArray();

            productViewItems.ForEach(x =>
            {
                x.EmployeeId = Constants.SystemEmployeeId;
                x.WarehouseId = null;
            });

            Products.AddRange(productViewItems);

            MessageFacadeService.ShowNotificationInfo("Резервы успешно актуализированы");
        }

        private void RefreshProductInfo()
        {
            ProductInformation.ClearProduct();

            if (SelectedProduct != null)
            {
                ProductInformation.ProductId = new ProductInfoId(SelectedProduct.ProductId, Currency.UahId);
            }
        }

        private void ProductsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            gridControl.RefreshData();
            gridControl.UpdateLayout();

            if (Products != null && !Products.Contains(SelectedProduct))
            {
                SelectedProduct = null;
            }
        }
    }
}