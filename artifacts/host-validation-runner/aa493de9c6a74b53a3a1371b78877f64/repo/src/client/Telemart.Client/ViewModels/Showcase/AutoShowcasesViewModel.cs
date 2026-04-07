using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Data;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.Xpf;
using DevExpress.Xpf.Grid;
using DynamicData;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements.Grid;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Locations;
using Telemart.Client.Data.Requests.Features.Segment;
using Telemart.Client.Data.Requests.Features.Showcase;
using Telemart.Client.Data.Requests.Features.Showcase.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Segment;
using Telemart.Client.TransferObjects.Showcase;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Locations;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.Validation;
using Telemart.Client.ViewModels.Warehouse.Movement;
using Telemart.Client.Views.Warehouse.Movement;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Showcase
{
    public sealed class AutoShowcasesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IMapper _mapper;
        private readonly List<int> _deletedIds;
        private IReadOnlyDictionary<int, string> _employeeNames;
        private IReadOnlyDictionary<int, string> _warehouseNames;
        private IReadOnlyDictionary<int, string> _allWarehouseNames;
        private IReadOnlyDictionary<int, string> _segmentNames;
        private IReadOnlyDictionary<int, int> _categoryEmployees;
        private IReadOnlyCollection<CategoryDto> _categories;
        private IReadOnlyCollection<int> _currentWarehouseIdsInGrid;
        private int _rowsCountAfterRefresh;
        private Dictionary<(int warehouseId, int productId), int> _showcasesCapacitiesBeforeEdit;
        private ObservableCollection<string> _groupSummaryItemNames;
        private ObservableCollection<int> _selectedWarehouseIds;

        public AutoShowcasesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMapper mapper,
            ProductInformationViewModel productInformationViewModel)
            : base(webClient, dictionaries, messageFacadeService)
        {
            SaveCommand = new AsyncCommand(SaveAsync);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            CalculateAutoShowcaseCommand = new AsyncCommand(CalculateAutoShowcaseAsync, () => WebClient.IsOperationAllowed(BusinessOperation.ShowcaseCalculateAuto));
            CalculateAutoShowcaseByCategoriesCommand = new AsyncCommand(CalculateAutoShowcaseByCategoriesAsync, () => WebClient.IsOperationAllowed(BusinessOperation.ShowcaseCalculateAutoByCategories));
            DeleteCommand = new DelegateCommand(Delete, () => CurrentRow is not null);
            AddCommand = new AsyncCommand(AddAsync);
            ShowcaseCategoryCommand = new DelegateCommand(ShowcaseCategory);
            ShowcaseClusterCommand = new DelegateCommand(ShowcaseCluster);
            CustomSummaryCommand = new DelegateCommand<RowSummaryArgs>(CustomSummary);

            ProductInformation = productInformationViewModel;
            Filter = new AutoShowcaseFilterViewModel(webClient, mapper);

            _deletedIds = new List<int>();
            _currentWarehouseIdsInGrid = Array.Empty<int>();
            _errorHandler = errorHandler;
            _mapper = mapper;
            _showcasesCapacitiesBeforeEdit = new Dictionary<(int warehouseId, int productId), int>();
            _groupSummaryItemNames = new ObservableCollection<string>();
            _selectedWarehouseIds = new ObservableCollection<int>();

            CanCreate = WebClient.IsOperationAllowed(BusinessOperation.ShowcaseCreate);
            CanDelete = WebClient.IsOperationAllowed(BusinessOperation.ShowcaseDelete);
            AutoButtonVisible = WebClient.IsOperationAllowed(BusinessOperation.ShowcaseCalculateAuto) || WebClient.IsOperationAllowed(BusinessOperation.ShowcaseCalculateAutoByCategories);
            HandleOutQuantityChangedCommand = new DelegateCommand<CellValueChangedEventArgs>(ChangeOutQuantity);
            ShowcaseRoutesCommand = new DelegateCommand(ShowcaseRoutes);
        }

        public IAsyncCommand SaveCommand { get; }

        public IAsyncCommand CalculateAutoShowcaseCommand { get; }

        public IAsyncCommand CalculateAutoShowcaseByCategoriesCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand AddCommand { get; }

        public IDelegateCommand DeleteCommand { get; }

        public IDelegateCommand ShowcaseCategoryCommand { get; }

        public IDelegateCommand ShowcaseClusterCommand { get; }

        public IDelegateCommand CustomSummaryCommand { get; }

        public IDelegateCommand HandleOutQuantityChangedCommand { get; }

        public IDelegateCommand ShowcaseRoutesCommand { get; }

        public ReadOnlyObservableCollection<LocationViewItem> Locations
        {
            get { return GetProperty(() => Locations); }
            set { SetProperty(() => Locations, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public bool CanCreate
        {
            get { return GetProperty(() => CanCreate); }
            private set { SetProperty(() => CanCreate, value); }
        }

        public bool CanDelete
        {
            get { return GetProperty(() => CanDelete); }
            private set { SetProperty(() => CanDelete, value); }
        }

        public AutoShowcaseFilterViewModel Filter
        {
            get { return GetProperty(() => Filter); }
            private set { SetProperty(() => Filter, value); }
        }

        public ObservableRangeCollection<GridBandItem> Bands
        {
            get { return GetProperty(() => Bands); }
            private set { SetProperty(() => Bands, value); }
        }

        public ObservableCollection<GridSummaryItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ObservableCollection<ExpandoObject> Showcases
        {
            get { return GetProperty(() => Showcases); }
            set { SetProperty(() => Showcases, value); }
        }

        public ObservableRangeCollection<FormattingRule> FormattingRules
        {
            get { return GetProperty(() => FormattingRules); }
            private set { SetProperty(() => FormattingRules, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private set { SetProperty(() => ProductInformation, value); }
        }

        public IDictionary<string, object> CurrentRow
        {
            get { return GetProperty(() => CurrentRow); }
            set { SetProperty(() => CurrentRow, value, CurrentProductChangedCallback); }
        }

        public ColumnBase CurrentColumn
        {
            get { return GetProperty(() => CurrentColumn); }
            set { SetProperty(() => CurrentColumn, value); }
        }

        public bool AutoButtonVisible
        {
            get { return GetProperty(() => AutoButtonVisible); }
            set { SetProperty(() => AutoButtonVisible, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool IsProductInfoClosed
        {
            get { return GetProperty(() => IsProductInfoClosed); }
            set { SetProperty(() => IsProductInfoClosed, value); }
        }

        public bool VisibleHistory => WebClient.IsOperationAllowed(BusinessOperation.ShowcaseHistoryAllowOpen);

        public IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public IDocumentManagerService NonModalDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalDialogDocumentManagerService");

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsProductInfoClosed = !IsProductInfoClosed;
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Refresh:
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;
                    case HotkeyMessageType.Add when CanCreate:
                        AddCommand.Execute(null);
                        handled = true;
                        break;
                    case HotkeyMessageType.Delete when CanDelete:
                        DeleteCommand.Execute(null);
                        handled = true;
                        break;

                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            IsProductInfoClosed = false;

            await Task.WhenAll(Filter.RefreshAsync(), RefreshLocationsAsync(), RefreshWarehousesAsync());
            Filter.ResetFilterValues();

            Task<PagedResult<EmployeeDto>> employeesTask = WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);
            Task<PagedResult<WarehouseDto>> warehousesTask = WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);
            Task<PagedResult<CategoryDto>> categoriesTask = WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);
            Task<List<SegmentDto>> segmentsTask = WebClient.ExecuteApiRequestAsync(new QuerySegments(), true);

            await Task.WhenAll(employeesTask, warehousesTask, categoriesTask, segmentsTask);

            _employeeNames = employeesTask.Result.Data.ToDictionary(x => x.Id, x => x.Name);
            _allWarehouseNames = warehousesTask.Result.Data.ToDictionary(x => x.Id, x => x.Name);
            _warehouseNames = warehousesTask.Result.Data.Where(x => x.Active == 1 && (x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.ShowCase.Id)).ToDictionary(x => x.Id, x => x.Name);
            _categoryEmployees = categoriesTask.Result.Data.ToDictionary(x => x.Id, x => x.EmployeeId);
            _segmentNames = segmentsTask.Result.ToDictionary(x => x.Id, x => x.Name);
            _categories = categoriesTask.Result.Data;

            await base.HandleLoadedAsync();
        }

        private async Task RefreshLocationsAsync()
        {
            List<LocationEntityDto> locations = await WebClient.ExecuteApiRequestAsync(new QueryLocations(), true);

            Locations = locations.Where(x => x.ClusterId.HasValue)
                .Select(x => _mapper.Map<LocationViewItem>(x))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshWarehousesAsync()
        {
            int[] warehouseTypeIds = new[] { WarehouseKind.Pickup.Id, WarehouseKind.ShowCase.Id };

            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
            Warehouses = warehouses.Where(x => warehouseTypeIds.Contains(x.TypeId)).ToReadOnlyObservableCollection();
        }

        private static void CalculateSummaryCapacityByWarehouses(IReadOnlyCollection<AutoShowcaseDto> autoShowcaseDtos)
        {
            foreach (IGrouping<(int WarehouseId, string Category), AutoShowcaseDto> autoShowcaseWarehouses in
                     autoShowcaseDtos.GroupBy(x => (x.WarehouseId, x.CategoryNameUkr)))
            {
                int sumCapacity = autoShowcaseWarehouses.Sum(x => x.Capacity);

                autoShowcaseWarehouses.ForEach(x => { x.SetSumCapacity(sumCapacity); });
            }
        }

        private static bool IsShowcaseChanged(ExpandoObject product)
        {
            return GetTrackableValues(product).Any(v => v.IsChanged);
        }

        private static IEnumerable<ITrackableValue> GetTrackableValues(ExpandoObject product)
        {
            return product.Where(pair => pair.Key.StartsWith(GridColumnTemplateSelector.WarehousePrefix)).Select(pair => (ITrackableValue)pair.Value);
        }

        private static string GetWarehousePropertyName(int warehouseId, string fieldName)
        {
            return $"{GridColumnTemplateSelector.WarehousePrefix}{warehouseId}_{fieldName}";
        }

        private static int GetIntValueByColumnName(IDictionary<string, object> product, string columnName)
        {
            NullableIntPropertyValue property = (NullableIntPropertyValue)product[columnName];
            return property.Value!.Value;
        }

        private static IReadOnlyCollection<int> GetAllIdsFromRow(IDictionary<string, object> product)
        {
            return product.Where(x => x.Key.EndsWith($"_{nameof(AutoShowcaseDto.Id)}")).Select(x => ((NullableIntPropertyValue)x.Value).Value ?? 0).Where(x => x > 0).ToArray();
        }

        private IEnumerable<FormattingRule> GetFormattingRules(IEnumerable<string> properties)
        {
            const string ChangedToValueName = nameof(NullableIntPropertyValue.IsChangedToValue);
            const string IsChangedFromEmptyName = nameof(NullableIntPropertyValue.IsChangedFromEmpty);
            const string IsChangedToEmptyName = nameof(NullableIntPropertyValue.IsChangedToEmpty);
            const string valueName = nameof(NullableIntPropertyValue.Value);
            const string WarehouseCategoryPlanName = nameof(AutoShowcaseDto.WarehouseCategoryPlan);
            const string SumCapacityName = nameof(AutoShowcaseDto.SumCapacity);

            string canBuyProperty = properties.First(x => x == nameof(AutoShowcaseDto.CanBuy));

            yield return new FormattingRule($"{GridColumnHelper.FieldNamePrefix}{canBuyProperty}", $"[{canBuyProperty}.{valueName}] = 'нет'", false, default, KnownColor.Red);

            foreach (string x in properties.Where(x => x.StartsWith(GridColumnTemplateSelector.WarehousePrefix) && !x.Contains(nameof(AutoShowcaseDto.SumCapacity))))
            {
                string fieldName = $"{GridColumnHelper.FieldNamePrefix}{x}";

                yield return new FormattingRule(fieldName, $"[{x}.{ChangedToValueName}] = True", false, PropertyChangeType.Changed);
                yield return new FormattingRule(fieldName, $"[{x}.{IsChangedFromEmptyName}] = True", false, PropertyChangeType.FromEmpty);
                yield return new FormattingRule(fieldName, $"[{x}.{IsChangedToEmptyName}] = True", false, PropertyChangeType.ToEmpty);
            }

            int[] warehouseIds = _warehouseNames?.Keys.ToArray() ?? Array.Empty<int>();

            foreach (int id in warehouseIds)
            {
                string[] propertiesByWarehouse = properties.Where(x => x.StartsWith($"{GridColumnTemplateSelector.WarehousePrefix}{id}_")).ToArray();

                string properySumCapacity = propertiesByWarehouse.FirstOrDefault(x => x.Contains(SumCapacityName));
                string propertyWarehouseCategoryPlan = propertiesByWarehouse.FirstOrDefault(x => x.Contains(WarehouseCategoryPlanName));

                yield return new FormattingRule($"{GridColumnHelper.FieldNamePrefix}{properySumCapacity}", $"[{properySumCapacity}.{valueName}] > [{propertyWarehouseCategoryPlan}.{valueName}]", false, default, KnownColor.Red);
            }
        }

        private IEnumerable<AutoShowcaseSaveDto> GetSaveDtos(IEnumerable<ExpandoObject> changedObjects)
        {
            foreach (ExpandoObject obj in changedObjects)
            {
                int productId = GetIntValueByColumnName(obj, nameof(AutoShowcaseDto.ProductId));

                foreach (IGrouping<string, KeyValuePair<string, object>> warehouseGroup in obj.Where(x => x.Key.StartsWith(GridColumnTemplateSelector.WarehousePrefix)).GroupBy(x => x.Key.Split("_")[1]))
                {
                    int capacity = ((NullableIntPropertyValue)warehouseGroup.First(x => x.Key.Contains(nameof(AutoShowcaseDto.Capacity)) && !x.Key.Contains(nameof(AutoShowcaseDto.SumCapacity))).Value).Value ?? 0;
                    int warehouseId = ((NullableIntPropertyValue)warehouseGroup.First(x => x.Key.Contains(nameof(AutoShowcaseDto.WarehouseId))).Value).Value ?? 0;
                    int id = ((NullableIntPropertyValue)warehouseGroup.First(x => x.Key.Contains(nameof(AutoShowcaseDto.Id))).Value).Value ?? 0;
                    int modifiedBy = ((StringPropertyValue)warehouseGroup.First(x => x.Key.Contains(nameof(AutoShowcaseDto.ModifiedBy))).Value).EntityId!.Value;
                    int createdBy = ((StringPropertyValue)warehouseGroup.First(x => x.Key.Contains(nameof(AutoShowcaseDto.CreatedBy))).Value).EntityId!.Value;

                    if (_showcasesCapacitiesBeforeEdit.TryGetValue((warehouseId, productId), out int capacityBeforeEdit) && capacityBeforeEdit != capacity)
                    {
                        modifiedBy = WebClient.AuthenticatedEmployee.Id;
                    }

                    yield return new AutoShowcaseSaveDto(id, warehouseId, productId, capacity, modifiedBy, createdBy);
                }
            }
        }

        private IEnumerable<GridBandItem> GetBands(IReadOnlyCollection<AutoShowcaseDto> autoShowcaseDtos)
        {
            SummaryItems.Add(new GridSummaryItem()
            {
                SummaryType = SummaryItemType.Count,
                Alignment = GridSummaryItemAlignment.Left,
                Visible = true
            });

            foreach (GridBandItem band in GetPermanentBands())
            {
                yield return band;
            }

            foreach (AutoShowcaseDto warehouse in autoShowcaseDtos.GroupBy(x => x.WarehouseId).Select(x => x.First()))
            {
                GridBandItem warehouseGroupBand = new GridBandItem(_allWarehouseNames[warehouse.WarehouseId]);

                GridColumnItem categoryPlanColumn = new GridColumnItem(
                    GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.WarehouseCategoryPlan)),
                    "ПК",
                    true,
                    headerToolTip: "План по категории",
                    width: 50);

                GridColumnItem freeQuantityColumn = new GridColumnItem(
                    GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.WarehouseQuantityFree)),
                    "СО",
                    true,
                    headerToolTip: "Свободный остаток",
                    width: 29);

                GridColumnItem warehouseSalesQuantityQuantityColumn = new GridColumnItem(
                    GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.SalesQuantityByWarehouse)),
                    "П",
                    true,
                    headerToolTip: "Продажи по складу за 14 дней",
                    width: 29);

                GridColumnItem summColumnPlan = new GridColumnItem(
                    GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.SumCapacity)),
                    "План (по категории)",
                    true,
                    width: 45,
                    headerToolTip: "План (по категории)");

                GridColumnItem planColumn = new GridColumnItem(
                    GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.Capacity)),
                    "План",
                    true,
                    true,
                    width: 42);

                GridColumnItem modifiedByColumn = new GridColumnItem(
                    GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.ModifiedBy)),
                    "Р",
                    true,
                    headerToolTip: "Редактировал",
                    width: 29,
                    horizontalContentAlignment: HorizontalContentAlignment.Right);

                GridColumnItem createdByColumn = new GridColumnItem(
                    GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.CreatedBy)),
                    "С",
                    true,
                    headerToolTip: "Создал",
                    width: 29,
                    horizontalContentAlignment: HorizontalContentAlignment.Right);

                GridColumnItem idColumn = new GridColumnItem(
                    GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.Id)),
                    nameof(AutoShowcaseDto.Id),
                    false,
                    columnVisible: false,
                    showInColumnChooser: false);

                GridColumnItem warehouseIdColumn = new GridColumnItem(
                    GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.WarehouseId)),
                    nameof(AutoShowcaseDto.WarehouseId),
                    false,
                    columnVisible: false,
                    showInColumnChooser: false);

                warehouseGroupBand.Columns.Add(idColumn);
                warehouseGroupBand.Columns.Add(warehouseIdColumn);
                warehouseGroupBand.Columns.Add(freeQuantityColumn);
                warehouseGroupBand.Columns.Add(warehouseSalesQuantityQuantityColumn);
                warehouseGroupBand.Columns.Add(categoryPlanColumn);
                warehouseGroupBand.Columns.Add(summColumnPlan);
                warehouseGroupBand.Columns.Add(planColumn);
                warehouseGroupBand.Columns.Add(modifiedByColumn);
                warehouseGroupBand.Columns.Add(createdByColumn);

                _groupSummaryItemNames.Add(GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.WarehouseCategoryPlan)));

                yield return warehouseGroupBand;
            }

            static IEnumerable<GridBandItem> GetPermanentBands()
            {
                GridBandItem productBand = new GridBandItem("Товар", true);

                productBand.Columns.Add(new GridColumnItem(nameof(AutoShowcaseDto.ProductId), "Код", true, false, "Код товара", horizontalContentAlignment: HorizontalContentAlignment.Left));
                productBand.Columns.Add(new GridColumnItem(nameof(AutoShowcaseDto.ProductNameUkr), "Название", false));

                yield return productBand;

                GridBandItem analyticsBand = new GridBandItem("Аналитика", true);

                analyticsBand.Columns.Add(new GridColumnItem(nameof(AutoShowcaseDto.CategoryNameUkr), "Категория", false));
                analyticsBand.Columns.Add(new GridColumnItem(nameof(AutoShowcaseDto.SegmentId), "Сегмент", false));
                analyticsBand.Columns.Add(new GridColumnItem(nameof(AutoShowcaseDto.CategoryEmployeeId), "Ответственный", false, horizontalContentAlignment: HorizontalContentAlignment.Left));
                analyticsBand.Columns.Add(new GridColumnItem(nameof(AutoShowcaseDto.QuantityFree), "СО", true, false, "Суммарный свободный остаток по активным главным складам", width: 29));
                analyticsBand.Columns.Add(new GridColumnItem(nameof(AutoShowcaseDto.MinLeftover), "НО", true, false, "Несгораемый остаток", width: 29));
                analyticsBand.Columns.Add(new GridColumnItem(nameof(AutoShowcaseDto.SalesQuantity), "П", true, false, "Продажи за 14 дней по складам \"Витрина\" и \"Самовывоз\"", width: 29));
                analyticsBand.Columns.Add(new GridColumnItem(nameof(AutoShowcaseDto.TotalSalesQuantity), "ВП", true, false, "Все продажи за 14 дней", width: 29));
                analyticsBand.Columns.Add(new GridColumnItem(nameof(AutoShowcaseDto.CanBuy), "МК", true, false, "Можно купить", width: 29));

                yield return analyticsBand;
            }
        }

        private async Task RefreshAsync()
        {
            if (Showcases != null
                && (Showcases.Any(IsShowcaseChanged) || _rowsCountAfterRefresh != Showcases.Count)
                && !MessageFacadeService.Confirm("Вы действительно хотите обновить данные без сохранения?"))
            {
                return;
            }

            if (Filter.WarehouseIds?.Any() != true)
            {
                MessageFacadeService.ShowNotificationError("Не выбран ни один склад");
                return;
            }

            await Filter.RefreshAsync();

            _selectedWarehouseIds = Filter.WarehouseIds;

            PagedResult<AutoShowcaseDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryAutoShowcases(Filter.GetFilteringItem()));

            SummaryItems = Array.Empty<GridSummaryItem>().ToObservableCollection();

            int[] warehouseIds = pagedResult.Data.Select(x => x.WarehouseId).Distinct().ToArray();

            if (_currentWarehouseIdsInGrid.Count != warehouseIds.Length
                || _currentWarehouseIdsInGrid.Intersect(warehouseIds).Count() != _currentWarehouseIdsInGrid.Count)
            {
                Bands = GetBands(pagedResult.Data).ToObservableRangeCollection();
            }

            Showcases = GetShowcases(pagedResult.Data).ToObservableRangeCollection();

            _currentWarehouseIdsInGrid = warehouseIds;
            _rowsCountAfterRefresh = Showcases.Count;

            IDictionary<string, object> showcase = Showcases.FirstOrDefault();

            if (showcase != null)
            {
                FormattingRules = GetFormattingRules(showcase.Keys).ToObservableRangeCollection();
            }
        }

        private async Task SaveAsync()
        {
            if (Showcases == null)
            {
                return;
            }

            if (!Showcases.Where(IsShowcaseChanged).Any() && _rowsCountAfterRefresh == Showcases.Count)
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            AutoShowcaseSaveDto[] saveDtos = GetSaveDtos(Showcases.Where(x => IsShowcaseChanged(x) || GetAllIdsFromRow(x).All(z => z == 0))).ToArray();

            AutoShowcasesSaveDto autoShowcasesSaveDto = new AutoShowcasesSaveDto(_deletedIds, saveDtos, _selectedWarehouseIds);

            PagedResult<AutoShowcaseDto> pagedShowcases = await _errorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new UpdateAutoShowcases(autoShowcasesSaveDto)), "сохранении витрины", "Витрина сохранена", this, true);

            if (pagedShowcases?.Data.Any() == true)
            {
                Showcases = GetShowcases(pagedShowcases.Data).ToObservableRangeCollection();

                _rowsCountAfterRefresh = Showcases.Count;

                _deletedIds.Clear();
            }
        }

        private async Task CalculateAutoShowcaseAsync()
        {
            if (!RefreshDataWithoutSaving())
            {
                return;
            }

            await CalculateAutoShowcaseInternalAsync(null);
        }

        private async Task CalculateAutoShowcaseInternalAsync(int[] categoryIds)
        {
            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                new GetTextFromUserParameter(
                    "Не учитывать сегменты меньше %",
                    "Параметры",
                    "^[0-9]{1,2}([.][0-9]{1,2})?$",
                    "Значение не валидно",
                    "0"),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (!double.TryParse(viewModel.Content, out double segmentMinPercent))
            {
                MessageFacadeService.ShowNotificationWarning("Введены не валидные параметры");
            }

            Result<PagedResult<AutoShowcaseDto>> result = await _errorHandler
                .HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(
                        new CalculateAutoShowcase(categoryIds, _selectedWarehouseIds.ToArray(), segmentMinPercent)),
                    "рассчета авто витрины",
                    "Авто витрина рассчитана",
                    this,
                    true);

            if (result is null)
            {
                return;
            }

            CalculateSummaryCapacityByWarehouses(result.Data.Data);

            IReadOnlyCollection<int> zeroCapacityIdsAfterCalculation = result.Data.Data
                .Where(x => x.Capacity == 0)
                .Select(x => x.Id)
                .ToArray();

            _deletedIds.AddRange(zeroCapacityIdsAfterCalculation);

            Showcases = GetShowcases(result.Data.Data.Where(x => x.Capacity > 0).ToArray()).ToObservableRangeCollection();

            IDictionary<string, object> showcase = Showcases.FirstOrDefault();

            if (showcase != null)
            {
                FormattingRules = GetFormattingRules(showcase.Keys).ToObservableRangeCollection();
            }
        }

        private async Task CalculateAutoShowcaseByCategoriesAsync()
        {
            if (!RefreshDataWithoutSaving())
            {
                return;
            }

            IReadOnlyCollection<int> allowedCategoryIds = _categories
                .Where(x => x.IsParent)
                .Select(x => x.Id)
                .ToArray();

            ChooseCategoryParameter parameter = new ChooseCategoryParameter(true, allowedCategoryIds, "Категории должны быть родительскими");

            ChooseCategoryViewModel viewModel = DialogDocumentManagerService.ShowView<ChooseCategoryViewModel>(parameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            int[] selectedCategoryIds = viewModel.SelectedCategories.Select(x => x.Id).ToArray();

            await CalculateAutoShowcaseInternalAsync(selectedCategoryIds);
        }

        private void Delete()
        {
            ExpandoObject showcaseToDelete = Showcases.First(x => CurrentRow.Equals(x));

            _deletedIds.AddRange(GetAllIdsFromRow(showcaseToDelete));

            Showcases.Remove(showcaseToDelete);
        }

        private async Task AddAsync()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.ByQuantity,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (!nomenclatureViewModel.IsOk)
            {
                return;
            }

            List<(int Id, int WarehouseId, int CategoryId)> requestShowcases = new List<(int Id, int WarehouseId, int CategoryId)>();

            foreach (NomenclatureViewItem nomenclatureViewItem in nomenclatureViewModel.GetSelectedItems())
            {
                foreach (KeyValuePair<int, string> warehouseName in _warehouseNames)
                {
                    requestShowcases.Add((nomenclatureViewItem.Id, warehouseName.Key, nomenclatureViewItem.CategoryId));
                }
            }

            IReadOnlyCollection<ShowcaseDataDto> showcases = await WebClient.ExecuteApiRequestAsync(new QueryShowcaseData(requestShowcases));

            List<string> errors = new List<string>();

            List<AutoShowcaseDto> showcaseDtos = new List<AutoShowcaseDto>();

            foreach (NomenclatureViewItem product in nomenclatureViewModel.GetSelectedItems())
            {
                if (Showcases.Any(x => ((NullableIntPropertyValue)x.GetValueOrDefault(nameof(AutoShowcaseDto.ProductId)))?.Value == product.Id))
                {
                    errors.Add($"Товар '{product.Name}' ({product.Id}) уже был добавлен");
                }
                else
                {
                    foreach (KeyValuePair<int, string> warehouseName in _warehouseNames)
                    {
                        ShowcaseDataDto data = showcases.FirstOrDefault(x => x.ProductId == product.Id && x.WarehouseId == warehouseName.Key);

                        AutoShowcaseDto autoShowcaseDto = new AutoShowcaseDto()
                        {
                            ProductId = product.Id,
                            ProductName = product.NameFull,
                            ProductNameUkr = product.NameFullUa,
                            ModifiedBy = WebClient.AuthenticatedEmployee.Id,
                            CreatedBy = WebClient.AuthenticatedEmployee.Id,
                            CategoryEmployeeId = _categoryEmployees[product.ParentCategoryId],
                            CategoryName = product.ParentCategoryName,
                            CategoryNameUkr = product.ParentCategoryName,
                            MinLeftover = product.MinLeftover,
                            SegmentId = _segmentNames.First(x => x.Value == product.SegmentName).Key,
                            WarehouseId = warehouseName.Key
                        };

                        if (data is not null)
                        {
                            autoShowcaseDto.QuantityFree = data.QuantityFree;
                            autoShowcaseDto.WarehouseQuantityFree = data.WarehouseQuantityFree;
                            autoShowcaseDto.WarehouseCategoryPlan = data.WarehouseCategoryPlan;
                        }

                        showcaseDtos.Add(autoShowcaseDto);
                    }
                }
            }

            if (showcaseDtos.Any())
            {
                Showcases.AddOrInsertRange(GetShowcases(showcaseDtos), 0);
            }

            if (errors.Any())
            {
                MessageFacadeService.ShowValidationResultView("Ошибки при добавлении товаров", errors.Select(x => new ValidationResultItem(x, false)), this);
            }
        }

        private void ShowcaseCategory()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.ShowcaseCategoryAccess))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<ShowcaseCategoryViewModel>(null, this);
        }

        private void ShowcaseCluster()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.ShowcaseCategoryAccess))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<ShowcaseClusterViewModel>(null, this);
        }

        private IEnumerable<ExpandoObject> GetShowcases(IReadOnlyCollection<AutoShowcaseDto> showcaseDtos)
        {
            CalculateSummaryCapacityByWarehouses(showcaseDtos);

            showcaseDtos.ForEach(x => _showcasesCapacitiesBeforeEdit[(x.WarehouseId, x.ProductId)] = x.Capacity);

            foreach (IGrouping<int, AutoShowcaseDto> showcaseGroup in showcaseDtos.GroupBy(x => x.ProductId))
            {
                AutoShowcaseDto firstShowcase = showcaseGroup.First();

                List<AutoShowcaseDto> showcasesForAllSelectedWarehouses = showcaseGroup.ToList();

                foreach (int selectedWarehouseId in _selectedWarehouseIds)
                {
                    if (showcasesForAllSelectedWarehouses.All(x => x.WarehouseId != selectedWarehouseId))
                    {
                        showcasesForAllSelectedWarehouses.Add(new AutoShowcaseDto()
                        {
                            WarehouseId = selectedWarehouseId,
                            ProductId = firstShowcase.ProductId,
                            ProductName = firstShowcase.ProductName,
                            ProductNameUkr = firstShowcase.ProductNameUkr,
                            ProductNameEn = firstShowcase.ProductNameEn,
                            Capacity = 0,
                            CreatedBy = Constants.SystemEmployeeId,
                            ModifiedBy = Constants.SystemEmployeeId,
                        });
                    }
                }

                yield return CreateShowcase(firstShowcase, showcasesForAllSelectedWarehouses);
            }
        }

        private void CurrentProductChangedCallback()
        {
            ProductInformation.ClearProduct();

            if (CurrentRow?.Any() == true)
            {
                ProductInformation.ProductId = new ProductInfoId(((NullableIntPropertyValue)CurrentRow[nameof(AutoShowcaseDto.ProductId)]).Value!.Value, Currency.UahId);
            }
        }

        private ExpandoObject CreateShowcase(
            AutoShowcaseDto autoShowcaseDto,
            IReadOnlyCollection<AutoShowcaseDto> warehouseShowcases)
        {
            IDictionary<string, object> item = new ExpandoObject();

            item.Add(nameof(AutoShowcaseDto.ProductId), new NullableIntPropertyValue(autoShowcaseDto.ProductId));
            item.Add(nameof(AutoShowcaseDto.ProductNameUkr), new StringPropertyValue(autoShowcaseDto.ProductNameUkr));
            item.Add(nameof(AutoShowcaseDto.CategoryNameUkr), new StringPropertyValue(autoShowcaseDto.CategoryNameUkr));
            item.Add(nameof(AutoShowcaseDto.CategoryEmployeeId), new StringPropertyValue(_employeeNames[autoShowcaseDto.CategoryEmployeeId]));
            item.Add(nameof(AutoShowcaseDto.SegmentId), new StringPropertyValue(_segmentNames.GetValueOrDefault(autoShowcaseDto.SegmentId ?? 0)));
            item.Add(nameof(AutoShowcaseDto.QuantityFree), new NullableIntPropertyValue(autoShowcaseDto.QuantityFree));
            item.Add(nameof(AutoShowcaseDto.MinLeftover), new NullableIntPropertyValue(autoShowcaseDto.MinLeftover));
            item.Add(nameof(AutoShowcaseDto.SalesQuantity), new NullableIntPropertyValue(autoShowcaseDto.SalesQuantity));
            item.Add(nameof(AutoShowcaseDto.TotalSalesQuantity), new NullableIntPropertyValue(autoShowcaseDto.TotalSalesQuantity));
            item.Add(nameof(AutoShowcaseDto.CanBuy), new StringPropertyValue(autoShowcaseDto.CanBuy ? "да" : "нет"));

            foreach (AutoShowcaseDto warehouse in warehouseShowcases)
            {
                string idPropertyName = GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.Id));
                NullableIntPropertyValue idPropertyValue = new NullableIntPropertyValue(warehouse.Id);
                item.Add(idPropertyName, idPropertyValue);

                string warehouseIdPropertyName = GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.WarehouseId));
                NullableIntPropertyValue warehouseIdPropertyValue = new NullableIntPropertyValue(warehouse.WarehouseId);
                item.Add(warehouseIdPropertyName, warehouseIdPropertyValue);

                string warehouseQuantityFreePropertyName = GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.WarehouseQuantityFree));
                NullableIntPropertyValue quantityFreePropertyValue = new NullableIntPropertyValue(warehouse.WarehouseQuantityFree);
                item.Add(warehouseQuantityFreePropertyName, quantityFreePropertyValue);

                string warehouseSalesQuantityPropertyName = GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.SalesQuantityByWarehouse));
                NullableIntPropertyValue warehouseSalesQuantityPropertyValue = new NullableIntPropertyValue(warehouse.SalesQuantityByWarehouse);
                item.Add(warehouseSalesQuantityPropertyName, warehouseSalesQuantityPropertyValue);

                string warehouseCategoryPlanPropertyName = GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.WarehouseCategoryPlan));
                NullableIntPropertyValue categoryPlanPropertyValue = new NullableIntPropertyValue(warehouse.WarehouseCategoryPlan);
                item.Add(warehouseCategoryPlanPropertyName, categoryPlanPropertyValue);

                string warehouseSumCapacityPropertyName = GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.SumCapacity));
                NullableIntPropertyValue sumCapacityPropertyValue = new NullableIntPropertyValue(warehouse.SumCapacity);
                item.Add(warehouseSumCapacityPropertyName, sumCapacityPropertyValue);

                string warehouseCapacityPropertyName = GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.Capacity));
                NullableIntPropertyValue capacityPropertyValue = new NullableIntPropertyValue(warehouse.Capacity);
                item.Add(warehouseCapacityPropertyName, capacityPropertyValue);

                string warehouseModifiedByPropertyName = GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.ModifiedBy));
                StringPropertyValue modifiedByPropertyValue = new StringPropertyValue(warehouse.ModifiedBy == Constants.SystemEmployeeId ? null : _employeeNames[warehouse.ModifiedBy], warehouse.ModifiedBy);
                item.Add(warehouseModifiedByPropertyName, modifiedByPropertyValue);

                string warehouseCreatedByPropertyName = GetWarehousePropertyName(warehouse.WarehouseId, nameof(AutoShowcaseDto.CreatedBy));
                StringPropertyValue createdByPropertyValue = new StringPropertyValue(warehouse.CreatedBy == Constants.SystemEmployeeId ? null : _employeeNames[warehouse.CreatedBy], warehouse.CreatedBy);
                item.Add(warehouseCreatedByPropertyName, createdByPropertyValue);
            }

            return (ExpandoObject)item;
        }

        private bool RefreshDataWithoutSaving()
        {
            if (Showcases != null
                && (Showcases.Any(IsShowcaseChanged) || _rowsCountAfterRefresh != Showcases.Count)
                && !MessageFacadeService.Confirm("Вы действительно хотите обновить данные без сохранения?"))
            {
                return false;
            }

            _deletedIds.Clear();
            return true;
        }

        private void CustomSummary(RowSummaryArgs args)
        {
            if (_groupSummaryItemNames?.Contains(args.SummaryItem.PropertyName) == true)
            {
                if (args.SummaryProcess == SummaryProcess.Start)
                {
                    args.TotalValue = 0;
                }

                if (args.SummaryProcess == SummaryProcess.Calculate && args.FieldValue is NullableIntPropertyValue)
                {
                    int? value = ((NullableIntPropertyValue)args.FieldValue).Value;

                    if (value.HasValue)
                    {
                        args.TotalValue = (int)args.TotalValue + value.Value;
                    }
                }
            }
        }

        private void ChangeOutQuantity(CellValueChangedEventArgs e)
        {
            IDictionary<string, object> selectedRow = (ExpandoObject)e.Row;

            int[] warehouseIds = _warehouseNames.Keys.ToArray();

            foreach (int id in warehouseIds)
            {
                string fieldName = $"FieldName_{GetWarehousePropertyName(id, nameof(AutoShowcaseDto.Capacity))}";

                if (e.Column.FieldName == fieldName)
                {
                    if (selectedRow.TryGetValue(nameof(AutoShowcaseDto.CategoryNameUkr), out object categoryNameValue) && categoryNameValue is StringPropertyValue categoryName)
                    {
                        List<ExpandoObject> listExpandoObjects = new List<ExpandoObject>();

                        int sumWarehouseCatigory = 0;

                        foreach (ExpandoObject expandoObject in Showcases)
                        {
                            IDictionary<string, object> expandoObjectDictionary = expandoObject;

                            if (expandoObjectDictionary.TryGetValue(nameof(AutoShowcaseDto.CategoryNameUkr), out object value1) && value1 is StringPropertyValue stringValue1 && stringValue1.Value == categoryName.Value)
                            {
                                listExpandoObjects.Add(expandoObject);

                                if (expandoObjectDictionary.TryGetValue(GetWarehousePropertyName(id, nameof(AutoShowcaseDto.Capacity)), out object capacityValue) &&
                                    capacityValue is NullableIntPropertyValue nullableIntPropertyValue)
                                {
                                    sumWarehouseCatigory += nullableIntPropertyValue.Value ?? 0;
                                }
                            }
                        }

                        foreach (ExpandoObject expandoObjectForChangeCapacitySum in listExpandoObjects)
                        {
                            IDictionary<string, object> expandoObjectDictionary = expandoObjectForChangeCapacitySum;

                            if (expandoObjectDictionary.TryGetValue(GetWarehousePropertyName(id, nameof(AutoShowcaseDto.SumCapacity)), out object sumCapacityvalue) && sumCapacityvalue is NullableIntPropertyValue sumCapacity)
                            {
                                sumCapacity.Value = sumWarehouseCatigory;
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void ShowcaseRoutes()
        {
            NonModalDialogDocumentManagerService.ShowView<ShowcaseRoutesViewModel>(null, this);
        }
    }
}