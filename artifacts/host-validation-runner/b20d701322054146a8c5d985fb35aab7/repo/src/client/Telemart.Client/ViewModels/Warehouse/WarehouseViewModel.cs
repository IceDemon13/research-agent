using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DynamicData;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Locations;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.Requests.Features.Warehouse.Delivery;
using Telemart.Client.Data.Requests.Features.Warehouse.Performance;
using Telemart.Client.Data.Requests.Features.WarehouseCell;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.Warehouse.Cell;
using Telemart.Client.TransferObjects.Warehouse.Delivery;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;
using Telemart.Client.TransferObjects.Warehouse.Route;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Extensions;

namespace Telemart.Client.ViewModels.Warehouse
{
    public sealed class WarehouseViewModel : TelemartEditorViewModelBase<WarehouseDto, WarehouseEditParameter, WarehouseViewItem>
    {
        private WarehouseEditParameter parameter;
        private IReadOnlyDictionary<int, WarehouseViewItem> warehousesDictionary;
        private IReadOnlyCollection<EmployeeDto> _employees;
        private bool canEdit;

        public WarehouseViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            ErrorHandler = errorHandler;

            HandleTabSelectionChangedCommand = new DelegateCommand<ValueChangedEventArgs<FrameworkElement>>(HandleTabSelectionChanged);

            CreateRouteCommand = new DelegateCommand(CreateRoute, () => IsLockedByCurrentEmployee);
            EditRouteCommand = new DelegateCommand<WarehouseRouteViewItem>(EditRoute, x => IsLockedByCurrentEmployee && x != null);
            DeleteRouteCommand = new AsyncCommand<WarehouseRouteViewItem>(DeleteRouteAsync, x => IsLockedByCurrentEmployee && x != null);
            RefreshRoutesCommand = new AsyncCommand(RefreshRoutesAsync);

            RefreshDeliveriesCommand = new AsyncCommand(RefreshDeliveriesAsync);
            CreateDeliveryCommand = new DelegateCommand(CreateDelivery, () => IsLockedByCurrentEmployee);
            EditDeliveryCommand = new DelegateCommand<WarehouseDeliveryViewItem>(EditDelivery, x => IsLockedByCurrentEmployee && x != null);
            DeleteDeliveryCommand = new AsyncCommand<WarehouseDeliveryViewItem>(DeleteDeliveryAsync, x => IsLockedByCurrentEmployee && x != null);

            RefreshPerfomancesCommand = new AsyncCommand(RefreshPerfomancesAsync);
            CreatePerfomanceCommand = new DelegateCommand(CreatePerfomance, () => IsLockedByCurrentEmployee);
            EditPerfomanceCommand = new DelegateCommand<WarehousePerfomanceViewItem>(EditPerfomance, x => IsLockedByCurrentEmployee && x != null);
            DeletePerfomanceCommand = new AsyncCommand<WarehousePerfomanceViewItem>(DeletePerfomanceAsync, x => IsLockedByCurrentEmployee && x != null);

            RefreshCellsCommand = new AsyncCommand(RefreshCellsAsync);
            CreateCellCommand = new DelegateCommand(CreateCell, () => IsLockedByCurrentEmployee);
            UpdateCellCommand = new DelegateCommand<WarehouseCellViewItem>(UpdateCell, x => x != null && IsLockedByCurrentEmployee);
            DeleteCellCommand = new AsyncCommand<WarehouseCellViewItem>(DeleteCellAsync, x => IsLockedByCurrentEmployee && x != null);

            CopyWarehouseCommand = new AsyncCommand(CopyWarehouseAsync, () => IsLockedByCurrentEmployee);

            Messenger.Register<WarehouseRouteMessage>(this, OnWarehouseRouteMessage);
            Messenger.Register<WarehouseDeliveryMessage>(this, OnWarehouseDeliveryMessage);
            Messenger.Register<WarehousePerfomanceMessage>(this, OnWarehousePerfomanceMessage);
            Messenger.Register<EntityMessage<WarehouseCellDto>>(this, OnWarehouseCellMessage);

            CanCreateRoute = webClient.IsOperationAllowed(BusinessOperation.WarehouseRouteCreate);
            CanUpdateRoute = webClient.IsOperationAllowed(BusinessOperation.WarehouseRouteUpdate);
            CanDeleteRoute = webClient.IsOperationAllowed(BusinessOperation.WarehouseRouteDelete);

            CanCreateDelivery = webClient.IsOperationAllowed(BusinessOperation.WarehouseDeliveryCreate);
            CanUpdateDelivery = webClient.IsOperationAllowed(BusinessOperation.WarehouseDeliveryUpdate);
            CanDeleteDelivery = webClient.IsOperationAllowed(BusinessOperation.WarehouseDeliveryDelete);

            CanCreatePerfomance = webClient.IsOperationAllowed(BusinessOperation.WarehousePerfomanceCreate);
            CanUpdatePerfomance = webClient.IsOperationAllowed(BusinessOperation.WarehousePerfomanceUpdate);
            CanCopyWarehouse = webClient.IsOperationAllowed(BusinessOperation.WarehouseCopy);
        }

        public WarehouseViewModel()
        {
        }

        private IErrorHandler ErrorHandler { get; }

        #region Commands

        public IDelegateCommand CreateRouteCommand { get; }

        public IDelegateCommand EditRouteCommand { get; }

        public IAsyncCommand DeleteRouteCommand { get; }

        public IAsyncCommand RefreshRoutesCommand { get; }

