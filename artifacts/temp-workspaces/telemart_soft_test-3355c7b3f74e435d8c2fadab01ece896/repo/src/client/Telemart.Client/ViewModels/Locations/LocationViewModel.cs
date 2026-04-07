using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Locations;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.Requests.Features.WorkSchedule;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.WorkSchedule;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Warehouse;
using Telemart.Client.ViewModels.WorkSchedule;
using Telemart.Common.ErrorHandling;
using DayOfWeek = Telemart.Client.Dictionaries.DayOfWeek;

namespace Telemart.Client.ViewModels.Locations
{
    public sealed class LocationViewModel : TelemartEditorViewModelBase<LocationEntityDto, LocationParameter, LocationViewItem>
    {
        private IReadOnlyCollection<WarehouseDto> _warehouses;
        private IReadOnlyCollection<WorkScheduleViewItem> _workScheduleViewItems;
        private IReadOnlyCollection<WorkScheduleType> _allWorkScheduleTypes;

        public LocationViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            WarehouseEditCommand = new DelegateCommand(WarehouseEdit, () => SelectedWarehouse != null);
            Messenger.Register<WarehouseMessage>(this, OnWarehouseMessage);
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyCollection<WorkScheduleViewItem> WorkSchedules
        {
            get { return GetProperty(() => WorkSchedules); }
            private set { SetProperty(() => WorkSchedules, value); }
        }

        public ReadOnlyObservableCollection<WorkScheduleType> WorkScheduleTypes
        {
            get { return GetProperty(() => WorkScheduleTypes); }
            private set { SetProperty(() => WorkScheduleTypes, value); }
        }

        public ReadOnlyObservableCollection<WorkScheduleType> AdditionalWorkScheduleTypes
        {
            get { return GetProperty(() => AdditionalWorkScheduleTypes); }
            private set { SetProperty(() => AdditionalWorkScheduleTypes, value); }
        }

        public ReadOnlyObservableCollection<ExternalLocationDto> ExternalLocations
        {
            get { return GetProperty(() => ExternalLocations); }
            private set { SetProperty(() => ExternalLocations, value); }
        }

        public ReadOnlyObservableCollection<LocationType> LocationTypes
        {
            get { return GetProperty(() => LocationTypes); }
            private set { SetProperty(() => LocationTypes, value); }
        }

        public ReadOnlyObservableCollection<ClusterViewItem> Clusters
        {
            get { return GetProperty(() => Clusters); }
            private set { SetProperty(() => Clusters, value); }
        }

        public ObservableCollection<WarehouseViewItem> ShopWarehouses
        {
            get { return GetProperty(() => ShopWarehouses); }
            set { SetProperty(() => ShopWarehouses, value); }
        }

        public ReadOnlyObservableCollection<DayOfWeek> DaysOfWeek
        {
            get { return GetProperty(() => DaysOfWeek); }
            private set { SetProperty(() => DaysOfWeek, value); }
        }

        public WarehouseViewItem SelectedWarehouse
        {
            get { return GetProperty(() => SelectedWarehouse); }
            set { SetProperty(() => SelectedWarehouse, value); }
        }

        public bool TypeLocationShop => Model?.LocationTypeId == LocationType.ShopId;

        public bool AllowCluster => WebClient.IsOperationAllowed(BusinessOperation.ClusterCreate) ||
                                    WebClient.IsOperationAllowed(BusinessOperation.ClusterUpdate);

        public IDelegateCommand WarehouseEditCommand { get; }

        protected override string CreatedActionMessage => string.Empty;

        protected override string EntityName => "Локация";

        protected override string UpdatedActionMessage => "сохранена";

        protected override Task<Result<LocationEntityDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override Task<LocationEntityDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryLocation(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            _allWorkScheduleTypes = Dictionaries.GetItems<WorkScheduleType>().ToArray();

            WorkScheduleTypes = _allWorkScheduleTypes.Where(x => x.ParentId == (int)WorkScheduleTypeIds.Shop).ToReadOnlyObservableCollection();
            AdditionalWorkScheduleTypes = _allWorkScheduleTypes.Where(x => x.ParentId == (int)WorkScheduleTypeIds.AdditionalService).ToReadOnlyObservableCollection();

            DaysOfWeek = Dictionaries.GetItems<DayOfWeek>().ToReadOnlyObservableCollection();
            LocationTypes = Dictionaries.GetItems<LocationType>().ToReadOnlyObservableCollection();

            await Task.WhenAll(
                RefreshWorkSchedulesAsync(),
                RefreshCitiesAsync(),
                RefreshWarehousesAsync(),
                RefreshExternalLocationsAsync(),
                RefreshClustersAsync());

            await base.HandleLoadedAsync();
        }

