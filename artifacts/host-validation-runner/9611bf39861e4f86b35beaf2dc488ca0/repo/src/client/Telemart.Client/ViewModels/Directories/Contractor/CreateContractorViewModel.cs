using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Country;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Country;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Cities;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Contractor
{
    public sealed class CreateContractorViewModel : TelemartDialogViewModelBase
    {
        private List<ContractorCityItem> allCities;

        public CreateContractorViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            ErrorHandler = errorHandler;

            CreateForeignCityCommand = new DelegateCommand(CreateForeignCity, () => SelectedCountryId != null && SelectedCountryId != Constants.UkraineCountryId);
            Messenger.Register<ForeignCityMessage>(this, OnForeignCityMessage);
        }

        public CreateContractorViewModel()
        {
        }

        public IDelegateCommand CreateForeignCityCommand { get; }

        #region INPC

        public int? CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value); }
        }

        public int? SelectedCountryId
        {
            get { return GetProperty(() => SelectedCountryId); }
            set { SetProperty(() => SelectedCountryId, value, SelectedCountryChanged); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public bool? IsCompetitor
        {
            get { return GetProperty(() => IsCompetitor); }
            set { SetProperty(() => IsCompetitor, value, IsCompetitorChangedCallback); }
        }

        public bool IsRetail
        {
            get { return GetProperty(() => IsRetail); }
            set { SetProperty(() => IsRetail, value); }
        }

        public bool IsFolder
        {
            get { return GetProperty(() => IsFolder); }
            set { SetProperty(() => IsFolder, value, () => { RaisePropertyChanged(nameof(TypeDisplayValue)); }); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int? ParentId
        {
            get { return GetProperty(() => ParentId); }
            set { SetProperty(() => ParentId, value); }
        }

        public Subdivision Subdivision
        {
            get { return GetProperty(() => Subdivision); }
            set { SetProperty(() => Subdivision, value); }
        }

        public string Edrpou
        {
            get { return GetProperty(() => Edrpou); }
            set { SetProperty(() => Edrpou, value); }
        }

        public string TypeDisplayValue => IsFolder
            ? "Папка"
            : "Контрагент";

        public ReadOnlyObservableCollection<ContractorCityItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<ContractorPopupViewItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<CountryDto> Countries
        {
            get { return GetProperty(() => Countries); }
            private set { SetProperty(() => Countries, value); }
        }

        #endregion INPC

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<CreateContractorViewModel> builder)
        {
            builder.Property(x => x.IsCompetitor)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(ContractorConstants.NameMaxLength);

            builder.Property(x => x.Subdivision)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.EmployeeId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.CityId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedCountryId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Comment)
                .MaxLength(ContractorConstants.CommentMaxLength);
        }

        protected override async Task HandleLoadedAsync()
        {
            ContractorViewMessage parameter = (ContractorViewMessage)Parameter;

            Subdivisions = Dictionaries.GetItems<Subdivision>().ToReadOnlyObservableCollection();

            await Task.WhenAll(
                RefreshCitiesAsync(),
                RefreshContractorsAsync(),
                RefreshWarehousesAsync(),
                RefreshCountriesAsync());

            IsFolder = parameter.IsFolder;
            IsCompetitor = parameter.IsCompetitor;
            ParentId = parameter.ParentId;
            CityId = parameter.CityId;
            EmployeeId = parameter.EmployeeId;
            Subdivision = parameter.Subdivision;
            IsRetail = true;
            Name = null;
            Comment = null;

            await RefreshEmployeesAsync();

            Title = IsFolder
                ? "Создание папки"
                : "Создание контрагента";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                ContractorCreateDto dto = new ContractorCreateDto
                {
                    Id = 0,
                    IsCompetitor = IsCompetitor.Value,
                    IsRetail = IsRetail,
                    IsFolder = IsFolder,
                    ParentId = ParentId.Value,
                    Name = Name,
                    EmployeeId = EmployeeId.Value,
                    CountryId = SelectedCountryId,
                    SubdivisionId = Subdivision.Id,
                    Edrpou = !string.IsNullOrWhiteSpace(Edrpou) ? Edrpou : null,
                    Comment = Comment
                };

                if (SelectedCountryId == null || SelectedCountryId == Constants.UkraineCountryId)
                {
                    dto.CityId = CityId > 0 ? CityId : null;
                }
                else
                {
                    dto.ForeignCityId = CityId > 0 ? CityId : null;
                }

                Result<ContractorDto> result = await WebClient.ExecuteApiRequestAsync(new CreateContractor(dto));

                string createdActionMessage = $"создан{(IsFolder ? "а" : string.Empty)}";

                Messenger.Send(new ContractorMessage(result.Data, MessageType.Added));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"{TypeDisplayValue} №{result.Data.Id} {createdActionMessage} с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"{TypeDisplayValue} №{result.Data.Id} {createdActionMessage} успешно");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                ShowValidationResultView("Ошибки при создании контрагента", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create contractor");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while creating contractor");
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
            }
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors()).GetPagedResultDataAsync();

            Contractors = contractors
                .Where(x => x.IsFolder)
                .Select(x => new ContractorPopupViewItem(x.Id, x.ParentId, x.Name, x.IsFolder))
                .OrderBy(x => x.Name).ToReadOnlyObservableCollection();
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            List<CityDto> citiesList = new List<CityDto>(cities)
            {
                new CityDto { Id = -1, Name = Constants.UkraineDisplayValue, Position = -1 }
            };

            allCities = citiesList
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
        }

        private async Task RefreshCountriesAsync()
        {
            List<CountryDto> countriesDto = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryCountries(), true),
                "получении списка стран",
                null,
                this,
                true,
                showNotification: false);

            if (countriesDto != null)
            {
                Countries = countriesDto.ToReadOnlyObservableCollection();
            }
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees
                .Where(x => x.Active || x.Id == EmployeeId)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouses
                .Where(x => x.TypeId == WarehouseKind.Main.Id)
                .OrderBy(x => x.Position)
                .ToReadOnlyObservableCollection();
        }

        private void IsCompetitorChangedCallback()
        {
            if (IsCompetitor.HasValue && IsCompetitor.Value)
            {
                IsRetail = true;
            }
        }

        private void CreateForeignCity()
        {
            DialogDocumentManagerService.ShowView<ForeignCityCreateViewModel>(SelectedCountryId, this);
        }

        private void SelectedCountryChanged()
        {
            int countryId = SelectedCountryId ?? Constants.UkraineCountryId;

            Cities = allCities.Where(x => x.CountryId == countryId).ToReadOnlyObservableCollection();
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