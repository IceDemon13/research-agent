using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.DataAnnotations;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Inventory;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Warehouse.Inventory
{
    public class CreateInventoryViewModel : TelemartDialogViewModelBase
    {
        private const int RootCategoryId = 1;
        private readonly List<CategoryViewItem> emptyCategoryList = new List<CategoryViewItem>();

        public CreateInventoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Title = "Создание инвентаризации";

            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            Categories = new ObservableRangeCollection<CategoryViewItem>();
            SelectedWarehouses = new List<object>();
        }

        public CreateInventoryViewModel()
        {
        }

        #region INPC

        public ObservableCollection<WarehouseSimpleDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public List<object> SelectedWarehouses
        {
            get { return GetProperty(() => SelectedWarehouses); }
            set { SetProperty(() => SelectedWarehouses, value); }
        }

        public ObservableRangeCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        public ObservableRangeCollection<InventoryProductType> InventoryProductTypes
        {
            get { return GetProperty(() => InventoryProductTypes); }
            set { SetProperty(() => InventoryProductTypes, value); }
        }

        public InventoryProductType SelectedInventoryProductType
        {
            get { return GetProperty(() => SelectedInventoryProductType); }
            set { SetProperty(() => SelectedInventoryProductType, value, InventoryProductTypeChanged); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public bool TransferScannedBalances
        {
            get { return GetProperty(() => TransferScannedBalances); }
            set { SetProperty(() => TransferScannedBalances, value); }
        }

        #endregion

        public bool CategoriesEnabled => SelectedInventoryProductType != InventoryProductType.Assemblies;

        public List<CategoryViewItem> SelectedCategories => Categories?.Where(x => x.Selected == true).ToList() ?? emptyCategoryList;

        public InventoryDto CreatedInventory { get; set; }

        private IMapper Mapper { get; }

        public static void BuildMetadata(MetadataBuilder<CreateInventoryViewModel> builder)
        {
            builder.Property(x => x.SelectedWarehouses).MatchesRule(x => x?.Any() == true, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Comment).MatchesRule(x => string.IsNullOrEmpty(x) || x.Length <= 1000, () => "Длина должна быть в диапазоне 1..1000 символов");
        }

        protected override async Task HandleLoadedAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            List<CategoryViewItem> categoriesTillParent = GetCategoriesTillParent(Mapper.Map<List<CategoryViewItem>>(categories));

            Categories.Clear();
            Categories.AddRange(categoriesTillParent.OrderBy(x => x.Position).ThenBy(x => x.Name));

            SetAllCategoriesSelected();

            IEnumerable<WarehouseSimpleDto> warehouseItems = warehouses
                .Where(x => WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id) && x.Active == 1)
                .OrderBy(x => x.Name)
                .Select(x => Mapper.Map<WarehouseSimpleDto>(x));

            Warehouses = new ObservableCollection<WarehouseSimpleDto>(warehouseItems);

            InventoryProductTypes = Dictionaries.GetItems<InventoryProductType>().ToObservableRangeCollection();

            SelectedInventoryProductType = InventoryProductType.New;
        }

        protected override async Task HandleOkAsync()
        {
            if (SelectedCategories.Count == 0)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите хотя бы одну категорию товара");
                return;
            }

            WarehouseSimpleDto[] selectedWarehouses = SelectedWarehouses.Cast<WarehouseSimpleDto>().ToArray();

            if (selectedWarehouses.GroupBy(x => x.Address).Count() > 1)
            {
                MessageFacadeService.ShowNotificationError("Выбраны склады с разных адресов");
                return;
            }

            try
            {
                InventoryCreateDto createDto = new InventoryCreateDto()
                {
                    CategoryIds = SelectedCategories.Select(x => x.Id).ToList(),
                    Comment = Comment,
                    WarehouseIds = selectedWarehouses.Select(x => x.Id).ToList(),
                    ProductType = SelectedInventoryProductType.Id,
                    TransferScannedBalances = TransferScannedBalances

                };

                CreatedInventory = await WebClient.ExecuteApiRequestAsync(new CreateInventory(createDto));

                MessageFacadeService.ShowNotificationInfo($"Инвентаризация №{CreatedInventory.Id} успешно создана");
                IsOk = true;
                Close();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get create inventory");
                MessageFacadeService.ShowNotificationError("Ошибка при создании инвентаризации");
            }
        }

        private static void AddCategoryParents(CategoryViewItem childCategory, IReadOnlyDictionary<int, CategoryViewItem> categoriesDictionary, ICollection<CategoryViewItem> parentCategories)
        {
            HashSet<int> parentIdsHash = new HashSet<int>(parentCategories.Select(x => x.Id));
            int parentId = childCategory.ParentId;

            while (parentId != RootCategoryId)
            {
                categoriesDictionary.TryGetValue(parentId, out CategoryViewItem parentCategory);

                if (parentCategory == null)
                {
                    break;
                }

                if (parentIdsHash.Contains(parentId))
                {
                    parentId = parentCategory.ParentId;
                    continue;
                }

                parentCategories.Add(parentCategory);
                parentIdsHash.Add(parentId);
                parentId = parentCategory.ParentId;
            }
        }

        private static List<CategoryViewItem> GetCategoriesTillParent(List<CategoryViewItem> categories)
        {
            List<CategoryViewItem> isParentCategories = categories.Where(x => x.IsParent).ToList();
            Dictionary<int, CategoryViewItem> categoriesDictionary = categories.ToDictionary(x => x.Id);
            List<CategoryViewItem> resultCategories = new List<CategoryViewItem>();

            foreach (CategoryViewItem categoryViewItem in isParentCategories)
            {
                resultCategories.Add(categoryViewItem);
                AddCategoryParents(categoryViewItem, categoriesDictionary, resultCategories);
            }

            return resultCategories;
        }

        private void SetAllCategoriesSelected()
        {
            if (Categories is null)
            {
                return;
            }

            foreach (CategoryViewItem categoryViewItem in Categories)
            {
                categoryViewItem.Selected = true;
            }
        }

        private void InventoryProductTypeChanged()
        {
            RaisePropertyChanged(nameof(CategoriesEnabled));
            SetAllCategoriesSelected();
        }
    }
}