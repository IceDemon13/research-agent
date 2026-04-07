using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Locations;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Locations
{
    public sealed class LocationCreateViewModel : TelemartDialogViewModelBase
    {
        private readonly IMessenger _messenger;
        private readonly IErrorHandler _errorHandler;
        private readonly IMapper _mapper;
        private IReadOnlyCollection<WorkScheduleType> _allWorkScheduleTypes;

        public LocationCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IErrorHandler errorHandler,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _messenger = messenger;
            _errorHandler = errorHandler;
            _mapper = mapper;
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
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

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string Address
        {
            get { return GetProperty(() => Address); }
            set { SetProperty(() => Address, value); }
        }

        public int CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value); }
        }

        public int? WorkScheduleTypeId
        {
            get { return GetProperty(() => WorkScheduleTypeId); }
            set { SetProperty(() => WorkScheduleTypeId, value); }
        }

        public int? AdditionalScheduleTypeId
        {
            get { return GetProperty(() => AdditionalScheduleTypeId); }
            set { SetProperty(() => AdditionalScheduleTypeId, value); }
        }

        public int LocationTypeId
        {
            get { return GetProperty(() => LocationTypeId); }
            set { SetProperty(() => LocationTypeId, value, () => RaisePropertiesChanged(nameof(WorkScheduleTypeId), nameof(ClusterId))); }
        }

        public int? ClusterId
        {
            get { return GetProperty(() => ClusterId); }
            set { SetProperty(() => ClusterId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<LocationCreateViewModel> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(100, () => "Длина поля должна быть не больше чем 100  символов");

            builder.Property(x => x.Address)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(250, () => "Длина поля должна быть не больше чем 250  символов");

            builder.Property(x => x.CityId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.LocationTypeId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.WorkScheduleTypeId)
                .MatchesInstanceRule((x, y) => y.LocationTypeId != LocationType.ShopId || x > 0, () => "Выберите основной график");

            builder.Property(x => x.ClusterId)
                .MatchesInstanceRule((x, y) => y.LocationTypeId != LocationType.ShopId || x > 0, () => "Выберите кластер");
        }

        protected override async Task HandleLoadedAsync()
        {
            _allWorkScheduleTypes = Dictionaries.GetItems<WorkScheduleType>().ToArray();

            WorkScheduleTypes = _allWorkScheduleTypes.Where(x => x.ParentId == (int)WorkScheduleTypeIds.Shop).ToReadOnlyObservableCollection();

            AdditionalWorkScheduleTypes = _allWorkScheduleTypes.Where(x => x.ParentId == (int)WorkScheduleTypeIds.AdditionalService).ToReadOnlyObservableCollection();

            LocationTypes = Dictionaries.GetItems<LocationType>().ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshCitiesAsync(), RefreshClustersAsync());

            await base.HandleLoadedAsync();

            Title = "Создание локации";
        }

        protected override async Task HandleOkAsync()
        {
            Result<LocationEntityDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateLocation(Name, Address, CityId, WorkScheduleTypeId, AdditionalScheduleTypeId, LocationTypeId, ClusterId)),
                "создании локации",
                "Локация создана",
                this,
                true,
                false);

            if (result?.IsSuccess == true)
            {
                _messenger.Send(new LocationMessage(result.Data, MessageType.Added));
                CloseOk();
            }
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();
            Cities = cities.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private async Task RefreshClustersAsync()
        {
            IReadOnlyCollection<ClusterDto> clusters = await WebClient.ExecuteApiRequestAsync(new QueryClusters());

            Clusters = clusters.Select(x => _mapper.Map<ClusterViewItem>(x)).ToReadOnlyObservableCollection();
        }
    }
}