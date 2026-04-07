using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Editors;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Locations;
using Telemart.Client.Data.Requests.Features.Showcase;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Showcase;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Locations;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Showcase
{
    public sealed class ShowcaseClusterViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper _mapper;
        private readonly IErrorHandler _errorHandler;
        private readonly List<int> _showcaseClusterForDelete;

        private ReadOnlyObservableCollection<ShowcaseClusterViewItem> _originalShowcaseClusterViewItems;
        private List<ShowcaseCategoryDto> _showcaseCategories;

        public ShowcaseClusterViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messagefacadeService,
            IMapper mapper,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messagefacadeService)
        {
            _mapper = mapper;
            _errorHandler = errorHandler;

            SaveCommand = new AsyncCommand(SaveAsync);
            SearchByFilterCommand = new AsyncCommand<EditValueChangedEventArgs>(SearchByFilterAsync);
            AllowSearchByFilterCommand = new DelegateCommand<EditValueChangingEventArgs>(AllowSearchByFilter);
            AddShowcaseClusterCommand = new AsyncCommand(AddShowcaseClusterAsync);
            HandleNodeCheckStateChangedCommand = new DelegateCommand(HandleNodeCheckStateChanged);
            DeleteShowcaseClusterCommand = new DelegateCommand(DeleteShowcaseCluster, () => SelectedShowcaseCluster != null);
            ShowcaseClusterFillCommand = new DelegateCommand(ShowcaseClusterFill, CanShowcaseClusterFill);

            ShowcaseClusters = new ObservableCollection<ShowcaseClusterViewItem>();
            _showcaseClusterForDelete = new List<int>();
        }

        public ObservableCollection<ShowcaseClusterViewItem> ShowcaseClusters
        {
            get { return GetProperty(() => ShowcaseClusters); }
            private set { SetProperty(() => ShowcaseClusters, value); }
        }

        public ReadOnlyObservableCollection<LocationViewItem> Locations
        {
            get { return GetProperty(() => Locations); }
            set { SetProperty(() => Locations, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ClusterViewItem> Clusters
        {
            get { return GetProperty(() => Clusters); }
            private set { SetProperty(() => Clusters, value); }
        }

        public ReadOnlyObservableCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ObservableCollection<int> SelectedCategoryIds
        {
            get { return GetProperty(() => SelectedCategoryIds); }
            set { SetProperty(() => SelectedCategoryIds, value); }
        }

        public ShowcaseClusterViewItem SelectedShowcaseCluster
        {
            get { return GetProperty(() => SelectedShowcaseCluster); }
            set { SetProperty(() => SelectedShowcaseCluster, value); }
        }

        public IAsyncCommand SaveCommand { get; }

        public IAsyncCommand SearchByFilterCommand { get; }

        public IDelegateCommand AllowSearchByFilterCommand { get; }

        public IAsyncCommand AddShowcaseClusterCommand { get; }

        public IDelegateCommand HandleNodeCheckStateChangedCommand { get; }

        public IDelegateCommand DeleteShowcaseClusterCommand { get; }

        public IDelegateCommand ShowcaseClusterFillCommand { get; }

        public override int MinHeight => 570;

        public override int Height => 645;

        public override int MaxHeight => 1080;

        public override int MinWidth => 710;

        public override int Width => 1000;

        public override int MaxWidth => 1920;

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(
                RefreshLocationsAsync(),
                RefreshCategoriesAsync(),
                RefreshClustersAsync(),
                RefreshEmployeesAsync(),
                RefreshShowcaseCategoriesAsync());

            Title = "Планирование ассортимента по кластерам";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (await SaveAsync())
            {
                CloseOk();
            }
        }

        private async Task RefreshShowcaseClustersAsync()
        {
            if (SelectedCategoryIds?.Count > 0)
            {
                List<ShowcaseClusterDto> showcaseClusters = await WebClient.ExecuteApiRequestAsync(new QueryShowcaseClusters(SelectedCategoryIds?.ToArray()));

                ShowcaseClusters.AddRange(showcaseClusters
                    .Select(x => _mapper.Map<ShowcaseClusterViewItem>(x)));

                _originalShowcaseClusterViewItems = ShowcaseClusters?.Select(x => (ShowcaseClusterViewItem)x.Clone()).ToReadOnlyObservableCollection();
            }
        }

        private async Task RefreshLocationsAsync()
        {
            List<LocationEntityDto> locations = await WebClient.ExecuteApiRequestAsync(new QueryLocations(), true);

            Locations = locations.Select(x => _mapper.Map<LocationViewItem>(x)).ToReadOnlyObservableCollection();
        }

        private async Task RefreshCategoriesAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories
                .Where(x => x.Active > 0 && x.IsParent)
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<CategoryViewItem>(x))
                .ToReadOnlyObservableCollection();

            Categories.ForEach(x => x.Selected = false);
        }

        private async Task RefreshClustersAsync()
        {
            IReadOnlyCollection<ClusterDto> clusters = await WebClient.ExecuteApiRequestAsync(new QueryClusters());

            Clusters = clusters.Select(x => _mapper.Map<ClusterViewItem>(x)).ToReadOnlyObservableCollection();
        }

        private async Task RefreshEmployeesAsync()
        {
            PagedResult<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

            Employees = employees.Data
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshShowcaseCategoriesAsync()
        {
            _showcaseCategories = await WebClient.ExecuteApiRequestAsync(new QueryShowcaseCategories());
        }

        private async Task<bool> SaveAsync()
        {
            IReadOnlyCollection<ShowcaseClusterSaveDto> saveDtos = ShowcaseClusters
                .Select(x => _mapper.Map<ShowcaseClusterSaveDto>(x)).ToArray();

            Result result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new UpdateShowcaseClusters(saveDtos, _showcaseClusterForDelete)),
                "сохранении ассортимента по кластерам",
                "Ассортимент по кластерам сохранен",
                this,
                true);

            if (result.IsSuccess)
            {
                ShowcaseClusters.Clear();

                _showcaseClusterForDelete.Clear();

                await Task.WhenAll(RefreshShowcaseClustersAsync(), RefreshShowcaseCategoriesAsync());
            }

            return result.IsSuccess;
        }

        private void ShowcaseClusterFill()
        {
            IReadOnlyCollection<ClusterViewItem> notAddedClusters = Clusters
                .Where(x => ShowcaseClusters?.Any(y => y.ClusterId == x.Id) != true)
                .ToArray();

            ShowcaseClusters ??= new ObservableCollection<ShowcaseClusterViewItem>();

            foreach (ClusterViewItem cluster in notAddedClusters.Where(x => x.LocationIds?.Length > 0))
            {
                int[] locationIds = Clusters.FirstOrDefault(x => x.Id == cluster.Id)?.LocationIds;

                int[] warehouseIds = Locations.Where(x => locationIds?.Contains(x.Id) == true).SelectMany(x => x.WarehouseIds)
                    .ToArray();

                SelectedCategoryIds?.ForEach(
                    y =>
                    {
                        IReadOnlyCollection<ShowcaseCategoryDto> showcaseCategoriesForSum = _showcaseCategories
                            .Where(x => warehouseIds.Contains(x.WarehouseId) && x.CategoryId == y)
                            .ToArray();

                        ShowcaseClusterViewItem item = new ShowcaseClusterViewItem()
                        {
                            ClusterId = cluster.Id,
                            CategoryId = y,
                            QuantityLocation = 0,
                            LocationIds = cluster.LocationIds?.ToObservableCollection() ?? Array.Empty<int>().ToObservableCollection(),
                            QuantityClusterFact = showcaseCategoriesForSum.Sum(x => x.Quantity)
                        };

                        ShowcaseClusters.Add(item);
                    });
            }
        }

        private bool CanShowcaseClusterFill()
        {
            return Clusters?.All(x => ShowcaseClusters?.Any(y => y.ClusterId == x.Id) == true) != true
                   && SelectedCategoryIds?.Count > 0;
        }

        private Task AddShowcaseClusterAsync()
        {
            ShowcaseClusterCreateViewModel viewModel = DialogDocumentManagerService.ShowView<ShowcaseClusterCreateViewModel>(
                new ShowcaseClusterCreateParameter(
                    "Добавление плана по кластеру",
                    SelectedCategoryIds?.Count == 1 ? SelectedCategoryIds.First() : null,
                    SelectedShowcaseCluster),
                this);

            if (!viewModel.IsOk)
            {
                return Task.CompletedTask;
            }

            if (ShowcaseClusters.Any(x => x.CategoryId == viewModel.SelectedCategory.Id && x.ClusterId == viewModel.SelectedCluster!.Value.Id))
            {
                ShowcaseClusterViewItem existedItem = ShowcaseClusters.First(
                    x => x.CategoryId == viewModel.SelectedCategory.Id
                         && x.ClusterId == viewModel.SelectedCluster!.Value.Id);

                existedItem.QuantityLocation = viewModel.Quantity!.Value;
            }
            else
            {
                int[] locationIds = Clusters.FirstOrDefault(x => x.Id == viewModel.SelectedCluster!.Value.Id)?.LocationIds;

                int[] warehouseIds = Locations.Where(x => locationIds?.Contains(x.Id) == true).SelectMany(x => x.WarehouseIds)
                    .ToArray();

                IReadOnlyCollection<ShowcaseCategoryDto> showcaseCategoriesForSum = _showcaseCategories
                    .Where(x => warehouseIds.Contains(x.WarehouseId) && x.CategoryId == viewModel.SelectedCategory?.Id)
                    .ToArray();

                ShowcaseClusterViewItem item = new ShowcaseClusterViewItem()
                {
                    ClusterId = viewModel.SelectedCluster!.Value.Id,
                    CategoryId = viewModel.SelectedCategory.Id,
                    QuantityLocation = viewModel.Quantity!.Value,
                    LocationIds = locationIds?.ToObservableCollection() ?? Array.Empty<int>().ToObservableCollection(),
                    QuantityClusterFact = showcaseCategoriesForSum.Sum(x => x.Quantity)
                };

                ShowcaseClusters.Add(item);
            }

            return Task.CompletedTask;
        }

        private void DeleteShowcaseCluster()
        {
            if (SelectedShowcaseCluster.Id > 0)
            {
                _showcaseClusterForDelete.Add(SelectedShowcaseCluster.Id);
            }

            ShowcaseClusters.Remove(SelectedShowcaseCluster);
        }

        private async Task SearchByFilterAsync(EditValueChangedEventArgs args)
        {
            if (args.NewValue == SelectedCategoryIds)
            {
                ShowcaseClusters?.Clear();
                _showcaseClusterForDelete.Clear();

                if (SelectedCategoryIds == null)
                {
                    Categories.Where(x => x.Selected == true).ForEach(x => x.Selected = false);

                    return;
                }

                await RefreshShowcaseClustersAsync();
            }
        }

        private void AllowSearchByFilter(EditValueChangingEventArgs args)
        {
            _originalShowcaseClusterViewItems ??= Array.Empty<ShowcaseClusterViewItem>().ToReadOnlyObservableCollection();

            if (ShowcaseClusters.Count != _originalShowcaseClusterViewItems.Count || _originalShowcaseClusterViewItems.Except(ShowcaseClusters).Any())
            {
                if (!MessageFacadeService.Confirm($"Есть не сохраненные данные.{Environment.NewLine}Продолжить?"))
                {
                    args.IsCancel = true;
                    var a = args.OldValue as CategoryViewItem;
                }

                return;
            }

            args.IsCancel = false;
        }

        private void HandleNodeCheckStateChanged()
        {
            if (Categories?.Count > 0)
            {
                SelectedCategoryIds = Categories
                    .Where(x => x.Selected.HasValue && x.Selected.Value)
                    .Select(x => x.Id)
                    .ToObservableCollection();
            }
        }
    }
}