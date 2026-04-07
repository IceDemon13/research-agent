using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Locations;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.Requests.Features.Showcase;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.TransferObjects.Showcase;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Locations;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Showcase
{
    public sealed class ShowcaseCategoryViewModel : TelemartDialogViewModelBase
    {
        private volatile bool _averageShowcaseQuantityLocation;
        private IReadOnlyCollection<ShowcaseCategoryViewItem> _originalShowcaseCategoryItems;
        private bool _originalClusterCategoryShowcaseSetQuantity;

        public ShowcaseCategoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messagefacadeService,
            IMapper mapper,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messagefacadeService)
        {
            Mapper = mapper;
            ErrorHandler = errorHandler;

            AddShowcaseCategoryCommand = new DelegateCommand(AddShowcaseCategory);
            CopyShowcaseCategoryCommand = new DelegateCommand(CopyShowcaseCategory, () => SelectedShowcaseCategory is not null);
            DeleteShowcaseCategoryCommand = new DelegateCommand(DeleteShowcaseCategory, () => SelectedShowcaseCategory is not null);
            EditShowcaseCategoryCommand = new DelegateCommand(EditShowcaseCategory, () => SelectedShowcaseCategory is not null);
            HandleRowDoubleClickCommand = new DelegateCommand<RowDoubleClickInfo>(HandleRowDoubleClick, x => x != null);
            SaveCommand = new AsyncCommand(SaveAsync);
        }

        public IDelegateCommand AddShowcaseCategoryCommand { get; }

        public IDelegateCommand CopyShowcaseCategoryCommand { get; }

        public IDelegateCommand DeleteShowcaseCategoryCommand { get; }

        public IDelegateCommand EditShowcaseCategoryCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        #region DialogSettings

        public override int MinHeight => 570;

        public override int Height => 645;

        public override int MaxHeight => 1080;

        public override int MinWidth => 710;

        public override int Width => 1000;

        public override int MaxWidth => 1920;

        #endregion

        public ObservableCollection<ShowcaseCategoryViewItem> ShowcaseCategories
        {
            get { return GetProperty(() => ShowcaseCategories); }
            private set { SetProperty(() => ShowcaseCategories, value); }
        }

        public ShowcaseCategoryViewItem SelectedShowcaseCategory
        {
            get { return GetProperty(() => SelectedShowcaseCategory); }
            set { SetProperty(() => SelectedShowcaseCategory, value); }
        }

        public ReadOnlyObservableCollection<ClusterViewItem> Clusters
        {
            get { return GetProperty(() => Clusters); }
            private set { SetProperty(() => Clusters, value); }
        }

        public ReadOnlyObservableCollection<LocationViewItem> Locations
        {
            get { return GetProperty(() => Locations); }
            set { SetProperty(() => Locations, value); }
        }

        public bool ClusterCategoryShowcaseSetQuantity
        {
            get { return GetProperty(() => ClusterCategoryShowcaseSetQuantity); }
            set { SetProperty(() => ClusterCategoryShowcaseSetQuantity, value); }
        }

        public bool IsAllowChangeSettings => WebClient.IsOperationAllowed(BusinessOperation.AllowChangeClusterCategoryShowcaseSetQuantity);

        private IMapper Mapper { get; }

        private IErrorHandler ErrorHandler { get; }

        public override void OnClose(CancelEventArgs e)
        {
            if (!IsOk && (_originalShowcaseCategoryItems.Count != ShowcaseCategories.Count || ShowcaseCategories.Any(x => x.IsChanged)) && !MessageFacadeService.Confirm("Закрыть диалог без сохранения изменений?"))
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
            await Task.WhenAll(
                RefreshShowcaseCategoriesAsync(),
                RefreshClustersAsync(),
                RefreshLocationsAsync(),
                RefreshAverageShowcaseQuantityLocationAsync(),
                RefreshClusterCategoryShowcaseAllowSetQuantityAsync());

            SetFactsShowcaseCategories(null);

            Title = "Планирование ассортимента на витринах";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            Result<List<ShowcaseCategoryDto>> result = await SaveAsync();

            if (result?.IsSuccess == true)
            {
                CloseOk();
            }
        }

        private async Task RefreshClusterCategoryShowcaseAllowSetQuantityAsync()
        {
            string allowSetQuantity = await WebClient.ExecuteApiRequestAsync(new QueryClusterCategoryShowcaseAllowSetQuantity());

            int.TryParse(allowSetQuantity, out int itemSet);

            _originalClusterCategoryShowcaseSetQuantity = ClusterCategoryShowcaseSetQuantity = itemSet > 0;
        }

        private async Task RefreshShowcaseCategoriesAsync()
        {
            List<ShowcaseCategoryDto> showcaseCategories = await WebClient.ExecuteApiRequestAsync(new QueryShowcaseCategories());

            ShowcaseCategories = showcaseCategories.Select(x => Mapper.Map<ShowcaseCategoryViewItem>(x)).ToObservableCollection();

            _originalShowcaseCategoryItems = ShowcaseCategories.Select(x => (ShowcaseCategoryViewItem)x.Clone()).ToArray();
        }

        private async Task RefreshClustersAsync()
        {
            IReadOnlyCollection<ClusterDto> clusters = await WebClient.ExecuteApiRequestAsync(new QueryClusters());

            Clusters = clusters.Select(x => Mapper.Map<ClusterViewItem>(x)).ToReadOnlyObservableCollection();
        }

        private async Task RefreshLocationsAsync()
        {
            List<LocationEntityDto> locations = await WebClient.ExecuteApiRequestAsync(new QueryLocations(), true);

            Locations = locations.Select(x => Mapper.Map<LocationViewItem>(x)).ToReadOnlyObservableCollection();
        }

        private async Task RefreshAverageShowcaseQuantityLocationAsync()
        {
            string averageShowcaseQuantityLocation = await WebClient.ExecuteApiRequestAsync(new QueryAverageShowcaseQuantityLocation());

            int.TryParse(averageShowcaseQuantityLocation, out int result);

            _averageShowcaseQuantityLocation = result > 0;
        }

        private async Task UpdateBusySecondsAfterCallAsync()
        {
            (await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new UpdateClusterCategoryShowcaseSettings(new UpdateClusterCategoryShowcaseSettingsDto()
                {
                    AllowSetQuantity = ClusterCategoryShowcaseSetQuantity
                })),
                "при сохранении настройки \"Запретить сохранение при отклонении от плана\"",
                "Настройка \"Запретить сохранение при отклонении от плана\" сохранена",
                this,
                true,
                showNotification: true))
                .IfNotNull(_ => _originalClusterCategoryShowcaseSetQuantity = ClusterCategoryShowcaseSetQuantity);
        }

        private async Task<Result<List<ShowcaseCategoryDto>>> SaveAsync()
        {
            bool isNotChangedShowcaseCategories = _originalShowcaseCategoryItems.Count == ShowcaseCategories.Count
                                                  && ShowcaseCategories.Any(x => x.IsChanged) != true;

            if (_originalClusterCategoryShowcaseSetQuantity != ClusterCategoryShowcaseSetQuantity)
            {
                await UpdateBusySecondsAfterCallAsync();

                if (isNotChangedShowcaseCategories)
                {
                    return Result.Success(Array.Empty<ShowcaseCategoryDto>().ToList());
                }
            }

            if (isNotChangedShowcaseCategories)
            {
                MessageFacadeService.ShowNotificationWarning("Нет изменений в планировании ассортимента на витринах");
                return default;
            }

            string allowSetQuantity = await WebClient.ExecuteApiRequestAsync(new QueryClusterCategoryShowcaseAllowSetQuantity());

            string clusterCategoryQuantityCheck = await WebClient.ExecuteApiRequestAsync(new QueryClusterCategoryShowcaseQuantityCheck());

            if ((int.TryParse(allowSetQuantity, out int itemSet) && itemSet > 0) || (int.TryParse(clusterCategoryQuantityCheck, out int item) && item > 0))
            {
                (bool Check, IReadOnlyCollection<string> Info) isDifferenceLocationQuantity = IsDifferenceLocationQuantity();

                if (isDifferenceLocationQuantity.Check)
                {
                     if (!ShowValidationResultView("Ошибки при сохранении", isDifferenceLocationQuantity.Info.Select(x => new ValidationResultItem(x, itemSet > 0))))
                     {
                         return default;
                     }
                }
            }

            List<ShowcaseCategorySaveDto> saveDtos = ShowcaseCategories.Select(x => new ShowcaseCategorySaveDto(x.Id, x.WarehouseId, x.CategoryId, x.Quantity, x.PlaceName)).ToList();

            Result<List<ShowcaseCategoryDto>> result = await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new UpdateShowcaseCategories(saveDtos)), "сохранении ассортимента", "Ассортимент сохранен", this, true);

            result.IfNotNull(r =>
            {

                IReadOnlyCollection<ShowcaseCategoryViewItem> savedShowcaseCategoryViewItems = r.Data.Select(x => Mapper.Map<ShowcaseCategoryViewItem>(x)).ToArray();

                IReadOnlyCollection<ShowcaseCategoryViewItem> forUpdateItems = savedShowcaseCategoryViewItems.Except(ShowcaseCategories).ToArray();

                foreach (ShowcaseCategoryViewItem viewItem in forUpdateItems)
                {
                    ShowcaseCategoryViewItem itemForUpdate = ShowcaseCategories.FirstOrDefault(x => x.Id == viewItem.Id);

                    if (itemForUpdate == null)
                    {
                        ShowcaseCategoryViewItem forUpdate = ShowcaseCategories
                            .FirstOrDefault(
                                x => x.Id == 0
                                     && x.CategoryId == viewItem.CategoryOldId &&
                                     x.WarehouseId == viewItem.WarehouseOldId && x.Quantity == viewItem.QuantityOld);

                        if (forUpdate != null)
                        {
                            itemForUpdate = forUpdate;
                            itemForUpdate.Id = viewItem.Id;
                        }
                    }

                    if (itemForUpdate != null && !viewItem.Equals(itemForUpdate))
                    {
                        itemForUpdate.QuantityOld = viewItem.QuantityOld;
                        itemForUpdate.WarehouseOldId = viewItem.WarehouseOldId;
                        itemForUpdate.CategoryOldId = viewItem.CategoryOldId;
                    }
                }

                SetFactsShowcaseCategories(null);

                RaisePropertiesChanged(nameof(ShowcaseCategories));

                _originalShowcaseCategoryItems = ShowcaseCategories.Select(x => (ShowcaseCategoryViewItem)x.Clone()).ToArray();
            });

            return result;
        }

        private void AddShowcaseCategory()
        {
           ShowcaseCategoryCreateViewModel viewModel = DialogDocumentManagerService.ShowView<ShowcaseCategoryCreateViewModel>(
               new ShowcaseCategoryCreateParameter(
                   ShowcaseCategories.Select(x => x.PlaceName).Distinct().ToArray(),
                   "Добавление плана в ассортимент"),
               this);

           if (!viewModel.IsOk)
           {
               return;
           }

           ShowcaseCategoryViewItem viewItem = new ShowcaseCategoryViewItem(
               viewModel.Quantity!.Value,
               viewModel.SelectedCategory.Id,
               viewModel.SelectedWarehouse!.Value.Id,
               viewModel.SelectedCategory.Name,
               viewModel.SelectedWarehouse.Value.DisplayValue,
               viewModel.PlaceName);

           if (ShowcaseCategories.Any(x => x.WarehouseId == viewItem.WarehouseId && x.CategoryId == viewItem.CategoryId && x.PlaceName == viewModel.PlaceName))
           {
               MessageFacadeService.ShowNotificationWarning("Такая категория для выбранного склада и места уже добавлена");
               return;
           }

           ShowcaseCategories.Add(viewItem);

           SetFactsShowcaseCategories(viewItem.CategoryId);
        }

        private void CopyShowcaseCategory()
        {
            ShowcaseCategoryCreateViewModel viewModel = DialogDocumentManagerService
                .ShowView<ShowcaseCategoryCreateViewModel>(
                    new ShowcaseCategoryCreateParameter(
                        ShowcaseCategories.Select(x => x.PlaceName).Distinct().ToArray(),
                        "Добавление плана в ассортимент",
                        SelectedShowcaseCategory),
                    this);

            if (!viewModel.IsOk)
            {
                return;
            }

            ShowcaseCategoryViewItem viewItem = new ShowcaseCategoryViewItem(
                viewModel.Quantity!.Value,
                viewModel.SelectedCategory.Id,
                viewModel.SelectedWarehouse!.Value.Id,
                viewModel.SelectedCategory.Name,
                viewModel.SelectedWarehouse.Value.DisplayValue,
                viewModel.PlaceName);

            if (ShowcaseCategories.Any(x => x.WarehouseId == viewItem.WarehouseId && x.CategoryId == viewItem.CategoryId && x.PlaceName == viewModel.PlaceName))
            {
                MessageFacadeService.ShowNotificationWarning("Такая категория для выбранного склада и места уже добавлена");
                return;
            }

            ShowcaseCategories.Add(viewItem);

            SetFactsShowcaseCategories(viewItem.CategoryId);
        }

        private void EditShowcaseCategory()
        {
            ShowcaseCategoryCreateViewModel viewModel = DialogDocumentManagerService.ShowView<ShowcaseCategoryCreateViewModel>(
                new ShowcaseCategoryCreateParameter(
                    ShowcaseCategories.Select(x => x.PlaceName).Distinct().ToArray(),
                    "Изменение плана в ассортименте",
                    SelectedShowcaseCategory),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (ShowcaseCategories.Any(x => x.Id != SelectedShowcaseCategory.Id && x.WarehouseId == viewModel.SelectedWarehouse!.Value.Id && x.CategoryId == viewModel.SelectedCategory.Id && x.PlaceName == viewModel.PlaceName))
            {
                MessageFacadeService.ShowNotificationWarning("Такая категория для выбранного склада и места уже существует");
                return;
            }

            SelectedShowcaseCategory.Quantity = viewModel.Quantity!.Value;
            SelectedShowcaseCategory.CategoryId = viewModel.SelectedCategory.Id;
            SelectedShowcaseCategory.CategoryName = viewModel.SelectedCategory.Name;
            SelectedShowcaseCategory.WarehouseId = viewModel.SelectedWarehouse!.Value.Id;
            SelectedShowcaseCategory.WarehouseName = viewModel.SelectedWarehouse.Value.DisplayValue;
            SelectedShowcaseCategory.PlaceName = viewModel.PlaceName;

            SetFactsShowcaseCategories(SelectedShowcaseCategory.CategoryId);
        }

        private void DeleteShowcaseCategory()
        {
            int? categoryId = SelectedShowcaseCategory.CategoryId;

            ShowcaseCategories.Remove(SelectedShowcaseCategory);

            SetFactsShowcaseCategories(categoryId);
        }

        private void HandleRowDoubleClick(RowDoubleClickInfo e)
        {
            ShowcaseCategoryViewItem selectedItem = (ShowcaseCategoryViewItem)e.Data;

            if (!e.FieldName.Equals(nameof(ShowcaseCategoryViewItem.Id)))
            {
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<ShowcaseCategoryHistoryViewModel>(new ShowcaseCategoryHistoryParameter(null, null, new[] { selectedItem.WarehouseId }, selectedItem.CategoryId), this);
        }

        private void SetFactsShowcaseCategories(int? categoryId)
        {
            ShowcaseCategories.Where(x => !categoryId.HasValue || x.CategoryId == categoryId)
                .AsParallel().ForEach(
                x =>
                {
                    ClusterViewItem clusterViewItem = Clusters.FirstOrDefault(y => y.Id == x.ClusterId);

                    IReadOnlyCollection<int> locationIds = clusterViewItem?.LocationIds;

                    IReadOnlyCollection<int> warehouseByClusterIds = Locations.Where(y => locationIds?.Contains(y.Id) == true).SelectMany(y => y.WarehouseIds)
                        .ToArray();

                    x.CountLocations = clusterViewItem?.LocationIds?.Length ?? 0;

                    x = SetQuantityClusterFact(x, warehouseByClusterIds);
                    x = SetQuantityLocationFact(x);

                    if (_averageShowcaseQuantityLocation)
                    {
                        x = SetAverageQuantityLocation(x, warehouseByClusterIds);
                    }
                });
        }

        private ShowcaseCategoryViewItem SetQuantityClusterFact(ShowcaseCategoryViewItem currentItem, IReadOnlyCollection<int> warehouseIds)
        {
            IReadOnlyCollection<ShowcaseCategoryViewItem> showcaseCategoriesForSum = ShowcaseCategories
                .Where(x =>
                    warehouseIds.Contains(x.WarehouseId)
                    && x.CategoryId == currentItem.CategoryId)
                .ToArray();

            currentItem.QuantityClusterFact = currentItem.ClusterId.HasValue
                ? showcaseCategoriesForSum.Sum(x => x.Quantity)
                : 0;

            return currentItem;
        }

        private ShowcaseCategoryViewItem SetQuantityLocationFact(ShowcaseCategoryViewItem currentItem)
        {
            LocationViewItem location = Locations.FirstOrDefault(x => x.WarehouseIds.Contains(currentItem.WarehouseId));

            int[] warehousesByLocation = location!.WarehouseIds;

            currentItem.QuantityLocationFact = ShowcaseCategories
                .Where(x => x.CategoryId == currentItem.CategoryId && warehousesByLocation.Contains(x.WarehouseId))
                .Sum(x => x.Quantity);

            return currentItem;
        }

        private ShowcaseCategoryViewItem SetAverageQuantityLocation(ShowcaseCategoryViewItem currentItem, IReadOnlyCollection<int> warehouseIds)
        {
            IDictionary<int, int> showcaseCategoriesForAverage = ShowcaseCategories
                .Where(x =>
                    x.LocationId.HasValue
                    && warehouseIds.Contains(x.WarehouseId)
                    && x.CategoryId == currentItem.CategoryId)
                .GroupBy(x => x.LocationId.Value)
                .ToDictionary(x => x.Key, y => y.Sum(x => x.Quantity));

            currentItem.AverageQuantityLocation = showcaseCategoriesForAverage.Any()
                ? showcaseCategoriesForAverage.Average(x => x.Value)
                : 0;

            return currentItem;
        }

        private (bool Check, IReadOnlyCollection<string> Info) IsDifferenceLocationQuantity()
        {
            StringBuilder stringBuilder = new StringBuilder(100);
            List<string> listErrors = new List<string>();

            bool result = false;

            foreach (var cluster in Clusters.Where(x => x.LocationIds?.Length > 0))
            {
                IReadOnlyCollection<ShowcaseCategoryViewItem> clusterShowcaseCategories = ShowcaseCategories
                    .Where(x => x.ClusterId == cluster.Id && x.LocationId.HasValue)
                    .ToArray();

                foreach (var categoryShowcaseCategories in clusterShowcaseCategories
                             .GroupBy(x => x.CategoryId)
                             .ToDictionary(x => x.Key, y => y.ToArray()))
                {
                    if (categoryShowcaseCategories.Value?.Any(x => x.IsChanged || x.Id == 0) == true)
                    {
                        IReadOnlyDictionary<int, int> locationSums = categoryShowcaseCategories.Value
                            .GroupBy(x => x.LocationId!.Value)
                            .ToDictionary(x => x.Key, y => y.Sum(x => x.Quantity));

                        int fact = categoryShowcaseCategories.Value.First().QuantityClusterFact;
                        int plan = categoryShowcaseCategories.Value.First().QuantityClusterPlan;

                        if (locationSums.Select(x => x.Value).Distinct().Count() > 1 || locationSums.Count != cluster.LocationIds.Length)
                        {
                            result = true;

                            stringBuilder.Append("Не соответствуєт СКЮ фактическое (").Append(fact).Append(") плановому (").Append(plan).Append(") кластера \"").Append(cluster.Name).Append("\" и категории \"")
                                .Append(categoryShowcaseCategories.Value.First().CategoryName).AppendLine("\":");

                            foreach (int locationId in cluster.LocationIds)
                            {
                                locationSums.TryGetValue(locationId, out int sum);

                                stringBuilder.Append(Locations.FirstOrDefault(x => x.Id == locationId)?.Name).Append(": СКЮ = ")
                                    .AppendLine(sum.ToString());
                            }

                            listErrors.Add(stringBuilder.ToString());
                        }
                    }

                    stringBuilder.Clear();
                }
            }

            return (result, listErrors.ToArray());
        }
    }
}