using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceCenters
{
    public sealed class ServiceCenterViewModel : TelemartEditorViewModelBase<ServiceCenterDto, ServiceCenterViewMessage, ServiceCenterViewItem>
    {
        public ServiceCenterViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
        }

        public ServiceCenterViewModel()
        {
        }

        #region INPC

        public ReadOnlyObservableCollection<CityDto> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<NpWarehouseDto> NpWarehouses
        {
            get { return GetProperty(() => NpWarehouses); }
            private set { SetProperty(() => NpWarehouses, value); }
        }

        public ReadOnlyObservableCollection<NpWarehouseDto> NpPostBoxes
        {
            get { return GetProperty(() => NpPostBoxes); }
            private set { SetProperty(() => NpPostBoxes, value); }
        }

        public ReadOnlyObservableCollection<StreetDto> NpStreets
        {
            get { return GetProperty(() => NpStreets); }
            private set { SetProperty(() => NpStreets, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ContractorContactPositionDto> Positions
        {
            get { return GetProperty(() => Positions); }
            private set { SetProperty(() => Positions, value); }
        }

        public ReadOnlyObservableCollection<ServiceCenterType> ServiceCenterTypes
        {
            get { return GetProperty(() => ServiceCenterTypes); }
            private set { SetProperty(() => ServiceCenterTypes, value); }
        }

        public ReadOnlyObservableCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public ReadOnlyObservableCollection<ContractorDto> Suppliers
        {
            get { return GetProperty(() => Suppliers); }
            private set { SetProperty(() => Suppliers, value); }
        }

        public ReadOnlyObservableCollection<ServiceCenterRepairConfirmType> ServiceCenterRepairConfirmTypes
        {
            get { return GetProperty(() => ServiceCenterRepairConfirmTypes); }
            private set { SetProperty(() => ServiceCenterRepairConfirmTypes, value); }
        }

        #endregion INPC

        public override void OnDestroy()
        {
            if (Model is not null)
            {
                Model.PropertyChanged -= OnModelPropertyChangedAsync;
            }

            base.OnDestroy();
        }

        protected override string CreatedActionMessage { get; } = "создан";

        protected override string EntityName { get; } = "Сервисный центр";

        protected override string UpdatedActionMessage { get; } = "сохранен";

        protected override Task<Result<ServiceCenterDto>> CreateEntityAsync()
        {
            ServiceCenterSaveDto serviceCenterSaveDto = Mapper.Map<ServiceCenterSaveDto>(Model);
            CreateServiceCenter gatewayRequest = new CreateServiceCenter(serviceCenterSaveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override object CreateEntityMessage(ServiceCenterDto dto, MessageType messageType)
        {
            return new ServiceCenterMessage(dto, messageType);
        }

        protected override Task<ServiceCenterDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryServiceCenter(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            ServiceCenterTypes = Dictionaries.GetItems<ServiceCenterType>().ToReadOnlyObservableCollection();
            ServiceCenterRepairConfirmTypes = Dictionaries.GetItems<ServiceCenterRepairConfirmType>().ToReadOnlyObservableCollection();
            Subdivisions = Dictionaries.GetItems<Subdivision>().ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            Model.PropertyChanged += OnModelPropertyChangedAsync;

            await Task.WhenAll(
                RefreshCitiesAsync(),
                RefreshNpWarehousesAsync(),
                RefreshNpStreetsAsync(),
                RefreshSuppliersAsync(),
                RefreshPositionsAsync(),
                RefreshEmployeesAsync());

            SetEmployees();
        }

        protected override Task<LockResponse<ServiceCenterDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockServiceCenter(id));
        }

        protected override Task<LockResponse<ServiceCenterDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockServiceCenter(id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание СЦ";
        }

        protected override void SetEditTitle()
        {
            Title = $"СЦ \"{Model.Name}\" ({Model.Id})";
        }

        protected override Task<Result<ServiceCenterDto>> UpdateEntityAsync()
        {
            ServiceCenterSaveDto serviceCenterSaveDto = Mapper.Map<ServiceCenterSaveDto>(Model);
            UpdateServiceCenter gatewayRequest = new UpdateServiceCenter(serviceCenterSaveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override void AfterSetData()
        {
            SetEmployees();
        }

        private async void OnModelPropertyChangedAsync(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                switch (e.PropertyName)
                {
                    case nameof(ServiceCenterViewItem.CityId):
                        {
                            await Task.WhenAll(
                                RefreshNpWarehousesAsync(),
                                RefreshNpStreetsAsync());
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ошибки во время обновления данных");
            }
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            Cities = cities
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshNpWarehousesAsync()
        {
            if (Model.CityId.HasValue)
            {
                List<NpWarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryNpWarehouses(Model.CityId.Value), true);
                List<NpWarehouseDto> postBoxes = await WebClient.ExecuteApiRequestAsync(new QueryNpPostboxes(Model.CityId.Value), true);

                NpWarehouses = warehouses
                    .Where(x => x.Active)
                    .OrderBy(x => x.Number)
                    .ToReadOnlyObservableCollection();

                NpPostBoxes = postBoxes
                    .Where(x => x.Active)
                    .OrderBy(x => x.Number)
                    .ToReadOnlyObservableCollection();
            }
        }

        private async Task RefreshNpStreetsAsync()
        {
            if (Model.CityId.HasValue)
            {
                List<StreetDto> streets = await WebClient.ExecuteApiRequestAsync(new QueryNpStreets(Model.CityId.Value), true);

                NpStreets = streets
                    .Where(x => x.Active)
                    .OrderBy(x => x.Name)
                    .ToReadOnlyObservableCollection();
            }
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees
                .Where(x => (x.Active && x.HasAnyRole(Role.ServiceManager, Role.Product)) || x.Id == Model.EmployeeId)
                .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshPositionsAsync()
        {
            List<ContractorContactPositionDto> positions = await WebClient.ExecuteApiRequestAsync(new QueryContractorContactPositions(), true);

            Positions = positions
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshSuppliersAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            Suppliers = contractors
                .Where(x => (!x.IsFolder && x.Active && x.IsSupplier) || x.Id == Model.SupplierId)
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private void SetEmployees()
        {
            if (Employees != null)
            {
                Model.Employee = ModelOriginal.Employee = Employees.FirstOrDefault(x => x.Id == Model.EmployeeId);
            }
        }
    }
}