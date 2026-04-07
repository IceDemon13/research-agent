using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Contractor.Contact;
using Telemart.Client.Data.Requests.Features.Country;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Country;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Cities;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Contractor
{
    public sealed class ContractorContactViewModel : TelemartDialogViewModelBase
    {
        private ContractorViewModel contractorViewModel;
        private List<ContractorCityItem> allCities;

        public ContractorContactViewModel(
             IWebClient webClient,
             IDictionaries dictionaries,
             IMessageFacadeService messageFacadeService,
             IMapper mapper,
             IErrorHandler errorHandler,
             IMessenger messenger)
             : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            ErrorHandler = errorHandler;
            Messenger = messenger;

            MailToCommand = new DelegateCommand(MailTo, () => !string.IsNullOrWhiteSpace(Model?.Email));
            ChangeCountryCommand = new DelegateCommand(SelectedCountryChanged, () => allCities != null && Model != null);
            CreateForeignCityCommand = new DelegateCommand(CreateForeignCity, () => Model?.CountryId != null && Model?.CountryId != Constants.UkraineCountryId);

            ContactStatuses = new List<KeyValuePair<string, bool>>
            {
                new KeyValuePair<string, bool>("Активен", true),
                new KeyValuePair<string, bool>("Уволен", false)
            };

            Messenger.Register<ForeignCityMessage>(this, OnForeignCityMessage);
        }

        public ContractorContactViewModel()
        {
        }

        #region Commands

        public IDelegateCommand MailToCommand { get; }

        public IDelegateCommand ChangeCountryCommand { get; }

        public IDelegateCommand CreateForeignCityCommand { get; }

        #endregion

        #region INPC

        public ContractorContactViewItem Model
        {
            get { return GetProperty(() => Model); }
            set { SetProperty(() => Model, value); }
        }

        public ObservableCollection<ContractorCityItem> Cities
        {
            get { return GetProperty(() => Cities); }
            set { SetProperty(() => Cities, value); }
        }

        public ObservableCollection<CountryDto> Countries
        {
            get { return GetProperty(() => Countries); }
            private set { SetProperty(() => Countries, value); }
        }

        public ObservableCollection<ComboBoxItem> Positions
        {
            get { return GetProperty(() => Positions); }
            private set { SetProperty(() => Positions, value); }
        }

        public bool IsHaveMaskPhone => Model?.CountryId != null && Model?.CountryId != Constants.UkraineCountryId;

        #endregion

        public List<KeyValuePair<string, bool>> ContactStatuses { get; set; }

        private IMapper Mapper { get; }

        private IErrorHandler ErrorHandler { get; }

        private IMessenger Messenger { get; }

        protected override async Task HandleLoadedAsync()
        {
            Title = Model.Id > 0
                ? "Изменение контакта"
                : "Создание контакта";

            await RefreshCountriesAsync();

            await Task.WhenAll(RefreshCitiesAsync(), RefreshPositionsAsync());
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            Model = (ContractorContactViewItem)parameter;
        }

        protected override void OnParentViewModelChanged(object parentViewModel)
        {
            if (IsInDesignMode)
            {
                return;
            }

            contractorViewModel = (ContractorViewModel)parentViewModel;
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                ContractorContactSaveDto saveDto = Mapper.Map<ContractorContactSaveDto>(Model);

                Task<Result<ContractorContactDto>> saveTask = Model.Id > 0
                    ? WebClient.ExecuteApiRequestAsync(new UpdateContractorContact(contractorViewModel.Model.Id, saveDto))
                    : WebClient.ExecuteApiRequestAsync(new CreateContractorContact(contractorViewModel.Model.Id, saveDto));

                Result<ContractorContactDto> result = await saveTask;

                Mapper.Map(result.Data, Model);

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Контакт сохранен с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Контакт успешно сохранен");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении контакта");
                ShowValidationResultView("Ошибки при сохранении контакта", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save contractor contact");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении контакта");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save contractor contact");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении контакта");
            }
        }

        private void MailTo()
        {
            ProcessHelper.Start($"mailto:{Model.Email}");
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            allCities = cities
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ContractorCityItem { Id = x.Id, Name = x.Name, CountryId = Constants.UkraineCountryId })
                .ToList();

            List<ForeignCityDto> foreignCitiesDto = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryForeignCities()),
                "получении списка инностранных городов",
                null,
                this,
                true,
                showNotification: false);
            if (foreignCitiesDto != null)
            {
                allCities.AddRange(foreignCitiesDto.Select(x => new ContractorCityItem { Id = x.Id, Name = x.Name, CountryId = x.CountryId }));
            }

            SelectedCountryChanged();
        }

        private async Task RefreshCountriesAsync()
        {
            List<CountryDto> countriesDto = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryCountries()),
                "получении списка стран",
                null,
                this,
                true,
                showNotification: false);

            if (countriesDto != null)
            {
                Countries = countriesDto.ToObservableCollection();
            }
        }

        private async Task RefreshPositionsAsync()
        {
            List<ContractorContactPositionDto> positions = await WebClient.ExecuteApiRequestAsync(new QueryContractorContactPositions());

            Positions = positions
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                .ToObservableCollection();
        }

        private void CreateForeignCity()
        {
            DialogDocumentManagerService.ShowView<ForeignCityCreateViewModel>(Model?.CountryId, this);
        }

        private void SelectedCountryChanged()
        {
            int countryId = Model.CountryId ?? Constants.UkraineCountryId;

            Cities = allCities.Where(x => x.CountryId == countryId).ToObservableCollection();

            RaisePropertiesChanged(nameof(IsHaveMaskPhone));
        }

        private void OnForeignCityMessage(ForeignCityMessage message)
                {
                    switch (message.MessageType)
                    {
                        case MessageType.Added:
                            allCities.Add(new ContractorCityItem { Id = message.Entity.Id, Name = message.Entity.Name, CountryId = message.Entity.CountryId });
                            SelectedCountryChanged();
                            break;
                    }
                }
    }
}