        protected override Task<LockResponse<LocationEntityDto>> LockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override Task<LockResponse<LocationEntityDto>> UnlockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override void SetEditTitle()
        {
            Title = $"Локация \"{Model?.Name}\" ({Model?.Id})";
        }

        protected override object CreateEntityMessage(LocationEntityDto dto, MessageType messageType)
        {
            return new LocationMessage(dto, messageType);
        }

        protected override Task<Result<LocationEntityDto>> UpdateEntityAsync()
        {
            LocationEntityDto location = new LocationEntityDto(Model.Name, Model.CityId!.Value, Model.Address, Model.WorkScheduleTypeId, Model.AdditionalScheduleTypeId,  Model.LocationTypeId, Model.SyncDeliverySchedules, Model.GoogleExternalLocationId, Model.ClusterId);

            return WebClient.ExecuteApiRequestAsync(new UpdateLocation(Model.Id, location));
        }

        protected override void AfterSetData()
        {
            ChangeWorkScheduleTypes();

            ShopWarehouses = _warehouses.Where(x => Model.WarehouseIds.Contains(x.Id)).Select(x => Mapper.Map<WarehouseViewItem>(x)).ToObservableCollection();

            base.AfterSetData();
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Model.WorkScheduleTypeId):
                case nameof(Model.AdditionalScheduleTypeId):
                    ChangeWorkScheduleTypes();
                    break;
                case nameof(Model.LocationTypeId):
                    RaisePropertyChanged(nameof(TypeLocationShop));
                    break;
            }
        }

        private async Task RefreshWorkSchedulesAsync()
        {
            IReadOnlyCollection<WorkScheduleDto> workSchedules = await WebClient.ExecuteApiRequestAsync(new QueryWorkSchedules());

            _workScheduleViewItems = workSchedules.Select(x => Mapper.Map<WorkScheduleViewItem>(x)).ToReadOnlyObservableCollection();
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();
            Cities = cities.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private async Task RefreshWarehousesAsync()
        {
            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

            _warehouses = warehouses.Data.Where(x => (x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.ShowCase.Id) && x.Active == 1).ToReadOnlyObservableCollection();
        }

        private async Task RefreshExternalLocationsAsync()
        {
            IReadOnlyCollection<ExternalLocationDto> result = await WebClient.ExecuteApiRequestAsync(new QueryExternalLocations());

            ExternalLocations = result.OrderBy(x => x.Description).ToReadOnlyObservableCollection();
        }

        private async Task RefreshClustersAsync()
        {
            IReadOnlyCollection<ClusterDto> clusters = await WebClient.ExecuteApiRequestAsync(new QueryClusters());

            Clusters = clusters.Select(x => Mapper.Map<ClusterViewItem>(x)).ToReadOnlyObservableCollection();
        }

        private void ChangeWorkScheduleTypes()
        {
            List<WorkScheduleViewItem> items = new List<WorkScheduleViewItem>();

            if (Model.WorkScheduleTypeId.HasValue)
            {
                items.AddRange(_workScheduleViewItems.Where(x => x.TypeId == Model.WorkScheduleTypeId).ToReadOnlyObservableCollection());
            }

            if (Model.AdditionalScheduleTypeId.HasValue)
            {
                items.AddRange(_workScheduleViewItems.Where(x => x.TypeId == Model.AdditionalScheduleTypeId).ToReadOnlyObservableCollection());
            }

            WorkSchedules = items.ToReadOnlyObservableCollection();

            RaisePropertyChanged(nameof(TypeLocationShop));
        }

        private void WarehouseEdit()
        {
            SizeableDialogDocumentManagerService.ShowView<WarehouseViewModel>(new WarehouseEditParameter(SelectedWarehouse.Id), this);
        }

        private void OnWarehouseMessage(WarehouseMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:

                    if (message.Entity.LocationId != Model.Id)
                    {
                        ShopWarehouses = ShopWarehouses.Where(x => x.Id != message.Entity.Id).ToObservableCollection();
                        RaisePropertyChanged(nameof(ShopWarehouses));
                    }

                    break;
            }
        }
    }
}