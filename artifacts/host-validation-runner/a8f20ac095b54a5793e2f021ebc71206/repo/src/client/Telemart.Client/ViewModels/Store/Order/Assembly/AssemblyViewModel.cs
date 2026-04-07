using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.CheckCompatibility;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Store.Order.Assembly
{
    public class AssemblyViewModel : TelemartDialogViewModelBase
    {
        private readonly ObservableCollection<AssemblyViewItem> _items;
        private NomenclatureViewItem _assemblyServiceProduct;
        private AssemblyParameter _parameter;
        private IReadOnlyCollection<TransferObjects.CategoryDto> _categories;
        private bool _isAssemblyComplectation;

        public AssemblyViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;

            SelectProductCommand = new DelegateCommand<AssemblyViewItem>(SelectProduct, x => x?.ReadOnly != true && x?.IsGift != true);
            DeleteProductCommand = new DelegateCommand<AssemblyViewItem>(DeleteProduct, x => x?.Product != null && !x.ReadOnly && !x.IsGift);

            _items = new ObservableCollection<AssemblyViewItem>();

            CheckCompatibilityVisible = webClient.IsOperationAllowed(BusinessOperation.OrderAllowAddIncompatibleAssemblies);
        }

        public IDelegateCommand SelectProductCommand { get; }

        public IDelegateCommand DeleteProductCommand { get; }

        public ReadOnlyObservableCollection<AssemblyViewItem> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        public bool ShowAll
        {
            get { return GetProperty(() => ShowAll); }
            set { SetProperty(() => ShowAll, value, ShowAllChanged); }
        }

        public string FilterString
        {
            get { return GetProperty(() => FilterString); }
            set { SetProperty(() => FilterString, value, () => ShowAll = string.IsNullOrEmpty(FilterString)); }
        }

        public bool AssemblyService
        {
            get { return GetProperty(() => AssemblyService); }
            set { SetProperty(() => AssemblyService, value); }
        }

        public bool ReadOnlyAssemblyService
        {
            get { return GetProperty(() => ReadOnlyAssemblyService); }
            set { SetProperty(() => ReadOnlyAssemblyService, value); }
        }

        public ReadOnlyObservableCollection<CategoryType> CategoryTypes
        {
            get { return GetProperty(() => CategoryTypes); }
            set { SetProperty(() => CategoryTypes, value); }
        }

        public bool CheckCompatibility
        {
            get { return GetProperty(() => CheckCompatibility); }
            set { SetProperty(() => CheckCompatibility, value); }
        }

        public bool CheckCompatibilityVisible { get; }

        public override int Width { get; } = 1000;

        public override int MinWidth { get; } = 1000;

        public override int Height { get; } = 600;

        public override int MinHeight { get; } = 600;

        private IMapper Mapper { get; }

        public IEnumerable<AssemblyViewModelResult> GetProducts()
        {
            foreach (AssemblyViewItem item in _items.Where(x => x.Product != null))
            {
                yield return new AssemblyViewModelResult(item.Product, item.AssemblyIncluded, item.IsGift);
            }

            if (AssemblyService)
            {
                yield return new AssemblyViewModelResult(_assemblyServiceProduct, true, false);
            }
        }

        protected override async Task HandleLoadedAsync()
        {
            CheckCompatibility = true;

            _parameter = (AssemblyParameter)Parameter;

            _isAssemblyComplectation = _parameter.OrderFolder?.ProductId != null;

            CategoryTypes = Dictionaries.GetItems<CategoryType>().ToReadOnlyObservableCollection();
            ShowAll = string.IsNullOrEmpty(FilterString);

            IReadOnlyCollection<NomenclatureViewItem> products;

            IReadOnlyDictionary<int, AssemblyParameterProduct[]> assemblyProducts;

            if (_parameter.Products?.Any() == true)
            {
                assemblyProducts = _parameter.Products
                    .GroupBy(x => x.ProductId)
                    .ToDictionary(x => x.Key, x => x.ToArray());

                AssemblyService = _parameter.Products.Any(x => x.ProductId == Constants.AssemblyServiceProductId);

                List<ProductDto> productDtos = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(
                    _parameter.ContractorId,
                    assemblyProducts.Keys.ToArray(),
                    true,
                    true,
                    _isAssemblyComplectation,
                    _isAssemblyComplectation));

                products = productDtos.Select(x => Mapper.Map<NomenclatureViewItem>(x)).ToList();

                products.ForEach(x => x.Quantity = assemblyProducts[x.Id].Max(y => y.Quantity));

                Title = "Сборка";
            }
            else
            {
                AssemblyService = false;
                products = Array.Empty<NomenclatureViewItem>();
                assemblyProducts = new Dictionary<int, AssemblyParameterProduct[]>();
                Title = "Создание сборки";
            }

            if (AssemblyService)
            {
                _assemblyServiceProduct = products.FirstOrDefault(x => x.Id == Constants.AssemblyServiceProductId);
                ReadOnlyAssemblyService = assemblyProducts[Constants.AssemblyServiceProductId].Any(x => x.ReadOnly);
            }
            else
            {
                List<ProductDto> assemblyServiceProducts = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(_parameter.ContractorId, new[] { Constants.AssemblyServiceProductId }, true, true));

                _assemblyServiceProduct = assemblyServiceProducts.Select(x => Mapper.Map<NomenclatureViewItem>(x)).First();
                ReadOnlyAssemblyService = false;
            }

            _assemblyServiceProduct.Quantity = 1;

            _categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            foreach (TransferObjects.CategoryDto category in _categories.Where(x => x.IsParent && x.Active > 0))
            {
                bool added = false;

                foreach (NomenclatureViewItem product in products.Where(x => category.Id == x.ParentCategoryId && x.Id != Constants.AssemblyServiceProductId))
                {
                    if (assemblyProducts.TryGetValue(product.Id, out AssemblyParameterProduct[] assemblyParameterProducts))
                    {
                        AssemblyViewItem assembly = AddAssembly(
                            category,
                            assemblyParameterProducts.Any(x => x.ReadOnly),
                            assemblyParameterProducts.All(x => x.AssemblyIncluded),
                            assemblyParameterProducts.Any(x => x.IsGift));

                        if (_parameter.OrderFolder.Price.HasValue)
                        {
                            product.Price = assemblyParameterProducts.Min(x => x.Price);
                        }

                        assembly.SetProduct(product);

                        added = true;
                    }
                }

                if (!added && category.TypeId.HasValue)
                {
                    AddAssembly(category, false, category.TypeId == CategoryType.PcComponents.Id, false);
                }
            }

            Items = new ReadOnlyObservableCollection<AssemblyViewItem>(_items);

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            if (_parameter.AnyAssemblyServices && !AssemblyService)
            {
                MessageFacadeService.ShowNotificationWarning("Запрещено удалять услугу сборки если есть привязанные сборки");
                return;
            }

            if (GetProducts().All(x => x.NomenclatureItem.Id == Constants.AssemblyServiceProductId))
            {
                MessageFacadeService.ShowNotificationWarning("Выберите хотя бы один товар");
                return;
            }

            if (AssemblyService && !Items.Any(x => x.Category.TypeId == CategoryType.PcComponents.Id && x.Product != null && x.AssemblyIncluded))
            {
                MessageFacadeService.ShowNotificationWarning("Выберите хотя бы одну комплектующую");
                return;
            }

            if (!CheckCompatibilityVisible || CheckCompatibility)
            {
                try
                {
                    IReadOnlyCollection<ProductQuantityDto> productQuantities = _items
                        .Where(x => x.Product != null && x.AssemblyIncluded)
                        .Select(x => new ProductQuantityDto(x.Product.Id, x.Product.Quantity))
                        .ToArray();

                    CheckCompatibilityResponse response =
                        await WebClient.ExecuteCatalogApiRequestAsync(
                            new CheckProductCompatibility(productQuantities, _parameter.OrderFolder?.ProductId));

                    var validationResults = response.GetValidationResults().ToArray();

                    if (!validationResults.Any() || ShowValidationResultView(
                            "Валидация при проверке сборки",
                            validationResults.Select(
                                x => new ValidationResultItem(x.Message, (!AssemblyService && x.NotificationImageId == NotificationImage.ErrorId) ? NotificationImage.WarningId : x.NotificationImageId))))
                    {
                        IsOk = true;
                        Close();
                    }
                }
                catch (UnexpectedSatusException exception)
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при проверке сборки");
                    ShowValidationResultView("Ошибки при проверке сборки", exception.GetErrorItems());
                }
                catch (UnexpectedErrorException exception)
                {
                    Logger.LogError(exception, "Failed to check compatibility");
                    ShowValidationResultView(
                        Resources.ServerConnectError,
                        new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при проверке сборки");
                    Logger.LogError(exception, "Error while checking compatibility");
                }
            }
            else
            {
                CloseOk();
            }
        }

        private void SelectProduct(AssemblyViewItem assemblyViewItem)
        {
            int? selectedCategoryId = assemblyViewItem?.Category.Id;
            int? selectedProductId = assemblyViewItem?.Product?.Id;

            NomenclatureViewSelectionMode selectionMode = assemblyViewItem == null
                ? NomenclatureViewSelectionMode.ByQuantity
                : NomenclatureViewSelectionMode.Single;

            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                _parameter.ContractorId,
                selectionMode,
                queryGifts: _parameter.QueryGifts,
                queryAdditionalServices: _parameter.QueryAdditionalServices,
                selectedCategoryId: selectedCategoryId,
                queryPriceIn: _isAssemblyComplectation,
                priceInIncludeReserve: _isAssemblyComplectation,
                compatibleWithProducts: _items
                    .Where(x => x.Product != null && (selectedProductId == null || x.Product.Id != selectedProductId))
                    .Select(x => new ProductQuantity(x.Product.Id, x.Product.Quantity))
                    .ToArray(),
                cartProductIds: _items
                    .Where(x => x.Product != null && (selectedProductId == null || x.Product.Id != selectedProductId))
                    .Select(x => x.Product.Id)
                    .ToArray());

            NomenclatureViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            List<string> warnings = new List<string>();

            if (viewModel.IsOk)
            {
                foreach (NomenclatureViewItem nomenclatureViewItem in viewModel.GetSelectedItems())
                {
                    if (assemblyViewItem != null && assemblyViewItem.Category.Id != nomenclatureViewItem.ParentCategoryId)
                    {
                        MessageFacadeService.ShowNotificationError("Родительская категория товара не равна выбранной");
                        continue;
                    }

                    if (Constants.UniqueComplectCategoryIds.Contains(nomenclatureViewItem.ParentCategoryId)
                        && _items.Any(x => x != assemblyViewItem && x.Product != null && x.Category.Id == nomenclatureViewItem.ParentCategoryId))
                    {
                        warnings.Add($"Товар в категории \"{nomenclatureViewItem.ParentCategoryName}\" уже добавлен");
                        continue;
                    }

                    if (_items.Any(x => x != assemblyViewItem && x.Product?.Id == nomenclatureViewItem.Id))
                    {
                        warnings.Add($"Товар \"{nomenclatureViewItem.NameFullRu}\" уже добавлен");
                        continue;
                    }

                    if (nomenclatureViewItem.TypeId == ProductType.AssemblyServiceId)
                    {
                        AssemblyService = true;
                        continue;
                    }

                    TransferObjects.CategoryDto parentCategory = _categories.First(x => x.Id == nomenclatureViewItem.ParentCategoryId);

                    AssemblyViewItem item = assemblyViewItem ?? AddAssembly(parentCategory, false, parentCategory.TypeId == CategoryType.PcComponents.Id, false);

                    if (_parameter.OrderFolder.Price.HasValue)
                    {
                        nomenclatureViewItem.Price = item.Product?.Price ?? 0;
                    }

                    item.SetProduct(nomenclatureViewItem);
                }

                if (warnings.Any())
                {
                    ShowValidationResultView("Предупреждения", warnings.Select(x => new ValidationResultItem(x, false)));
                }
            }

            Items = new ReadOnlyObservableCollection<AssemblyViewItem>(_items);
        }

        private void DeleteProduct(AssemblyViewItem viewItem)
        {
            if (_items.Count(x => x.Category.Id == viewItem.Category.Id) > 1)
            {
                _items.Remove(viewItem);
            }
            else
            {
                viewItem.RemoveProduct();
            }

            RaisePropertyChanged(nameof(Items));
        }

        private AssemblyViewItem AddAssembly(TransferObjects.CategoryDto category, bool readOnly, bool assemblyIncluded, bool isGift)
        {
            AssemblyViewItem viewItem = _items.FirstOrDefault(x => x.Category.Id == category.Id && x.Product == null);

            if (viewItem == null)
            {
                viewItem = new AssemblyViewItem(
                    category,
                    Constants.UniqueComplectCategoryIds.Contains(category.Id),
                    _categories.Where(y => y.Left >= category.Left && y.Right <= category.Right).Select(y => y.Id).ToArray(),
                    readOnly,
                    assemblyIncluded,
                    isGift);

                _items.Add(viewItem);
            }

            return viewItem;
        }

        private void ShowAllChanged()
        {
            if (ShowAll && !string.IsNullOrEmpty(FilterString))
            {
                FilterString = string.Empty;
            }
            else if (!ShowAll && string.IsNullOrEmpty(FilterString))
            {
                FilterString = "![Product] IS NULL";
            }
        }
    }
}