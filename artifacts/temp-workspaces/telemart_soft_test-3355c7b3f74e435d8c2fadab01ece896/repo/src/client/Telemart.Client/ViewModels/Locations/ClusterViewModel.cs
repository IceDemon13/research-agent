using System;
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
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Locations
{
    public sealed class ClusterViewModel : TelemartEditorViewModelBase<ClusterDto, ClusterParameter, ClusterViewItem>
    {
        private IReadOnlyCollection<LocationEntityDto> _allLocations;

        public ClusterViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            AddLocationsCommand = new DelegateCommand(AddLocations, () => SelectedForAddLocation != null);
            ShowLocationCommand = new DelegateCommand(ShowLocation, () => SelectedLocation != null);

            messenger.Register<LocationMessage>(this, OnLocationMessage);
        }

        #region INPC

        public ReadOnlyObservableCollection<LocationViewItem> Locations
        {
            get { return GetProperty(() => Locations); }
            set { SetProperty(() => Locations, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public LocationViewItem SelectedLocation
        {
            get { return GetProperty(() => SelectedLocation); }
            set { SetProperty(() => SelectedLocation, value); }
        }

        public LocationViewItem SelectedForAddLocation
        {
            get { return GetProperty(() => SelectedForAddLocation); }
            set { SetProperty(() => SelectedForAddLocation, value); }
        }

        public string NullTextSelectLocation => Locations?.Any() == true
            ? "Выбирите локацию"
            : "Нет локаций вне кластера";

        #endregion

        #region Commands

        public IDelegateCommand AddLocationsCommand { get; }

        public IDelegateCommand ShowLocationCommand { get; }

        #endregion

        protected override string CreatedActionMessage => "сохранен";

        protected override string EntityName => "Кластер";

        protected override string UpdatedActionMessage => "сохранен";

        protected override Task<Result<ClusterDto>> CreateEntityAsync()
        {
            return WebClient.ExecuteApiRequestAsync(new CreateCluster(Model.Name, Model.Locations?.Select(x => x.Id).ToArray()));
        }

        protected override Task<Result<ClusterDto>> UpdateEntityAsync()
        {
            return WebClient.ExecuteApiRequestAsync(new UpdateCluster(Mapper.Map<ClusterDto>(Model)));
        }

        protected override Task<ClusterDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryCluster(id));
        }

        protected override object CreateEntityMessage(ClusterDto dto, MessageType messageType)
        {
            return new ClusterMessage(dto, messageType);
        }

        protected override Task<LockResponse<ClusterDto>> LockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override Task<LockResponse<ClusterDto>> UnlockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание кластера";
        }

        protected override void SetEditTitle()
        {
            Title = $"Кластер \"{Model?.Name}\" ({Model?.Id})";
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(RefreshCitiesAsync(), RefreshLocationsAsync());

            await base.HandleLoadedAsync();
        }

        protected override void AfterSetData()
        {
            Model.Locations = _allLocations.Where(x => Model.LocationIds?.Contains(x.Id) == true)
                .Select(x => Mapper.Map<LocationViewItem>(x))
                .ToObservableCollection();

            ModelOriginal.Locations = Model.Locations
                .Select(x => (LocationViewItem)x.Clone())
                .ToObservableCollection();

            base.AfterSetData();
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();
            Cities = cities.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private async Task RefreshLocationsAsync()
        {
            List<LocationEntityDto> locationDtos = await WebClient.ExecuteApiRequestAsync(new QueryLocations());

            _allLocations = locationDtos.ToReadOnlyObservableCollection();

            Locations = locationDtos.Where(x => !x.ClusterId.HasValue)
                .Select(x => Mapper.Map<LocationViewItem>(x))
                .ToReadOnlyObservableCollection();
        }

        private void AddLocations()
        {
            Model.Locations ??= new ObservableCollection<LocationViewItem>();

            Model.Locations.Add(SelectedForAddLocation);

            Locations = Locations.Where(x => x.Id != SelectedForAddLocation.Id).ToReadOnlyObservableCollection();

            SelectedForAddLocation = null;

            RaisePropertiesChanged(nameof(Model.Locations), nameof(Locations));
        }

        private void ShowLocation()
        {
            if (IsNew)
            {
                return;
            }

            DialogDocumentManagerService.ShowView<LocationViewModel>(new LocationParameter(SelectedLocation.Id), this);
        }

        private void OnLocationMessage(LocationMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:

                    if (Model.Id != message.Entity.ClusterId)
                    {
                        LocationViewItem item = Model.Locations.FirstOrDefault(x => x.Id == message.Entity.Id);
                        LocationViewItem itemOrigin = ModelOriginal.Locations.FirstOrDefault(x => x.Id == message.Entity.Id);

                        if (item != null)
                        {
                            Model.Locations.Remove(item);
                            ModelOriginal.Locations.Remove(itemOrigin);
                        }
                    }

                    break;
            }
        }
    }
}