using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Locations
{
    public sealed class LocationsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private readonly IMapper _mapper;

        public LocationsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;

            RefreshCommand = new AsyncCommand(RefreshAsync);
            EditCommand = new DelegateCommand(EditLocation, () => WebClient.IsOperationAllowed(BusinessOperation.LocationUpdate) && SelectedLocation != null);
            CreateLocationCommand = new DelegateCommand(CreateLocation, () => WebClient.IsOperationAllowed(BusinessOperation.LocationCreate));
            CreateClusterCommand = new DelegateCommand(CreateCluster, () => WebClient.IsOperationAllowed(BusinessOperation.ClusterCreate));
            EditClusterCommand = new DelegateCommand(EditCluster, () => WebClient.IsOperationAllowed(BusinessOperation.ClusterUpdate) && SelectedLocation?.ClusterId.HasValue == true);

            messenger.Register<LocationMessage>(this, OnLocationMessage);
            messenger.Register<ClusterMessage>(this, OnClusterMessage);
        }

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand CreateLocationCommand { get; }

        public IDelegateCommand CreateClusterCommand { get; }

        public IDelegateCommand EditClusterCommand { get; }

        #endregion

        #region INPC

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ObservableCollection<LocationViewItem> Locations
        {
            get { return GetProperty(() => Locations); }
            set { SetProperty(() => Locations, value); }
        }

        public ReadOnlyObservableCollection<WorkScheduleType> WorkScheduleTypes
        {
            get { return GetProperty(() => WorkScheduleTypes); }
            private set { SetProperty(() => WorkScheduleTypes, value); }
        }

        public ReadOnlyObservableCollection<LocationType> LocationTypes
        {
            get { return GetProperty(() => LocationTypes); }
            private set { SetProperty(() => LocationTypes, value); }
        }

        public ReadOnlyObservableCollection<ExternalLocationDto> ExternalLocations
        {
            get { return GetProperty(() => ExternalLocations); }
            private set { SetProperty(() => ExternalLocations, value); }
        }

        public LocationViewItem SelectedLocation
        {
            get { return GetProperty(() => SelectedLocation); }
            set { SetProperty(() => SelectedLocation, value); }
        }

        public ObservableCollection<ClusterViewItem> Clusters
        {
            get { return GetProperty(() => Clusters); }
            private set { SetProperty(() => Clusters, value); }
        }

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>(nameof(DialogDocumentManagerService), ServiceSearchMode.PreferParents);

        #endregion

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;

                case HotkeyMessageType.Edit:
                    EditCommand.Execute(null);
                    handled = true;
                    break;
                case HotkeyMessageType.ShowColumnChooser:
                    IsColumnChooserVisible = !IsColumnChooserVisible;
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            LocationTypes = Dictionaries.GetItems<LocationType>().ToReadOnlyObservableCollection();

            WorkScheduleType[] workScheduleTypes = Dictionaries.GetItems<WorkScheduleType>().ToArray();

            WorkScheduleTypes = workScheduleTypes
                .Where(x => x.ParentId == (int)WorkScheduleTypeIds.Shop
                            || x.Id == (int)WorkScheduleTypeIds.AdditionalService
                            || x.ParentId == (int)WorkScheduleTypeIds.AdditionalService)
                .ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshCitiesAsync(), RefreshExternalLocationsAsync(), RefreshClustersAsync());

            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            await RefreshLocationsAsync();
        }

        private async Task RefreshLocationsAsync()
        {
            List<LocationEntityDto> locationDtos = await WebClient.ExecuteApiRequestAsync(new QueryLocations());

            Locations = locationDtos.Select(x => _mapper.Map<LocationViewItem>(x)).ToObservableCollection();
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();
            Cities = cities.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private async Task RefreshExternalLocationsAsync()
        {
            IReadOnlyCollection<ExternalLocationDto> result = await WebClient.ExecuteApiRequestAsync(new QueryExternalLocations());

            ExternalLocations = result.OrderBy(x => x.Description).ToReadOnlyObservableCollection();
        }

        private async Task RefreshClustersAsync()
        {
            IReadOnlyCollection<ClusterDto> clusters = await WebClient.ExecuteApiRequestAsync(new QueryClusters());

            Clusters = clusters.Select(x => _mapper.Map<ClusterViewItem>(x)).ToObservableCollection();
        }

        private void EditLocation()
        {
            DialogDocumentManagerService.ShowView<LocationViewModel>(new LocationParameter(SelectedLocation.Id), this);
        }

        private void CreateLocation()
        {
            DialogDocumentManagerService.ShowView<LocationCreateViewModel>(null, this);
        }

        private void CreateCluster()
        {
            DialogDocumentManagerService.ShowView<ClusterViewModel>(new ClusterParameter(0), this);
        }

        private void EditCluster()
        {
            DialogDocumentManagerService.ShowView<ClusterViewModel>(new ClusterParameter(SelectedLocation.ClusterId!.Value), this);
        }

        private void OnLocationMessage(LocationMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Locations.Insert(0, _mapper.Map<LocationViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    Locations.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => _mapper.Map(message.Entity, viewItem));
                    break;
            }
        }

        private void OnClusterMessage(ClusterMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                case MessageType.Changed:

                    if (Clusters.Any(x => x.Id == message.Entity.Id) != true)
                    {
                        Clusters.Add(_mapper.Map<ClusterViewItem>(message.Entity));
                    }
                    else
                    {
                        ClusterViewItem cluster = Clusters.First(x => x.Id == message.Entity.Id);

                        cluster.Name = message.Entity.Name;
                    }

                    Locations.ForEach(
                        x =>
                        {
                            if (message.Entity.LocationIds?.Contains(x.Id) == true)
                            {
                                x.ClusterId = message.Entity.Id;
                            }
                        });
                    break;
            }
        }
    }
}