        public IAsyncCommand RefreshDeliveriesCommand { get; }

        public IAsyncCommand RefreshPerfomancesCommand { get; }

        public IDelegateCommand CreateDeliveryCommand { get; }

        public IDelegateCommand CreatePerfomanceCommand { get; }

        public IDelegateCommand EditDeliveryCommand { get; }

        public IDelegateCommand EditPerfomanceCommand { get; }

        public IAsyncCommand DeletePerfomanceCommand { get;  }

        public IAsyncCommand DeleteDeliveryCommand { get; }

        public IDelegateCommand HandleTabSelectionChangedCommand { get; }

        public IDelegateCommand CreateCellCommand { get; }

        public IDelegateCommand UpdateCellCommand { get; }

        public IAsyncCommand DeleteCellCommand { get; }

        public IAsyncCommand RefreshCellsCommand { get; }

        public IAsyncCommand CopyWarehouseCommand { get; }

        #endregion

        #region INPC

        public int SelectedTabIndex
        {
            get { return GetProperty(() => SelectedTabIndex); }
            set { SetProperty(() => SelectedTabIndex, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AssemblyEmployees
        {
            get { return GetProperty(() => AssemblyEmployees); }
            private set { SetProperty(() => AssemblyEmployees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> BufferWarehouses
        {
            get { return GetProperty(() => BufferWarehouses); }
            private set { SetProperty(() => BufferWarehouses, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AssemblyWarehouses
        {
            get { return GetProperty(() => AssemblyWarehouses); }
            private set { SetProperty(() => AssemblyWarehouses, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Locations
        {
            get { return GetProperty(() => Locations); }
            private set { SetProperty(() => Locations, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> PerformancePatterns
        {
            get { return GetProperty(() => PerformancePatterns); }
            private set { SetProperty(() => PerformancePatterns, value); }
        }

        public ReadOnlyObservableCollection<WarehouseKind> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public ReadOnlyObservableCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public ObservableCollection<WarehouseRouteViewItem> WarehouseRoutes
        {
            get { return GetProperty(() => WarehouseRoutes); }
            private set { SetProperty(() => WarehouseRoutes, value); }
        }

        public ReadOnlyObservableCollection<WarehouseWorkTypeDto> WorkTypes
        {
            get { return GetProperty(() => WorkTypes); }
            private set { SetProperty(() => WorkTypes, value); }
        }

        public ObservableCollection<WarehouseDeliveryViewItem> WarehouseDeliveries
        {
            get { return GetProperty(() => WarehouseDeliveries); }
            private set { SetProperty(() => WarehouseDeliveries, value); }
        }

        public ObservableCollection<WarehousePerfomanceViewItem> WarehousePerfomances
        {
            get { return GetProperty(() => WarehousePerfomances); }
            private set { SetProperty(() => WarehousePerfomances, value); }
        }

        public ObservableCollection<WarehouseCellViewItem> WarehouseCells
        {
            get { return GetProperty(() => WarehouseCells); }
            private set { SetProperty(() => WarehouseCells, value); }
        }

        public bool AllowQuota
        {
            get { return GetProperty(() => AllowQuota); }
            private set { SetProperty(() => AllowQuota, value); }
        }

        public bool CanCreateRoute { get; }

        public bool CanUpdateRoute { get; }

        public bool CanDeleteRoute { get; }

        public bool CanCreateDelivery { get; }

        public bool CanUpdateDelivery { get; }

        public bool CanDeleteDelivery { get; }

        public bool CanCreatePerfomance { get; }

        public bool CanUpdatePerfomance { get; }

        public bool CanCopyWarehouse { get; }

        #endregion

        #region DialogSettings

        public override int Height => 690;

        public override int MinHeight => 690;

        public override int MinWidth => 700;

        public override int Width => 700;

        public override int MaxHeight => 1000;

        public override int MaxWidth => 1600;

        #endregion

        protected override string CreatedActionMessage => "создан";

        protected override string EntityName => "Склад";

        protected override string UpdatedActionMessage => "сохранен";

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(WarehouseViewItem.EmployeeId):

                    Model.Phone = Model.EmployeeId > 0 ? _employees.FirstOrDefault(x => x.Id == Model.EmployeeId)!.Phone1 : string.Empty;
                    break;
                case nameof(WarehouseViewItem.PerformancePatternId):
                    AllowQuota = Model?.PerformancePatternId == (int)WarehousePerformancePattern.Quota;

                    if (Model != null && Model.PerformancePatternId != (int)WarehousePerformancePattern.Quota)
                    {
                        Model.Quota = null;
                    }

                    RaisePropertiesChanged(nameof(AllowQuota));

                    break;
            }
        }

        protected override void OnInitializeInDesignModeInternal()
        {
            WarehousePerfomances = new ObservableCollection<WarehousePerfomanceViewItem>();

            Model.UseCells = true;
        }

        protected override Task<Result<WarehouseDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(WarehouseDto taskDto, MessageType messageType)
        {
            return new WarehouseMessage(taskDto, messageType);
        }

        protected override Task<WarehouseDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryWarehouse(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            canEdit = WebClient.IsOperationAllowed(BusinessOperation.WarehouseUpdate);

            parameter = (WarehouseEditParameter)Parameter;

            if (parameter.IsNew)
            {
                throw new NotSupportedException("Warehouse creation is not supported");
            }

            Types = Dictionaries.GetItems<WarehouseKind>().Where(x => !x.IsVirtual).ToReadOnlyObservableCollection();
            CarryTypes = Dictionaries.GetItems<CarryType>().ToReadOnlyObservableCollection();
            Subdivisions = Dictionaries.GetItems<Subdivision>().ToReadOnlyObservableCollection();
            List<WarehouseWorkTypeDto> resultWorkTypes = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseWorkTypes());
            WorkTypes = resultWorkTypes.ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            await Task.WhenAll(RefreshCitiesAsync(), RefreshEmployeesAsync(), RefreshWarehousesAsync(), RefreshShopsAsync(), RefreshPerformancePatternsAsync());

            SetEmployees();
            SetAssemblyEmployees();
            SetAdditionalServiceEmployees();
            SetBufferWarehouses();
            SetAssemblyWarehouses();

            if (parameter.OpenOnLogisticsTab)
            {
                SelectedTabIndex = 1;
                RefreshRoutesCommand.Execute(null);
            }
            else
            {
                SelectedTabIndex = 0;
            }
        }

        protected override Task<LockResponse<WarehouseDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockWarehouse(id));
        }

        protected override Task<LockResponse<WarehouseDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockWarehouse(id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание Склада";
        }

        protected override void SetEditTitle()
        {
            Title = $"Склад \"{Model.Name}\" ({Model.Id})";
        }

        protected override void AfterSetData()
        {
            SetEmployees();
            SetAssemblyEmployees();
            SetAdditionalServiceEmployees();
            SetBufferWarehouses();
            SetAssemblyWarehouses();

            AllowQuota = Model?.PerformancePatternId == (int)WarehousePerformancePattern.Quota;
            RaisePropertiesChanged(nameof(AllowQuota));
        }

        protected override async Task<Result<WarehouseDto>> UpdateEntityAsync()
        {
            if (Model.Quota.HasValue)
            {
                if (WarehousePerfomances == null)
                {
                    await RefreshPerfomancesAsync();
                }

                IReadOnlyCollection<WarehousePerfomanceViewItem> warehousePerfomances = WarehousePerfomances!
                    .Where(x => x.Estimate > Model.Quota && x.WorkId == WarehouseWorkTypeIds.AdditionalServiceWorkTypeId)
                    .OrderBy(x => x.Estimate)
                    .ToArray();

                if (warehousePerfomances.Any())
                {
                    List<ValidationResultItem> validationResultItems = new List<ValidationResultItem>();

                    validationResultItems.Add(new ValidationResultItem($"В производительности склада есть услуги, трудозатраты на которые больше, чем указанная квота для склада.", false));

                    foreach (WarehousePerfomanceViewItem warehousePerfomance in warehousePerfomances)
                    {
                        validationResultItems.Add(new ValidationResultItem($"\"{warehousePerfomance.AdditionalServiceName}\" имеет трудозатраты \'{warehousePerfomance.Estimate!.Value.ToString(@"dd\.hh\:mm")}\"", false));
                    }

                    ShowValidationResultView("Предупрежедения", validationResultItems);
                }
            }

            WarehouseSaveDto saveDto = Mapper.Map<WarehouseSaveDto>(Model);
            UpdateWarehouse gatewayRequest = new UpdateWarehouse(Model.Id, saveDto);
            return await WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override bool CanEdit()
        {
            return canEdit;
        }

        private void SetEmployees()
        {
            if (Employees != null)
            {
                ModelOriginal.Employee = Model.Employee = Employees.FirstOrDefault(x => x.Id == Model.EmployeeId);
            }
        }

        private void SetAssemblyEmployees()
        {
            if (AssemblyEmployees != null && Model.EmployeeAssemblyId.HasValue)
            {
                ModelOriginal.EmployeeAssembly = Model.EmployeeAssembly = Employees.FirstOrDefault(x => x.Id == Model.EmployeeAssemblyId);
            }
        }

        private void SetAdditionalServiceEmployees()
        {
            if (Employees != null && Model.EmployeeAdditionalServiceId.HasValue)
            {
                ModelOriginal.EmployeeAdditionalService = Model.EmployeeAdditionalService = Employees.FirstOrDefault(x => x.Id == Model.EmployeeAdditionalServiceId);
            }
        }

        private void SetBufferWarehouses()
        {
            if (BufferWarehouses != null && Model.BufferWarehouseId.HasValue)
            {
                ModelOriginal.BufferWarehouse = Model.BufferWarehouse = BufferWarehouses.FirstOrDefault(x => x.Id == Model.BufferWarehouseId);
            }
        }

        private void SetAssemblyWarehouses()
        {
            if (AssemblyWarehouses != null && Model.AssemblyWarehouseId.HasValue)
            {
                ModelOriginal.AssemblyWarehouse = Model.AssemblyWarehouse = AssemblyWarehouses.FirstOrDefault(x => x.Id == Model.AssemblyWarehouseId);
            }
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            Cities = cities
                .Where(x => x.Active || x.Id == Model.CityId)
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            _employees = employees;

            Employees = employees
                .Where(x => x.Active || Model.EmployeeId == x.Id || Model.EmployeeAdditionalServiceId == x.Id || Model.EmployeeAssemblyId == x.Id )
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                .ToReadOnlyObservableCollection();

            AssemblyEmployees = Employees;
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses()).GetPagedResultDataAsync();
            warehousesDictionary = warehouses.ToDictionary(x => x.Id, y => Mapper.Map<WarehouseViewItem>(y));

            BufferWarehouses = warehouses
                .Where(x => (x.TypeId == WarehouseKind.Main.Id || x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.Assembly.Id) && (x.Active == 1 || x.Id == Model.Id))
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            AssemblyWarehouses = warehouses
                .Where(x => x.WarehousePerformances.Where(z => z.Activity).Select(y => y.WorkId).Contains(WarehouseWorkTypeIds.AssemblyServiceWorkTypeId))
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshRoutesAsync()
        {
            List<WarehouseRouteSimpleDto> warehouseRoutes = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseRoutes(parameter.Id));

            WarehouseRoutes = warehouseRoutes
                .Select(MapWarehouseRoute)
                .OrderBy(x => x.WarehouseFromId == parameter.Id ? 0 : 1)
                .ThenByDescending(x => x.WarehouseFrom.Position)
                .ThenByDescending(x => x.WarehouseTo.Position)
                .ToObservableCollection();
        }

        private async Task RefreshShopsAsync()
        {
            List<LocationEntityDto> locations = await WebClient.ExecuteApiRequestAsync(new QueryLocations(), true);

            Locations = locations.Where(x => x.CityId == Model?.CityId).Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private async Task RefreshPerformancePatternsAsync()
        {
            List<WarehousePerformancePatternDto> warehousePerformancePatterns = await WebClient.ExecuteApiRequestAsync(new QueryWarehousePerformancePatterns());

            PerformancePatterns = warehousePerformancePatterns
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private WarehouseRouteViewItem MapWarehouseRoute(WarehouseRouteSimpleDto source)
        {
            WarehouseRouteViewItem destination = Mapper.Map<WarehouseRouteViewItem>(source);

            destination.WarehouseFrom = warehousesDictionary.GetValueOrDefault(destination.WarehouseFromId);
            destination.WarehouseTo = warehousesDictionary.GetValueOrDefault(destination.WarehouseToId);

            return destination;
        }

        private async Task RefreshDeliveriesAsync()
        {
            List<DeliveryDto> deliveries = await WebClient.ExecuteApiRequestAsync(new QueryDeliveries(parameter.Id));

            WarehouseDeliveries = deliveries
                .OrderBy(x => x.CarryIds.Select(c => Dictionaries.GetItemById<CarryType>(c)).Select(c => c.Position).DefaultIfEmpty(999).Min())
                .ThenBy(x => x.DaysOfWeek)
                .ThenBy(x => x.TimeGet)
                .Select(MapWarehouseDelivery)
                .ToObservableCollection();
        }

        private async Task RefreshPerfomancesAsync()
        {
            List<WarehousePerformanceDto> perfomances = await WebClient.ExecuteApiRequestAsync(new QueryPerformances(parameter.Id));

            WarehousePerfomances = perfomances
                .OrderBy(x => x.Id)
                .ThenBy(x => x.DayOfWeek)
                .ThenBy(x => x.Performance)
                .ThenBy(x => x.Activity)
                .Select(MapWarehousePerfomance)
                .ToObservableCollection();
        }

        private WarehouseDeliveryViewItem MapWarehouseDelivery(DeliveryDto source)
        {
            return MapWarehouseDelivery(source, new WarehouseDeliveryViewItem());
        }

        private WarehousePerfomanceViewItem MapWarehousePerfomance(WarehousePerformanceDto source)
        {
            return MapWarehousePerfomance(source, new WarehousePerfomanceViewItem());
        }

        private WarehouseDeliveryViewItem MapWarehouseDelivery(DeliveryDto source, WarehouseDeliveryViewItem target)
        {
            WarehouseDeliveryViewItem result = Mapper.Map(source, target);

            IEnumerable<string> carries = source.CarryIds
                .Select(c => Dictionaries.GetItemById<CarryType>(c))
                .OrderBy(x => x.Position)
                .Select(x => x.Name);

            result.CarriesString = string.Join(", ", carries);

            return result;
        }

        private WarehousePerfomanceViewItem MapWarehousePerfomance(WarehousePerformanceDto source, WarehousePerfomanceViewItem target)
        {
            WarehousePerfomanceViewItem result = Mapper.Map(source, target);
            return result;
        }

        private async Task DeleteRouteAsync(WarehouseRouteViewItem route)
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите удалить маршрут со всеми его графиками?"))
            {
                return;
            }

            try
            {
                Result<object> result = await WebClient.ExecuteApiRequestAsync(new DeleteWarehouseRoute(parameter.Id, route.Id));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Маршрут удален с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Маршрут успешно удален");
                }

                WarehouseRoutes.Remove(route);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении маршрута");
                ShowValidationResultView("Ошибки при удалении маршрута", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to delete warehouse route");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while deleting warehouse route");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении маршрута");
            }
        }

        private void EditRoute(WarehouseRouteViewItem route)
        {
            if (!route.WarehouseFrom.IsActive || !route.WarehouseTo.IsActive)
            {
                MessageFacadeService.ShowNotificationWarning("Нельзя редактировать графики для неактивных маршрутов");
                return;
            }

            DialogDocumentManagerService.ShowView<WarehouseRouteEditViewModel>(new WarehouseRouteEditParameter(parameter.Id, route.Id), this);
        }

        private void CreateRoute()
        {
            DialogDocumentManagerService.ShowView<WarehouseRouteCreateViewModel>(new WarehouseRouteCreateParameter(parameter.Id), this);
        }

        private void CreateDelivery()
        {
            DialogDocumentManagerService.ShowView<WarehouseDeliveryEditViewModel>(new WarehouseDeliveryEditParameter(parameter.Id, 0), this);
        }

        private void CreatePerfomance()
        {
            DialogDocumentManagerService.ShowView<WarehousePerfomanceEditViewModel>(new WarehousePerfomanceEditParameter(parameter.Id, 0), this);
        }

        private void EditDelivery(WarehouseDeliveryViewItem item)
        {
            DialogDocumentManagerService.ShowView<WarehouseDeliveryEditViewModel>(new WarehouseDeliveryEditParameter(parameter.Id, item.Id), this);
        }

        private void EditPerfomance(WarehousePerfomanceViewItem item)
        {
            DialogDocumentManagerService.ShowView<WarehousePerfomanceEditViewModel>(new WarehousePerfomanceEditParameter(parameter.Id, item.Id), this);
        }

        private async Task DeletePerfomanceAsync(WarehousePerfomanceViewItem item)
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите удалить правило производительности?"))
            {
                return;
            }

            var deleteResult = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new DeletePerformance(item.WarehouseId, item.Id)),
                "удалении правила",
                "Правило удалено",
                this,
                false,
                showNotification: true);

            await RefreshPerfomancesAsync();
        }

        private async Task DeleteDeliveryAsync(WarehouseDeliveryViewItem item)
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите удалить график доставок?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteDelivery(parameter.Id, item.Id));

                MessageFacadeService.ShowNotificationInfo("График доставок успешно удален");

                WarehouseDeliveries.Remove(item);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении графика доставок");
                ShowValidationResultView("Ошибки при удалении графика доставок", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to delete warehouse delivery");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while deleting warehouse delivery");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении графика доставок");
            }
        }

        private async Task RefreshCellsAsync()
        {
            List<WarehouseCellDto> cells = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseCells(parameter.Id));

            WarehouseCells = cells
                .Select(x => Mapper.Map<WarehouseCellViewItem>(x))
                .ToObservableCollection();
        }

        private void CreateCell()
        {
            DialogDocumentManagerService.ShowView<WarehouseCellViewModel>(new WarehouseCellParameter(0, parameter.Id), this);
        }

        private void UpdateCell(WarehouseCellViewItem cell)
        {
            DialogDocumentManagerService.ShowView<WarehouseCellViewModel>(new WarehouseCellParameter(cell.Id, parameter.Id), this);
        }

        private async Task DeleteCellAsync(WarehouseCellViewItem cell)
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите ячейку?"))
            {
                return;
            }

            try
            {
                Result<object> result = await WebClient.ExecuteApiRequestAsync(new DeleteWarehouseCell(parameter.Id, cell.Id));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Ячейка удалена с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Ячейка успешно удалена");
                }

                WarehouseCells.Remove(cell);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении ячейки");
                ShowValidationResultView("Ошибки при удалении ячейки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to delete warehouse cell");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while deleting warehouse cell");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении ячейки");
            }
        }

