using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AdditionalService;
using Telemart.Client.Data.Requests.Features.Subdivision;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.Requests.Features.Warehouse.Performance;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;
using Telemart.Client.ViewModels.AdditionalService;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;
using DayOfWeek = Telemart.Client.Dictionaries.DayOfWeek;

namespace Telemart.Client.ViewModels.Warehouse
{
    public sealed class WarehousePerfomanceEditViewModel
        : TelemartEditorViewModelBase<WarehousePerformanceDto, WarehousePerfomanceEditParameter, WarehousePerfomanceViewItem>
    {
        private WarehousePerfomanceEditParameter parameter;

        public WarehousePerfomanceEditViewModel(
           IWebClient webClient,
           IDictionaries dictionaries,
           IMessageFacadeService messageFacadeService,
           IMapper mapper,
           IMessenger messenger)
           : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
        }

        public WarehousePerfomanceEditViewModel()
        {
        }

        public ReadOnlyObservableCollection<DayOfWeek> DaysOfWeek
        {
            get { return GetProperty(() => DaysOfWeek); }
            private set { SetProperty(() => DaysOfWeek, value); }
        }

        public ReadOnlyObservableCollection<WarehouseWorkTypeDto> WorkTypes
        {
            get { return GetProperty(() => WorkTypes); }
            private set { SetProperty(() => WorkTypes, value); }
        }

        public ReadOnlyObservableCollection<SubdivisionDto> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public ReadOnlyObservableCollection<HierarchicalItem> AdditionalServices
        {
            get { return GetProperty(() => AdditionalServices); }
            private set { SetProperty(() => AdditionalServices, value); }
        }

        public HierarchicalItem? SelectedAdditionalService
        {
            get { return GetProperty(() => SelectedAdditionalService); }
            set { SetProperty(() => SelectedAdditionalService, value, () => Model.AdditionalServiceId = SelectedAdditionalService?.Id); }
        }

        protected override string CreatedActionMessage => "создано";

        protected override string EntityName => "Правило";

        protected override string UpdatedActionMessage => "сохранено";

        public static void BuildMetadata(MetadataBuilder<WarehousePerfomanceEditViewModel> builder)
        {
            builder.Property(x => x.SelectedAdditionalService)
                .MatchesInstanceRule((x, y) => y.Model?.WorkId != WarehouseWorkTypeIds.AdditionalServiceWorkTypeId || x.HasValue, () => Resources.RequiredErrorMessage)
                .MatchesRule(x => x == null || x?.Active == true, () => "Выберите услугу, а не группу");
        }

        protected override object CreateEntityMessage(WarehousePerformanceDto dto, MessageType messageType)
        {
            return new WarehousePerfomanceMessage(dto, messageType);
        }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (WarehousePerfomanceEditParameter)Parameter;

            List<WarehouseWorkTypeDto> resultWorkTypes = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseWorkTypes());
            WorkTypes = resultWorkTypes.ToReadOnlyObservableCollection();

            List<SubdivisionDto> resultSubdivisions = await WebClient.ExecuteApiRequestAsync(new QuerySubdivisions());
            Subdivisions = resultSubdivisions.ToReadOnlyObservableCollection();

            List<AdditionalServiceGroupDto> additionalServiceGroups = await WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceGroups());
            List<AdditionalServiceDto> additionalServices = await WebClient.ExecuteApiRequestAsync(new QueryAdditionalServices(new AdditionalServicesFilteringItem(active: true))).GetPagedResultDataAsync();

            AdditionalServices = additionalServiceGroups
                .Select(x => new HierarchicalItem(-x.Id, x.Name, -x.ParentId, false))
                .Concat(additionalServices.Select(x => new HierarchicalItem(x.Id, x.Name, -x.GroupId, true)))
                .ToReadOnlyObservableCollection();

            DaysOfWeek = Dictionaries.GetItems<DayOfWeek>().ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            Model.PropertyChanged += OnModelPropertyChanged;

            if (Model.AdditionalServiceId.HasValue)
            {
                SelectedAdditionalService = AdditionalServices.FirstOrDefault(x => x.Id == Model.AdditionalServiceId);
            }
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WarehousePerfomanceViewItem.WorkId))
            {
                RaisePropertyChanged(nameof(SelectedAdditionalService));
            }
        }

        protected override Task<WarehousePerformanceDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryPerformance(parameter.WarehouseId, id));
        }

        protected override Task<LockResponse<WarehousePerformanceDto>> LockEntityAsync(int id)
        {
            throw new NotImplementedException();
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание правила";
        }

        protected override void SetEditTitle()
        {
            Title = "Изменение правила";
        }

        protected override Task<LockResponse<WarehousePerformanceDto>> UnlockEntityAsync(int id)
        {
            throw new NotImplementedException();
        }

        protected override Task<Result<WarehousePerformanceDto>> CreateEntityAsync()
        {
            WarehousePerfomanceSaveDto saveDto = MapViewItem(Model);
            CreatePerformance gatewayRequest = new CreatePerformance(Model.Id, saveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override Task<Result<WarehousePerformanceDto>> UpdateEntityAsync()
        {
            WarehousePerfomanceSaveDto saveDto = MapViewItem(Model);
            UpdatePerformance gatewayRequest = new UpdatePerformance(parameter.WarehouseId, Model.Id, saveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(WarehousePerfomanceViewItem.WorkId):

                    if (Model.WorkId != WarehouseWorkTypeIds.AdditionalServiceWorkTypeId)
                    {
                        SelectedAdditionalService = null;
                    }
                    else if (Model.WorkId != WarehouseWorkTypeIds.AssemblyServiceWorkTypeId)
                    {
                        Model.SubdivisionId = null;
                    }

                    break;
            }
        }

        private WarehousePerfomanceSaveDto MapViewItem(WarehousePerfomanceViewItem viewItem)
        {
            WarehousePerfomanceSaveDto saveDto = new WarehousePerfomanceSaveDto
            {
                Activity = viewItem.Activity,
                WorkId = viewItem.WorkId,
                DayOfWeek = viewItem.DayOfWeek,
                Estimate = viewItem.Estimate,
                Performance = viewItem.Performance,
                AdditionalServiceId = viewItem.AdditionalServiceId,
                WarehouseId = parameter.WarehouseId,
                Id = viewItem.Id,
                SubdivisionId = viewItem.SubdivisionId
            };

            return saveDto;
        }
    }
}