using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Humanizer;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Common.Validation;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Employee.Actions;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Contractor;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Employee
{
    internal sealed class CreateEmployeeViewModel : TelemartDialogViewModelBase
    {
        private const int MaxPasswordLength = 10;
        private const int MinPasswordLength = 8;
        private ContractorDto _contractorDto;

        public CreateEmployeeViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IPasswordGenerator passwordGenerator,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            PasswordGenerator = passwordGenerator;
            ErrorHandler = errorHandler;

            GeneratePasswordCommand = new DelegateCommand(GeneratePassword);
            ShowPasswordSymbolsCommand = new DelegateCommand(() => ShowPasswordSymbols = !ShowPasswordSymbols);
        }

        public CreateEmployeeViewModel()
        {
        }

        #region Commands

        public IDelegateCommand GeneratePasswordCommand { get; }

        public IDelegateCommand ShowPasswordSymbolsCommand { get; }

        #endregion

        #region INPC

        public bool CreateAccontBitrix
        {
            get { return GetProperty(() => CreateAccontBitrix); }
            private set { SetProperty(() => CreateAccontBitrix, value); }
        }

        public bool CreateAccount1C
        {
            get { return GetProperty(() => CreateAccount1C); }
            set { SetProperty(() => CreateAccount1C, value); }
        }

        public bool CreateAccountTelemart
        {
            get { return GetProperty(() => CreateAccountTelemart); }
            set { SetProperty(() => CreateAccountTelemart, value); }
        }

        public bool CreateContractor
        {
            get { return GetProperty(() => CreateContractor); }
            set { SetProperty(() => CreateContractor, value); }
        }

        public bool CreateEmail
        {
            get { return GetProperty(() => CreateEmail); }
            private set { SetProperty(() => CreateEmail, value); }
        }

        public bool CreateSiteCustomer
        {
            get { return GetProperty(() => CreateSiteCustomer); }
            set { SetProperty(() => CreateSiteCustomer, value, ChangeSiteValue); }
        }

        public bool CreateInfoSite
        {
            get { return GetProperty(() => CreateInfoSite); }
            set { SetProperty(() => CreateInfoSite, value); }
        }

        public bool CreateCashbox
        {
            get { return GetProperty(() => CreateCashbox); }
            set { SetProperty(() => CreateCashbox, value, ChangeCashboxValue); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public bool AllowSetWarehouse => CreateCashbox;

        public bool AllowSetInfoSite => CreateSiteCustomer;

        public string Email
        {
            get
            {
                string email;

                if (!string.IsNullOrWhiteSpace(NameTranslit) && !string.IsNullOrWhiteSpace(SurnameTranslit))
                {
                    email = $"{NameTranslit.Substring(0, 1).ToLower()}.{SurnameTranslit.ToLower()}@telemart.com.ua";
                }
                else
                {
                    email = string.Empty;
                }

                return email;
            }
        }

        public string FullName => Join(" ", Surname, Name);

        public string Login => Join("__", SurnameTranslit, NameTranslit);

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value, () => { RaisePropertyChanged(nameof(FullName)); }); }
        }

        public string NameTranslit
        {
            get { return GetProperty(() => NameTranslit); }
            set { SetProperty(() => NameTranslit, value, () => { RaisePropertiesChanged(nameof(Email), nameof(Login)); }); }
        }

        public string Password
        {
            get { return GetProperty(() => Password); }
            set { SetProperty(() => Password, value, () => RaisePropertyChanged(nameof(PasswordConfirmation))); }
        }

        public string PasswordConfirmation
        {
            get { return GetProperty(() => PasswordConfirmation); }
            set { SetProperty(() => PasswordConfirmation, value, () => RaisePropertyChanged(nameof(Password))); }
        }

        public string Phone1
        {
            get { return GetProperty(() => Phone1); }
            set { SetProperty(() => Phone1, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public CityDto SelectedCity
        {
            get { return GetProperty(() => SelectedCity); }
            set { SetProperty(() => SelectedCity, value); }
        }

        public Subdivision SelectedSubdivision
        {
            get { return GetProperty(() => SelectedSubdivision); }
            set { SetProperty(() => SelectedSubdivision, value); }
        }

        public bool ShowPasswordSymbols
        {
            get { return GetProperty(() => ShowPasswordSymbols); }
            set { SetProperty(() => ShowPasswordSymbols, value); }
        }

        public string Skype
        {
            get { return GetProperty(() => Skype); }
            set { SetProperty(() => Skype, value); }
        }

        public string Telegram
        {
            get { return GetProperty(() => Telegram); }
            set { SetProperty(() => Telegram, value); }
        }

        public string Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public int? DepartmentId
        {
            get { return GetProperty(() => DepartmentId); }
            set { SetProperty(() => DepartmentId, value); }
        }

        public string Surname
        {
            get { return GetProperty(() => Surname); }
            set { SetProperty(() => Surname, value, () => { RaisePropertyChanged(nameof(FullName)); }); }
        }

        public string SurnameTranslit
        {
            get { return GetProperty(() => SurnameTranslit); }
            set { SetProperty(() => SurnameTranslit, value, () => { RaisePropertiesChanged(nameof(Email), nameof(Login)); }); }
        }

        public ObservableRangeCollection<Subdivision> Subdivisions { get; } = new ObservableRangeCollection<Subdivision>();

        public ObservableRangeCollection<DepartmentDto> Departments { get; } = new ObservableRangeCollection<DepartmentDto>();

        public ObservableRangeCollection<CityDto> Cities { get; } = new ObservableRangeCollection<CityDto>();

        public ObservableRangeCollection<ComboBoxItem> Positions
        {
            get { return GetProperty(() => Positions); }
            set { SetProperty(() => Positions, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        #endregion

        private IMessenger Messenger { get; }

        private IPasswordGenerator PasswordGenerator { get; }

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<CreateEmployeeViewModel> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.CreateEmployeeViewModel_NameRequired)
                .ApplyClientNameRusUkrValidationRules();

            builder.Property(x => x.Surname)
                .Required(() => Resources.CreateEmployeeViewModel_SurnameRequired)
                .ApplyClientNameRusUkrValidationRules();

            builder.Property(x => x.NameTranslit)
                .Required(() => Resources.CreateEmployeeViewModel_NameTranslitRequired)
                .Latin();

            builder.Property(x => x.SurnameTranslit)
                .Required(() => Resources.CreateEmployeeViewModel_SurnameTranslitRequired)
                .Latin();

            builder.Property(x => x.SelectedSubdivision)
                .Required(() => Resources.CreateEmployeeViewModel_SubdivisionRequired);

            builder.Property(x => x.SelectedCity)
                .Required(() => Resources.CreateEmployeeViewModel_CityRequired);

            builder.Property(x => x.Password)
                .Required(() => Resources.CreateEmployeeViewModel_PasswordRequired)
                .Password()
                .PasswordLength(MinPasswordLength, MaxPasswordLength);

            builder.Property(x => x.PasswordConfirmation)
                .Required(() => Resources.CreateEmployeeViewModel_PasswordConfirmationRequired)
                .Password()
                .PasswordLength(MinPasswordLength, MaxPasswordLength);

            builder.Property(x => x.DepartmentId).Required();

            builder.Property(x => x.WarehouseId)
                .MatchesInstanceRule((x, y) => x.HasValue || !y.CreateCashbox, () => Resources.RequiredErrorMessage);
        }

        protected override string GetErrorText(string columnName)
        {
            string error = base.GetErrorText(columnName);

            if (string.IsNullOrWhiteSpace(error))
            {
                if ((nameof(PasswordConfirmation).Equals(columnName, StringComparison.Ordinal) || nameof(Password).Equals(columnName, StringComparison.Ordinal))
                    && !string.Equals(PasswordConfirmation, Password, StringComparison.Ordinal))
                {
                    error = "Пароль и подтверждение пароля не совпадают";
                }

                if ((nameof(SurnameTranslit).Equals(columnName, StringComparison.Ordinal) || nameof(NameTranslit).Equals(columnName, StringComparison.Ordinal))
                    && Login.Length > 20)
                {
                    error = "Имя удаленной учетной записи может состоять максимум из 20 символов";
                }
            }

            return error;
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(LoadDepartmentsAsync(), LoadEmployeePositionAsync(), LoadCitiesAsync(), LoadWarehousesAsync());

            IReadOnlyCollection<Subdivision> subdivisions = Dictionaries.GetItems<Subdivision>();
            Subdivisions.AddRange(subdivisions);

            CreateEmail = true;
            CreateAccontBitrix = true;
            CreateAccount1C = true;
            CreateAccountTelemart = false;
            CreateContractor = false;
            CreateSiteCustomer = true;

            Title = "Создание сотрудника";
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                CreateEmployee gatewayRequest = new CreateEmployee(GetEmployeeCreateDto());

                Result<EmployeeRichDto> response = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                List<string> warnings = response.Warnings.ToList();

                if (CreateContractor)
                {
                    IReadOnlyCollection<string> contractorCreateWarnings = await CreateContractorAsync(response.Data);

                    warnings.AddRange(contractorCreateWarnings);
                }

                if (CreateSiteCustomer)
                {
                    IReadOnlyCollection<string> customerCreateWarnings = await CraeteEmployeeCustomerAsync(response.Data);

                    warnings.AddRange(customerCreateWarnings);
                }

                if (CreateCashbox)
                {
                    if (!string.IsNullOrWhiteSpace(response.Data.Email))
                    {
                        IReadOnlyCollection<string> cashboxCreateWarnings = await CreateCashBoxAsync(response.Data);

                        warnings.AddRange(cashboxCreateWarnings);
                    }
                    else
                    {
                        warnings.Add("Ошибки при создании кассы сотрудника: Касса не создана из-за отсутсвия Яндекс-почты");
                    }
                }

                if (warnings.Any())
                {
                    string message = "Сотрудник создан с ошибками";

                    MessageFacadeService.ShowNotificationWarning(message);
                    ShowValidationResultView(message, warnings.Select(x => new ValidationResultItem(x, false)).ToArray());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Сотрудник успешно создан");
                }

                Messenger.Send(new EmployeeMessage(response.Data, MessageType.Added));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании сотрудника");
                ShowValidationResultView("Ошибки при создании сотрудника", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create employee");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to create employee");
                MessageFacadeService.ShowNotificationError("Ошибка при создании сотрудника");
            }
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            Name = "ДЖоН";
            Surname = "дОу";

            NameTranslit = "Jhon";
            SurnameTranslit = "Doe";

            Subdivisions.Add(Subdivision.Telemart);
            SelectedSubdivision = Subdivision.Telemart;

            CityDto city = new CityDto { Id = 1, Name = "Dnepropetrovsk", Position = 1 };
            Cities.Add(city);
            SelectedCity = city;

            PasswordConfirmation = "EbPwd230";
            Password = "EbPwd230";

            CreateEmail = true;
            CreateAccontBitrix = true;
            CreateAccount1C = true;
        }

        private static string Join(string delimeter, string s1, string s2)
        {
            return $"{s1?.Trim().Transform(To.LowerCase, To.SentenceCase)}{delimeter}{s2?.Trim().Transform(To.LowerCase, To.SentenceCase)}";
       }

        private async Task LoadCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            Cities.Clear();
            Cities.AddRange(cities.OrderBy(x => x.Position).ThenBy(x => x.Name));
        }

        private async Task LoadWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouses
                .Where(x => x.Active == 1 && x.TypeId == WarehouseKind.Pickup.Id)
                .OrderByDescending(x => x.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadDepartmentsAsync()
        {
            List<DepartmentDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

            Departments.Clear();
            Departments.AddRange(dtos.Where(x => x.Active).OrderBy(x => x.Name));
        }

        private async Task LoadEmployeePositionAsync()
        {
            List<EmployeePositionDto> positions = await WebClient.ExecuteApiRequestAsync(new QueryActiveEmployeePositions());

            Positions = positions.OrderBy(x => x.Name).Select(x => new ComboBoxItem(x.Id, x.Name)).ToObservableRangeCollection();
        }

        private void GeneratePassword()
        {
            string password = PasswordGenerator.Generate(MinPasswordLength, MaxPasswordLength);

            Password = password;
            PasswordConfirmation = password;
        }

        private async Task<IReadOnlyCollection<string>> CreateContractorAsync(EmployeeDto employee)
        {
            ContractorCreateDto dto = new ContractorCreateDto
            {
                ParentId = ContractorConstants.RetailClients,
                Name = $"Розница {employee.Name}",
                EmployeeId = employee.Id,
                SubdivisionId = employee.SubdivisionId,
                CityId = employee.CityId,
                IsFolder = false,
                IsCompetitor = false,
                IsRetail = true,
                Comment = null,
                Active = true,
                OldClient = true
            };

            return await ActionHandleErrorsAsync(
                async () =>
                {
                    Result<ContractorDto> result = await WebClient.ExecuteApiRequestAsync(new CreateContractor(dto));

                    _contractorDto = result.Data;

                    return result;
                },
                "создании контрагента",
                "Failed to create contractor");
        }

        private async Task<IReadOnlyCollection<string>> CraeteEmployeeCustomerAsync(EmployeeDto employee)
        {
            EmployeeCustomerCreateDto dto = new EmployeeCustomerCreateDto()
            {
                EmployeeId = employee.Id,
                Surname = Surname,
                Name = Name,
                Password = Password,
                ContractorId = _contractorDto?.Id,
                CreateSiteCustomerInfo = CreateInfoSite
            };

            return await ActionHandleErrorsAsync(
                () => WebClient.ExecuteApiRequestAsync(new CraeteEmployeeCustomer(dto)),
                "создании пользователя сайта",
                "Failed to create customer");
        }

        private async Task<IReadOnlyCollection<string>> CreateCashBoxAsync(EmployeeDto employee)
        {
            string stringEmployeeCashBoxLegalEntityId = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryEmployeeCashBoxLegalEntityId()),
                null,
                null,
                this,
                false,
                false,
                false);

            CashboxSaveDto createDto = new CashboxSaveDto()
            {
                EmployeeId = employee.Id,
                Name = $"{Surname} {Name} гривна",
                CurrencyId = Currency.UahId,
                TypeId = CashboxType.Employee.Id,
                IsActive = true,
                WarehouseId = WarehouseId,
                LegalEntityId = null,
                IsCreateEmployeeCashbox = true,
                AllowedPayments = new[] { Payment.CashId }
            };

            if (int.TryParse(stringEmployeeCashBoxLegalEntityId, out int employeeCashBoxLegalEntityId))
            {
                createDto.LegalEntityId = employeeCashBoxLegalEntityId;
            }

            return await ActionHandleErrorsAsync(
                () => WebClient.ExecuteApiRequestAsync(new CreateCashbox(createDto)),
                "создании кассы сотрудника",
                "Failed to create employee cashbox");
        }

        private EmployeeCreateDto GetEmployeeCreateDto()
        {
            return new EmployeeCreateDto
            {
                Name = Name,
                Surname = Surname,
                NameTranslit = NameTranslit,
                SurnameTranslit = SurnameTranslit,
                CityId = SelectedCity.Id,
                SubdivisionId = SelectedSubdivision.Id,
                Password = Password,
                PasswordConfirmation = PasswordConfirmation,
                CreateAccount1C = CreateAccount1C,
                Phone1 = Phone1,
                Phone2 = Phone2,
                Skype = Skype,
                Telegram = Telegram,
                CreateAccountTelemart = CreateAccountTelemart,
                Position = Position,
                DepartmentId = DepartmentId.Value
            };
        }

        private void ChangeCashboxValue()
        {
            if (!CreateCashbox)
            {
                WarehouseId = null;
            }

            RaisePropertiesChanged(nameof(AllowSetWarehouse));
        }

        private void ChangeSiteValue()
        {
            if (!CreateSiteCustomer)
            {
                CreateInfoSite = false;
            }

            RaisePropertiesChanged(nameof(AllowSetInfoSite));
        }

        private async Task<IReadOnlyCollection<string>> ActionHandleErrorsAsync<TResult>(Func<Task<TResult>> sourceFunc, string message, string logMessage)
            where TResult : Result
        {
            IReadOnlyCollection<string> warnings;

            try
            {
                TResult actionResult = await sourceFunc();

                warnings = actionResult.Warnings;
            }
            catch (UnexpectedSatusException exception)
            {
                warnings = exception.GetErrorItems().Select(x => x.Message).ToArray();
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, logMessage);
                warnings = new[] { Resources.ServerUnavailable };
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, logMessage);
                warnings = new[] { "Неизвестная ошибка" };
            }

            return warnings.Select(x => $"Ошибка при {message}: {x}").ToList();
        }
    }
}