        private async Task CopyWarehouseAsync()
        {
            WarehouseCopyViewModel warehouseCopyViewModel = DialogDocumentManagerService.ShowView<WarehouseCopyViewModel>(new WarehouseCopyParameter(Model.Id), this);

            int? selectedWarehouseId = warehouseCopyViewModel?.SelectedWarehouseId;

            if (warehouseCopyViewModel?.IsOk == true && selectedWarehouseId.HasValue)
            {
                List<ValidationResultItem> results = new List<ValidationResultItem>();

                if (warehouseCopyViewModel.IsCopyDeliveries && warehouseCopyViewModel.SelectedDeliveriesCopyChoice.HasValue)
                {
                    if (WarehouseDeliveries == null)
                    {
                        await RefreshDeliveriesAsync();
                    }

                    IReadOnlyCollection<ValidationResultItem> warningCopyDeliveries = await CopyDeliveriesAsync(selectedWarehouseId.Value, warehouseCopyViewModel.SelectedDeliveriesCopyChoice.Value);

                    results.AddRange(warningCopyDeliveries);
                }

                if (warehouseCopyViewModel.IsCopyMovements && warehouseCopyViewModel.SelectedMovementsCopyChoice.HasValue)
                {
                    if (WarehouseRoutes == null)
                    {
                        await RefreshRoutesAsync();
                    }

                    IReadOnlyCollection<ValidationResultItem> warningCopyMovements = await CopyMovementsAsync(selectedWarehouseId.Value, warehouseCopyViewModel.SelectedMovementsCopyChoice.Value);

                    results.AddRange(warningCopyMovements);
                }

                if (warehouseCopyViewModel.IsCopyPerfomances && warehouseCopyViewModel.SelectedPerfomancesCopyChoice.HasValue)
                {
                    if (WarehousePerfomances == null)
                    {
                        await RefreshPerfomancesAsync();
                    }

                    IReadOnlyCollection<ValidationResultItem> warningCopyPerfomances = await CopyPerfomancesAsync(selectedWarehouseId.Value, warehouseCopyViewModel.SelectedPerfomancesCopyChoice.Value);

                    results.AddRange(warningCopyPerfomances);
                }

                if (results.Any())
                {
                    string message = "Копирование склада выполнено с ошибками";

                    MessageFacadeService.ShowNotificationWarning(message);
                    ShowValidationResultView(message, results);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Настройки перенесены успешно");
                }
            }
        }

