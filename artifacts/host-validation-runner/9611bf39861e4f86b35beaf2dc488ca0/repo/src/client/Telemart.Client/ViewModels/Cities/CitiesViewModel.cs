using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Area;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.District;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Cities
{
    public class CitiesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public CitiesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mapper = mapper;
            ErrorHandler = errorHandler;

            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedCity != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            DeleteCityCommand = new AsyncCommand(DeleteCityAsync, () => SelectedCity != null);

            Messenger.Register<CityMessage>(this, OnCityMessage);

            Cities = new ObservableRangeCollection<CityViewItem>();
        }

        public CitiesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand DeleteCityCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> Areas
        {
            get { return GetProperty(() => Areas); }
            private set { SetProperty(() => Areas, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Districts
        {
            get { return GetProperty(() => Districts); }
            private set { SetProperty(() => Districts, value); }
        }

        public ObservableRangeCollection<CityViewItem> Cities
        {
            get { return GetProperty(() => Cities); }
            init { SetProperty(() => Cities, value); }
        }

        public CityViewItem SelectedCity
        {
            get { return GetProperty(() => SelectedCity); }
            set { SetProperty(() => SelectedCity, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;
            switch (msg.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;

                case HotkeyMessageType.Add:
                    AddCommand.Execute(null);
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
            Task<List<DistrictDto>> districtsTask = WebClient.ExecuteApiRequestAsync(new QueryDistricts());
            Task<List<AreaDto>> areasTask = WebClient.ExecuteApiRequestAsync(new QueryAreas());

            await Task.WhenAll(districtsTask, areasTask);

            Districts = districtsTask.Result.Select(x => new ComboBoxItem(x.Id, x.NameUkr)).ToReadOnlyObservableCollection();
            Areas = areasTask.Result.Select(x => new ComboBoxItem(x.Id, x.NameUkr)).ToReadOnlyObservableCollection();

            await RefreshAsync();
        }

        private void Add()
        {
            DialogDocumentManagerService.ShowView<CityCreateViewModel>(null, this);
        }

        private void Edit()
        {
            Messenger.Send(new CityViewMessage(SelectedCity.Id));
        }

        private async Task DeleteCityAsync()
        {
            if (SelectedCity.CityCarries?.Any() == true)
            {
                MessageFacadeService.ShowNotificationError("У города проставлены способы доставки");
                return;
            }

            (await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new DeleteCity(SelectedCity.Id)), "удалении города", "Город удален", this, true))
                .IfNotNull(_ =>
                {
                    Cities.Remove(SelectedCity);
                    SelectedCity = null;
                });
        }

        private async Task RefreshAsync()
        {
            try
            {
                PagedResult<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities());

                Cities.Clear();

                Cities.AddRange(cities.Data
                    .OrderBy(x => x.Position)
                    .ThenBy(x => x.Name)
                    .Select(x => Mapper.Map<CityViewItem>(x)));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void OnCityMessage(CityMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Cities.Add(Mapper.Map<CityViewItem>(message.Entity));
                    break;

                case MessageType.Changed:
                    Cities.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }
    }
}