        private void OnWarehouseRouteMessage(WarehouseRouteMessage message)
        {
            WarehouseRouteSimpleDto entity = message.Entity;

            if (entity.WarehouseFromId == parameter.Id || entity.WarehouseToId == parameter.Id)
            {
                switch (message.MessageType)
                {
                    case MessageType.Added:
                        WarehouseRoutes ??= new ObservableCollection<WarehouseRouteViewItem>();
                        WarehouseRoutes.Insert(0, MapWarehouseRoute(entity));
                        break;
                }
            }
        }

        private void OnWarehouseDeliveryMessage(WarehouseDeliveryMessage message)
        {
            if (message.Entity.WarehouseId != parameter.Id)
            {
                return;
            }

            switch (message.MessageType)
            {
                case MessageType.Added:
                    WarehouseDeliveries.Insert(0, MapWarehouseDelivery(message.Entity));
                    break;
                case MessageType.Changed:
                    WarehouseDeliveries.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => MapWarehouseDelivery(message.Entity, viewItem));
                    break;
            }
        }

        private void OnWarehousePerfomanceMessage(WarehousePerfomanceMessage message)
        {
            if (message.Entity.WarehouseId != parameter.Id || WarehousePerfomances == null)
            {
                return;
            }

            switch (message.MessageType)
            {
                case MessageType.Added:
                    WarehousePerfomances.Insert(0, MapWarehousePerfomance(message.Entity));
                    break;
                case MessageType.Changed:
                    WarehousePerfomances.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => MapWarehousePerfomance(message.Entity, viewItem));
                    break;
            }
        }

        private void OnWarehouseCellMessage(EntityMessage<WarehouseCellDto> message)
        {
            if (message.Entity.WarehouseId != parameter.Id || WarehouseCells == null)
            {
                return;
            }

            switch (message.MessageType)
            {
                case MessageType.Added:
                    WarehouseCells.Insert(0, Mapper.Map<WarehouseCellViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    WarehouseCells.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }

        private void HandleTabSelectionChanged(ValueChangedEventArgs<FrameworkElement> e)
        {
            switch (e.NewValue.Name)
            {
                case "Tab1":

                    if (WarehouseRoutes == null)
                    {
                        RefreshRoutesCommand.Execute(null);
                    }

                    break;

                case "Tab2":

                    if (WarehouseDeliveries == null)
                    {
                        RefreshDeliveriesCommand.Execute(null);
                    }

                    break;

                case "Tab3":

                    if (WarehousePerfomances == null)
                    {
                        RefreshPerfomancesCommand.Execute(null);
                    }

                    break;

                case "Tab4":

                    if (WarehouseCells == null)
                    {
                        RefreshCellsCommand.Execute(null);
                    }

                    break;
            }
        }

        private async Task<IReadOnlyCollection<ValidationResultItem>> CopyMovementsAsync(int selectedWarehouseId, int copyActionTypeId)
        {
            List<ValidationResultItem> warnings = new List<ValidationResultItem>();

            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses()).GetPagedResultDataAsync();

            int[] activeWarehouseIds = warehouses?.Where(x => x.Active > 0).Select(x => x.Id).ToArray();

            List<WarehouseRouteFullDto> warehouseRoutesSelectedWarehouse = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseFullRoutes(selectedWarehouseId));

            warnings.Add(ValidateCopyMovements(warehouseRoutesSelectedWarehouse, activeWarehouseIds, selectedWarehouseId));

            if (warnings.Any(x => x.IsError))
            {
                return warnings.Where(x => x.IsError).ToArray();
            }

            if (copyActionTypeId == CopyActionType.Override.Id)
            {
                warehouseRoutesSelectedWarehouse = warehouseRoutesSelectedWarehouse.Where(
                    x =>
                        (x.WarehouseFromId == selectedWarehouseId && activeWarehouseIds?.Contains(x.WarehouseToId) == true)
                        || (x.WarehouseToId == selectedWarehouseId && activeWarehouseIds?.Contains(x.WarehouseFromId) == true))
                    .ToList();

                IReadOnlyCollection<int> deletedWarehouseRoutes = WarehouseRoutes?.Select(x => x.Id).ToArray();

                Result resultDeleted = await WebClient.ExecuteApiRequestAsync(new DeleteRoutes(new DeleteRoutesDto { RouteIds = deletedWarehouseRoutes }));

                if (resultDeleted?.IsSuccess != true)
                {
                    warnings.Add(new ValidationResultItem("При удалении старой перемещений произошла ошибка", true));

                    return warnings;
                }
            }
            else
            {
                (int From, int To)[] warehouseRouteAlradyExistsIds = WarehouseRoutes?.Select(x => (x.WarehouseFromId, x.WarehouseToId)).ToArray();

                warehouseRoutesSelectedWarehouse = warehouseRoutesSelectedWarehouse.Where(
                        x =>
                            (x.WarehouseFromId == selectedWarehouseId && activeWarehouseIds?.Contains(x.WarehouseToId) == true && warehouseRouteAlradyExistsIds?.Any(y => y.To == x.WarehouseToId) != true)
                             || (x.WarehouseToId == selectedWarehouseId && activeWarehouseIds?.Contains(x.WarehouseFromId) == true && warehouseRouteAlradyExistsIds?.Any(y => y.From == x.WarehouseFromId) != true))
                    .ToList();
            }

            if (!warehouseRoutesSelectedWarehouse.Any())
            {
                warnings.Add(new ValidationResultItem("Попытка скопировать маршруты, которые существуют.", true));

                return warnings.Where(x => x.IsError).ToArray();
            }

            ManyWarehouseRouteCreateDto dto = new ManyWarehouseRouteCreateDto()
            {
                WarehouseRoutes = MapToWarehouseRouteCreateDtos(warehouseRoutesSelectedWarehouse, selectedWarehouseId)
            };

            Result<List<WarehouseRouteSimpleDto>> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateManyWarehouseRoute(dto)),
                "создании перемещений",
                null,
                this,
                false,
                showNotification: false);

            if (result?.IsSuccess == true)
            {
                await RefreshRoutesAsync();

                warnings.Add(result.Warnings.Select(x => new ValidationResultItem(x, false)));
            }
            else
            {
                string[] errorMessage = result?.ErrorObj != null ? result.ErrorObj.GetMessages().ToArray() : new[] { "Возникла ошибка при создании перемещений" };

                warnings = errorMessage.Select(x => new ValidationResultItem(x, true)).ToList();
            }

            return warnings.ToArray();
        }

        private async Task<IReadOnlyCollection<ValidationResultItem>> CopyDeliveriesAsync(int selectedWarehouseId, int copyActionTypeId)
        {
            IReadOnlyCollection<ValidationResultItem> warnings = Array.Empty<ValidationResultItem>();

            List<DeliveryDto> deliveries = await WebClient.ExecuteApiRequestAsync(new QueryDeliveries(selectedWarehouseId));

            if (deliveries == null || deliveries.Count == 0)
            {
                warnings = new[] { new ValidationResultItem("Доставка на выбранном складе не заполнена", true) };

                return warnings;
            }

            if (copyActionTypeId == CopyActionType.Override.Id)
            {
                IReadOnlyCollection<int> deletedDeliveryIds = WarehouseDeliveries?.Select(x => x.Id).ToArray();

                Result resultDeleted = await WebClient.ExecuteApiRequestAsync(new DeleteDeliveries(new DeleteManyDeliveriesDto { DeliveryIds = deletedDeliveryIds }));

                if (resultDeleted?.IsSuccess != true)
                {
                    warnings = new[] { new ValidationResultItem("При удалении старых доставок произошла ошибка", true) };

                    return warnings;
                }
            }
            else
            {
                deliveries = deliveries.Where(
                        x => WarehouseDeliveries?.Any(
                            y => y.DaysOfWeek == x.DaysOfWeek &&
                                 y.TimeGet == x.TimeGet &&
                                 y.TimeDeliveryFrom == x.TimeDeliveryFrom &&
                                 y.TimeDeliveryTo == x.TimeDeliveryTo) != true)
                    .ToList();
            }

            if (deliveries.Any())
            {
                DeliverySaveDto[] deliverySaveDtos = deliveries.Select(
                    x => new DeliverySaveDto()
                    {
                        CarryIds = x.CarryIds.ToArray(),
                        WarehouseId = Model.Id,
                        SubdivisionId = x.SubdivisionId,
                        DaysOfWeek = x.DaysOfWeek,
                        Days = x.Days,
                        TimeGet = x.TimeGet,
                        TimeDeliveryFrom = x.TimeDeliveryFrom,
                        TimeDeliveryTo = x.TimeDeliveryTo
                    }).ToArray();

                ManyDeliveriesSaveDto dto = new ManyDeliveriesSaveDto()
                {
                    Deliveries = deliverySaveDtos,
                    WarehouseId = Model.Id
                };

                Result<List<DeliveryDto>> result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new CreateManyDeliveries(dto)),
                    "создании правил доставки",
                    null,
                    this,
                    false,
                    showNotification: false);

                if (result?.IsSuccess == true)
                {
                    warnings = result.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray();

                    await RefreshDeliveriesAsync();
                }
                else
                {
                    string[] errorMessage = result?.ErrorObj != null ? result.ErrorObj.GetMessages().ToArray() : new[] { "Возникла ошибка при создании перемещений" };

                    warnings = errorMessage.Select(x => new ValidationResultItem(x, true)).ToArray();
                }
            }

            return warnings;
        }

        private async Task<IReadOnlyCollection<ValidationResultItem>> CopyPerfomancesAsync(int selectedWarehouseId, int copyActionTypeId)
        {
            IReadOnlyCollection<ValidationResultItem> warnings = Array.Empty<ValidationResultItem>();

            List<WarehousePerformanceDto> perfomances = await WebClient.ExecuteApiRequestAsync(new QueryPerformances(selectedWarehouseId));

            if (perfomances == null || perfomances.Count == 0)
            {
                warnings = new[] { new ValidationResultItem("Производительность на выбранном складе не заполнена", true) };

                return warnings;
            }

            if (copyActionTypeId == CopyActionType.Override.Id)
            {
                IReadOnlyCollection<int> deletedPerfomancesIds = WarehousePerfomances?.Select(x => x.Id).ToList();

                Result resultDeleted = await WebClient.ExecuteApiRequestAsync(new DeletePerformances(new DeletePerfomancesDto { PerformancesIds = deletedPerfomancesIds }));

                if (resultDeleted?.IsSuccess != true)
                {
                    warnings = new[] { new ValidationResultItem("При удалении старой производительности произошла ошибка", true) };

                    return warnings;
                }
            }
            else
            {
                perfomances = perfomances.Where(
                        x => WarehousePerfomances?
                            .Any(
                                y =>
                                    (y.WorkId == x.WorkId &&
                                     y.Performance.ToString() == x.Performance &&
                                     y.Estimate == x.Estimate &&
                                     y.AdditionalServiceId == x.AdditionalServiceId &&
                                     y.DayOfWeek == x.DayOfWeek &&
                                     y.Activity == x.Activity)
                                || (((x.WorkId == WarehouseWorkTypeIds.AdditionalServiceWorkTypeId && y.AdditionalServiceId == x.AdditionalServiceId) || (x.WorkId == y.WorkId && x.WorkId == WarehouseWorkTypeIds.AssemblyServiceWorkTypeId)) &&
                                    (x.DayOfWeek + y.DayOfWeek).Distinct().Count() != (x.DayOfWeek + y.DayOfWeek).Length)) != true)
                    .ToList();
            }

            if (perfomances.Any())
            {
                WarehousePerfomanceSaveDto[] perfomanceSaveDto = perfomances.Select(
                    x => new WarehousePerfomanceSaveDto
                    {
                        WorkId = x.WorkId,
                        WarehouseId = Model.Id,
                        Performance = int.TryParse(x.Performance, out int val) ? val : 0,
                        Estimate = x.Estimate,
                        AdditionalServiceId = x.AdditionalServiceId,
                        DayOfWeek = x.DayOfWeek,
                        Activity = x.Activity
                    }).ToArray();

                WarehouseManyPerfomancesSaveDto dto = new WarehouseManyPerfomancesSaveDto()
                {
                    Perfomances = perfomanceSaveDto,
                    WarehouseId = Model.Id
                };

                Result<List<WarehousePerformanceDto>> result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new CreateManyPerformances(Model.Id, dto)),
                    "создании производительности",
                    null,
                    this,
                    false,
                    showNotification: false);

                if (result?.IsSuccess == true)
                {
                    warnings = result.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray();

                    await RefreshPerfomancesAsync();
                }
                else
                {
                    string[] errorMessage = result?.ErrorObj != null ? result.ErrorObj.GetMessages().ToArray() : new[] { "Возникла ошибка при создании производительности" };

                    warnings = errorMessage.Select(x => new ValidationResultItem(x, true)).ToArray();
                }
            }

            return warnings;
        }

        private WarehouseRouteCreateDto[] MapToWarehouseRouteCreateDtos(IReadOnlyCollection<WarehouseRouteFullDto> warehouseRoutesSelectedWarehouse, int selectedWarehouseId)
        {
            List<WarehouseRouteCreateDto> routeWarehousesForCreate = new List<WarehouseRouteCreateDto>();

            routeWarehousesForCreate.AddRange(
                warehouseRoutesSelectedWarehouse
                    .Where(x => x.WarehouseFromId == selectedWarehouseId && x.WarehouseToId != Model.Id)
                    .Select(
                        x => new WarehouseRouteCreateDto
                        {
                            WarehouseFromId = Model.Id,
                            WarehouseToId = x.WarehouseToId,
                            Weight = x.Weight,
                            RouteTimes = x.RouteTimes?.Select(MapWarehouseRouteTime).ToArray()
                        }).ToList());

            routeWarehousesForCreate.AddRange(
                warehouseRoutesSelectedWarehouse
                    .Where(x => x.WarehouseToId == selectedWarehouseId && x.WarehouseFromId != Model.Id)
                    .Select(
                        x => new WarehouseRouteCreateDto
                        {
                            WarehouseFromId = x.WarehouseFromId,
                            WarehouseToId = Model.Id,
                            Weight = x.Weight,
                            RouteTimes = x.RouteTimes?.Select(MapWarehouseRouteTime).ToArray()
                        }).ToList());

            return routeWarehousesForCreate.ToArray();
        }

        private IEnumerable<ValidationResultItem> ValidateCopyMovements(IReadOnlyCollection<WarehouseRouteSimpleDto> warehouseRoutesSelectedWarehouse, int[] activeWarehouseIds, int selectedWarehouseId)
        {
            if (warehouseRoutesSelectedWarehouse == null || warehouseRoutesSelectedWarehouse.Count == 0)
            {
                yield return new ValidationResultItem("У выбранного склада отсутствуют маршруты", true);
                yield break;
            }

            if (warehouseRoutesSelectedWarehouse.All(x => (x.WarehouseFromId == selectedWarehouseId && activeWarehouseIds?.Contains(x.WarehouseToId) != true) || (x.WarehouseToId == selectedWarehouseId && activeWarehouseIds?.Contains(x.WarehouseFromId) != true)))
            {
                yield return new ValidationResultItem("У выбранного склада в маршрутах все маршруты имеют неактивные склады. При копировании перемещения не были добалены", true);
                yield break;
            }

            if (warehouseRoutesSelectedWarehouse.Any(x => (x.WarehouseFromId == selectedWarehouseId && activeWarehouseIds?.Contains(x.WarehouseToId) != true) || (x.WarehouseToId == selectedWarehouseId && activeWarehouseIds?.Contains(x.WarehouseFromId) != true)))
            {
                yield return new ValidationResultItem("У выбранного склада есть маршруты, у которых неактивные склады. При копировании были перенесены маршруты только с активными складами", false);
            }
        }

        private WarehouseRouteTimeCreateDto MapWarehouseRouteTime(WarehouseRouteTimeDto source)
        {
            return new WarehouseRouteTimeCreateDto()
            {
                TimeOut = source.TimeOut,
                TimeIn = source.TimeIn,
                TimeDeparture = source.TimeDeparture,
                TimeArrive = source.TimeArrive,
                Days = source.Days,
                DaysOfWeekOut = source.DaysOfWeekOut,
                DaysOfWeekIn = source.DaysOfWeekIn,
                CarryId = source.CarryId,
                DeliveryTypeId = source.DeliveryTypeId,
                Auto = source.Auto,
                DeliveryType = source.DeliveryType,
                PurposeIds = source.PurposeIds
            };
        }
    }
}