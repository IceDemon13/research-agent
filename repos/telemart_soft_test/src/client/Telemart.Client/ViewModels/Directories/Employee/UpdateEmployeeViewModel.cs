using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.MvvmEnhancements.Grid;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Contractor.Template;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Security;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.Requests.Features.WorkAccount;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.WorkAccount;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Employee
{
    internal sealed class UpdateEmployeeViewModel : TelemartEditorViewModelBase<EmployeeRichDto, EmployeeViewMessage, EmployeeRichViewItem>
    {
        private bool loaded;

        private IReadOnlyDictionary<int, ComboBoxItem> workAccounts;
        private IReadOnlyDictionary<int, string> employeeNames;
        private IReadOnlyCollection<OperationDto> securityOperations;
        private IReadOnlyCollection<Role> roles;
        private IReadOnlyCollection<Subdivision> subdivisions;
        private IReadOnlyCollection<WarehouseDto> warehouses;
        private IReadOnlyCollection<CashboxDto> cashboxes;

        public UpdateEmployeeViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            HandleNodeCheckStateChangedCommand = new DelegateCommand(HandleNodeCheckStateChanged);
            RoleChangedCommand = new DelegateCommand(RoleChanged);
            WarehouseChangedCommand = new DelegateCommand(WarehouseChanged);
            CashboxChangedCommand = new DelegateCommand(CashboxChanged);
            SubdivisionChangedCommand = new DelegateCommand(SubdivisionChanged);
            AccountChangedCommand = new DelegateCommand(AccountChanged);
            CreateAccountCommand = new DelegateCommand(CreateAccount);
            AddOperationCommand = new DelegateCommand(AddOperation);
            DeleteOperationCommand = new DelegateCommand<EmployeeOperationViewItem>(DeleteOperation, x => x != null);
            SelectContractorTemplateCommand = new DelegateCommand(SelectContractorTemplate, () => IsLockedByCurrentEmployee);
            DeleteContractorTemplateCommand = new DelegateCommand(
                () =>
                {
                    Model.ContractorTemplateId = null;
                    ContractorTemplateName = null;
                },
                () => Model?.ContractorTemplateId != null && IsLockedByCurrentEmployee);

            CopyEmployeeCommand = new AsyncCommand(CopyEmployeeAsync, () => AllowModulesAccess && Model?.Active == true);

            Messenger.Register<EmployeeAccountMessage>(this, OnEmployeeAccountMessage);

            ErrorHandler = errorHandler;
        }

        public UpdateEmployeeViewModel()
        {
        }

        public event Action OnEmployeeLocked;

        public event Action OnEmployeeUnlocked;

        #region Commands

        public IDelegateCommand HandleNodeCheckStateChangedCommand { get; }

        public IDelegateCommand RoleChangedCommand { get; }

        public IDelegateCommand WarehouseChangedCommand { get; }

        public IDelegateCommand CashboxChangedCommand { get; }

        public IDelegateCommand SubdivisionChangedCommand { get; }

        public IDelegateCommand AccountChangedCommand { get; }

        public IDelegateCommand CreateAccountCommand { get; }

        public IDelegateCommand AddOperationCommand { get; }

        public IDelegateCommand DeleteOperationCommand { get; }

        public IDelegateCommand SelectContractorTemplateCommand { get; }

        public IDelegateCommand DeleteContractorTemplateCommand { get; }

        public IAsyncCommand CopyEmployeeCommand { get; }

        #endregion

        #region INPC

        public string ContractorTemplateName
        {
            get { return GetProperty(() => ContractorTemplateName); }
            private set { SetProperty(() => ContractorTemplateName, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AllDepartments
        {
            get { return GetProperty(() => AllDepartments); }
            private set { SetProperty(() => AllDepartments, value); }
        }

        public ReadOnlyObservableCollection<CheckableItem<string>> AllRoles
        {
            get { return GetProperty(() => AllRoles); }
            private set { SetProperty(() => AllRoles, value); }
        }

        public ReadOnlyObservableCollection<CheckableItem<WarehouseDto>> AllWarehouses
        {
            get { return GetProperty(() => AllWarehouses); }
            private set { SetProperty(() => AllWarehouses, value); }
        }

        public ReadOnlyObservableCollection<CheckableItem<CashboxDto>> AllCashboxes
        {
            get { return GetProperty(() => AllCashboxes); }
            private set { SetProperty(() => AllCashboxes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AllCities
        {
            get { return GetProperty(() => AllCities); }
            private set { SetProperty(() => AllCities, value); }
        }

        public ReadOnlyObservableCollection<CheckableItem<ComboBoxItem>> AllSubdivisions
        {
            get { return GetProperty(() => AllSubdivisions); }
            set { SetProperty(() => AllSubdivisions, value); }
        }

        public ReadOnlyObservableCollection<CategoryViewItem> AllCategories
        {
            get { return GetProperty(() => AllCategories); }
            private set { SetProperty(() => AllCategories, value); }
        }

        public ObservableCollection<CheckableItem<EmployeeAccountViewItem>> EmployeeAccounts
        {
            get { return GetProperty(() => EmployeeAccounts); }
            private set { SetProperty(() => EmployeeAccounts, value); }
        }

        public ReadOnlyObservableCollection<string> Positions
        {
            get { return GetProperty(() => Positions); }
            private set { SetProperty(() => Positions, value); }
        }

        public bool? RoleCheckAllState
        {
            get
            {
                return AllRoles?.All(r => r.IsChecked) == true ? true :
                    (AllRoles?.Any(r => r.IsChecked) == true ? null : false);
            }

            set
            {
                foreach (CheckableItem<string> role in AllRoles)
                {
                    role.IsChecked = value ?? false;
                }

                RoleChanged();
            }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public bool? WarehouseCheckAllState
        {
            get
            {
                return AllWarehouses?.All(r => r.IsChecked) == true ? true :
                    (AllWarehouses?.Any(r => r.IsChecked) == true ? null : false);
            }

            set
            {
                foreach (CheckableItem<WarehouseDto> warehouse in AllWarehouses)
                {
                    warehouse.IsChecked = value ?? false;
                }

                WarehouseChanged();
            }
        }

        public bool? CashboxCheckAllState
        {
            get
            {
                return AllCashboxes?.All(r => r.IsChecked) == true ? true :
                    (AllCashboxes?.Any(r => r.IsChecked) == true ? null : false);
            }

            set
            {
                foreach (CheckableItem<CashboxDto> cashbox in AllCashboxes)
                {
                    cashbox.IsChecked = value ?? false;
                }

                CashboxChanged();
            }
        }

        public bool? SubdivisionCheckAllState
        {
            get
            {
                return AllSubdivisions?.All(r => r.IsChecked) == true ? true :
                    (AllSubdivisions?.Any(r => r.IsChecked) == true ? null : false);
            }

            set
            {
                foreach (CheckableItem<ComboBoxItem> subdivision in AllSubdivisions)
                {
                    subdivision.IsChecked = value ?? false;
                }

                SubdivisionChanged();
            }
        }

        public bool? AccountCheckAllState
        {
            get
            {
                return EmployeeAccounts?.All(r => r.IsChecked) == true ? true :
                    (EmployeeAccounts?.Any(r => r.IsChecked) == true ? null : false);
            }

            set
            {
                foreach (CheckableItem<EmployeeAccountViewItem> account in EmployeeAccounts)
                {
                    account.IsChecked = value ?? false;
                }

                AccountChanged();
            }
        }

        public bool AllowModulesAccess => WebClient.IsOperationAllowed(BusinessOperation.EmployeesModulesAccess);

        #endregion

        #region DialogSettings

        public override int Height => 530;

        public override int MinHeight => 530;

        public override int MinWidth => 640;

        public override int Width => 640;

        #endregion

        protected override string CreatedActionMessage { get; } = "создан";

        protected override string EntityName { get; } = "Сотрудник";

        protected override string UpdatedActionMessage { get; } = "сохранен";

        private IErrorHandler ErrorHandler { get; }

        protected override Task<Result<EmployeeRichDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(EmployeeRichDto dto, MessageType messageType)
        {
            return new EmployeeMessage(dto, messageType);
        }

        protected override Task<EmployeeRichDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryEmployee(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            await base.HandleLoadedAsync();

            roles = Dictionaries.GetItems<Role>();
            subdivisions = Dictionaries.GetItems<Subdivision>();
            AllSubdivisions = subdivisions.Select(x => new CheckableItem<ComboBoxItem>(new ComboBoxItem(x.Id, x.Name), Model.AllowSubdivisions.Contains(x.Id))).ToReadOnlyObservableCollection();
            AllRoles = roles.Select(x => new CheckableItem<string>(x.Name, Model.Roles.Contains(x.Name))).OrderBy(x => x.Item).ToReadOnlyObservableCollection();

            await Task.WhenAll(
                RefreshCitiesAsync(),
                RefreshWarehousesAsync(),
                RefreshCashboxesAsync(),
                RefreshCategoriesAsync(),
                RefreshWorkAccountsAsync(),
                RefreshPositionsAsync(),
                RefreshEmployeesAsync(),
                RefreshOperationsAsync(),
                RefreshDepartmentsAsync());

            HashSet<int> allowCategories = new HashSet<int>(Model.AllowCategories);

            foreach (CategoryViewItem category in AllCategories)
            {
                category.Selected = allowCategories.Contains(category.Id);
            }

            AllCashboxes = AllCashboxes
                .OrderByDescending(x => x.IsChecked)
                .ThenByDescending(x => x.Item.IsActive)
                .ThenBy(x => x.Item.Name)
                .ToReadOnlyObservableCollection();

            AllWarehouses = AllWarehouses
                .OrderByDescending(x => x.IsChecked)
                .ThenByDescending(x => x.Item.Active)
                .ThenBy(x => x.Item.Name)
                .ToReadOnlyObservableCollection();

            Model.Accounts.ForEach(x => x.Account = workAccounts.GetValueOrDefault(x.AccountId));
            ModelOriginal.Accounts.ForEach(x => x.Account = workAccounts.GetValueOrDefault(x.AccountId));

            EmployeeAccounts = Model.Accounts
                .Select(x => new CheckableItem<EmployeeAccountViewItem>(x, x.Active))
                .OrderBy(x => x.Item.Account.DisplayValue)
                .ThenBy(x => x.Item.Login)
                .ToObservableCollection();

            if (Model.ContractorTemplateId.HasValue)
            {
                ContractorTemplateDto dto = await WebClient.ExecuteApiRequestAsync(new QueryContractorTemplate(Model.ContractorTemplateId.Value));
                ContractorTemplateName = string.Empty;
                ContractorTemplateName = dto.Name;
                RaisePropertyChanged(nameof(ContractorTemplateName));
            }

            SummaryItems = GetSummaryItems();

            loaded = true;
        }

        protected override Task<LockResponse<EmployeeRichDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockEmployee(id));
        }

        protected override Task<LockResponse<EmployeeRichDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockEmployee(id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание сотрудника";
        }

        protected override void SetEditTitle()
        {
            Title = $"Сотрудник \"{Model.Name}\" ({Model.Id})";
        }

        protected override Task<Result<EmployeeRichDto>> UpdateEntityAsync()
        {
            EmployeeUpdateDto dto = new EmployeeUpdateDto
            {
                Id = Model.Id,
                Phone1 = Model.Phone1,
                Phone2 = Model.Phone2,
                Skype = Model.Skype,
                Telegram = Model.Telegram,
                Position = Model.Position,
                CityId = Model.CityId!.Value,
                SubdivisionId = Model.SubdivisionId!.Value,
                DepartmentId = Model.DepartmentId,
                ContractorTemplateId = Model.ContractorTemplateId,
                CardKey = Model.CardKey,
                ClientAccessDenied = Model.ClientAccessDenied,
                Roles = Model.Roles.ToList(),
                AllowWarehouses = Model.AllowWarehouses.ToList(),
                AllowSubdivisions = Model.AllowSubdivisions.ToList(),
                AllowCashboxes = Model.AllowCashboxes.ToList(),
                AllowCategories = Model.AllowCategories.ToList(),
                Accounts = Model.Accounts.Select(x => new EmployeeAccountSaveDto { Id = x.Id, Active = x.Active }).ToList(),
                EmployeeOperations = Model.EmployeeOperations.Select(x => new EmployeeOperationSaveDto { Id = x.Id, Allow = x.Allow }).ToList()
            };

            return WebClient.ExecuteApiRequestAsync(new UpdateEmployee(dto));
        }

        protected override void AfterSetData()
        {
            if (IsLockedByCurrentEmployee)
            {
                OnEmployeeLocked?.Invoke();
            }
            else
            {
                OnEmployeeUnlocked?.Invoke();
            }

            RaisePropertiesChanged(
                nameof(AllRoles),
                nameof(RoleCheckAllState),
                nameof(AllWarehouses),
                nameof(WarehouseCheckAllState),
                nameof(AllCashboxes),
                nameof(CashboxCheckAllState),
                nameof(AllSubdivisions),
                nameof(SubdivisionCheckAllState),
                nameof(EmployeeAccounts),
                nameof(AccountCheckAllState));
        }

        private async Task RefreshOperationsAsync()
        {
            securityOperations = await WebClient.ExecuteApiRequestAsync(new QueryOperations(), true);
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            AllCities = cities
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshWarehousesAsync()
        {
            warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            AllWarehouses = warehouses
                .OrderByDescending(x => x.Position)
                .Select(x => new CheckableItem<WarehouseDto>(x, Model.AllowWarehouses.Contains(x.Id)))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshCashboxesAsync()
        {
            cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            AllCashboxes = cashboxes
                .OrderBy(x => x.Name)
                .Select(x => new CheckableItem<CashboxDto>(x, Model.AllowCashboxes.Contains(x.Id)))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshCategoriesAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            AllCategories = categories
                .OrderBy(x => x.Position)
                .Select(x => Mapper.Map<CategoryViewItem>(x))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshWorkAccountsAsync()
        {
            List<WorkAccountDto> accounts = await WebClient.ExecuteApiRequestAsync(new QueryWorkAccounts(), true);

            workAccounts = accounts.ToDictionary(x => x.Id, x => new ComboBoxItem(x.Id, x.Name));
        }

        private async Task RefreshPositionsAsync()
        {
            List<EmployeePositionDto> positions = await WebClient.ExecuteApiRequestAsync(new QueryActiveEmployeePositions(), true);

            Positions = positions.OrderBy(x => x.Name).Select(x => x.Name).ToReadOnlyObservableCollection();
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            employeeNames = employees.ToDictionary(x => x.Id, y => y.Name);
        }

        private async Task RefreshDepartmentsAsync()
        {
            List<DepartmentDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

            AllDepartments = dtos
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private void CreateAccount()
        {
            DialogDocumentManagerService.ShowView<CreateEmployeeAccountViewModel>(Model.Id, this);
        }

        private void OnEmployeeAccountMessage(EmployeeAccountMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    {
                        CheckableItem<EmployeeAccountViewItem> newAccount = new CheckableItem<EmployeeAccountViewItem>(
                            Mapper.Map<EmployeeAccountViewItem>(message.Entity),
                            message.Entity.Active);

                        newAccount.Item.Account = workAccounts.GetValueOrDefault(newAccount.Item.AccountId);

                        EmployeeAccounts.Insert(0, newAccount);

                        break;
                    }

                case MessageType.Changed:
                    {
                        EmployeeAccounts.DoActionWithItem(
                            x => x.Item.Id == message.Entity.Id,
                            viewItem =>
                            {
                                Mapper.Map(message.Entity, viewItem.Item);
                                viewItem.IsChecked = viewItem.Item.Active;
                                viewItem.Item.Account = workAccounts.GetValueOrDefault(viewItem.Item.AccountId);
                            });

                        break;
                    }
            }

            RaisePropertyChanged(nameof(AccountCheckAllState));
        }

        private void HandleNodeCheckStateChanged()
        {
            if (loaded)
            {
                Model.AllowCategories = AllCategories
                    .Where(x => x.Selected.HasValue && x.Selected.Value)
                    .Select(x => x.Id)
                    .ToObservableCollection();
            }
        }

        private void RoleChanged()
        {
            if (Model != null)
            {
                Model.Roles = AllRoles
                    .Where(x => x.IsChecked)
                    .Select(x => x.Item)
                    .ToObservableCollection();
            }

            RaisePropertyChanged(nameof(RoleCheckAllState));
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            yield return new SummaryViewItem("Создал", $"{employeeNames.GetValueOrDefault(Model.CreatedBy)} ({Model.CreatedOn:dd.MM.yy HH:mm})");
            yield return new SummaryViewItem("Изменил", $"{employeeNames.GetValueOrDefault(Model.ModifiedBy)} ({Model.ModifiedOn:dd.MM.yy HH:mm})");

            DateTime endDate = Model.FiredOn ?? DateTime.Now;

            DateDiff dateDiff = Model.CreatedOn.Diff(endDate);

            yield return new SummaryViewItem("Работает", dateDiff.GetFormatted());

            if (Model.FiredOn.HasValue)
            {
                yield return new SummaryViewItem("Уволен", $"{Model.FiredOn:dd.MM.yy HH:mm}");
            }
        }

        private void WarehouseChanged()
        {
            if (Model != null)
            {
                Model.AllowWarehouses = AllWarehouses
                    .Where(x => x.IsChecked)
                    .Select(x => x.Item.Id)
                    .ToObservableCollection();
            }

            RaisePropertyChanged(nameof(WarehouseCheckAllState));
        }

        private void CashboxChanged()
        {
            if (Model != null)
            {
                Model.AllowCashboxes = AllCashboxes
                    .Where(x => x.IsChecked)
                    .Select(x => x.Item.Id)
                    .ToObservableCollection();
            }

            RaisePropertyChanged(nameof(CashboxCheckAllState));
        }

        private void SubdivisionChanged()
        {
            if (Model != null)
            {
                Model.AllowSubdivisions = AllSubdivisions
                    .Where(x => x.IsChecked)
                    .Select(x => x.Item.Id)
                    .ToObservableCollection();
            }

            RaisePropertyChanged(nameof(SubdivisionCheckAllState));
        }

        private void AccountChanged()
        {
            if (Model != null)
            {
                EmployeeAccounts.ForEach(x => x.Item.Active = x.IsChecked);
                Model.Accounts = EmployeeAccounts.Select(x => x.Item).ToObservableCollection();
            }

            RaisePropertyChanged(nameof(AccountCheckAllState));
        }

        private void AddOperation()
        {
            OperationDto[] availOperations = securityOperations
                .Where(x => Model.EmployeeOperations.All(y => y.Id != x.Id))
                .OrderBy(x => x.Id)
                .ToArray();

            AddEmployeeOperationViewModel viewModel = DialogDocumentManagerService.ShowView<AddEmployeeOperationViewModel>(availOperations, this);

            if (viewModel.IsOk)
            {
                EmployeeOperationViewItem viewItem = new EmployeeOperationViewItem
                {
                    Id = viewModel.SelectedOperation.Id,
                    Name = viewModel.SelectedOperation.Name,
                    Description = viewModel.SelectedOperation.Description,
                    Allow = true
                };

                Model.EmployeeOperations.Add(viewItem);
            }
        }

        private void DeleteOperation(EmployeeOperationViewItem employeeOperation)
        {
            Model.EmployeeOperations.Remove(employeeOperation);
        }

        private void SelectContractorTemplate()
        {
            SelectContractorTemplateViewModel viewModel = DialogDocumentManagerService.ShowView<SelectContractorTemplateViewModel>(ContractorTemplateMode.Select, this);

            if (viewModel.IsOk)
            {
                Model.ContractorTemplateId = viewModel.TemplatesEditor.SelectedContractorTemplate.Id;
                ContractorTemplateName = viewModel.TemplatesEditor.SelectedContractorTemplate.Name;
            }
        }

        private async Task CopyEmployeeAsync()
        {
            EmployeeCopyViewModel viewModel = DialogDocumentManagerService.ShowView<EmployeeCopyViewModel>(new EmployeeCopyParameter(Model.Id), this);

            if (viewModel.IsOk)
            {
                int? selectedEmployeeId = viewModel.SelectedEmployeeId;

                if (selectedEmployeeId.HasValue)
                {
                    EmployeeRichDto copyEmployee = await ErrorHandler.HandleErrorsAsync(
                        _ => WebClient.ExecuteApiRequestAsync(new QueryEmployee(selectedEmployeeId.Value)),
                        "получении сотрудника",
                        null,
                        this,
                        true,
                        showNotification: false);

                    EmployeeRichViewItem employeeForCopy = Mapper.Map<EmployeeRichViewItem>(copyEmployee);

                    if (viewModel.IsCopyRole)
                    {
                        List<string> copyRoles = employeeForCopy.Roles.ToList();

                        if (viewModel.SelectedRoleCopyChoice == CopyActionType.Add.Id)
                        {
                            copyRoles.AddRange(AllRoles.Where(x => x.IsChecked).Select(x => x.Item));
                        }

                        AllRoles = roles.Select(x => new CheckableItem<string>(x.Name, copyRoles.Contains(x.Name))).OrderBy(x => x.Item).ToReadOnlyObservableCollection();

                        RoleChanged();
                    }

                    if (viewModel.IsCopyOperation)
                    {
                        if (viewModel.SelectedOparationCopyChoice == CopyActionType.Override.Id)
                        {
                            Model.EmployeeOperations.Clear();
                            TelemartCollectionExtensions.AddRange(Model.EmployeeOperations, employeeForCopy.EmployeeOperations);
                        }
                        else
                        {
                            int[] employeeOperationIds = Model.EmployeeOperations.Select(x => x.Id).ToArray();

                            ObservableCollection<EmployeeOperationViewItem> employeeOperationsForAdd = employeeForCopy.EmployeeOperations.Where(x => employeeOperationIds.Contains(x.Id) == false).ToObservableCollection();

                            TelemartCollectionExtensions.AddRange(Model.EmployeeOperations, employeeOperationsForAdd);
                        }
                    }

                    if (viewModel.IsCopySubdivision)
                    {
                        List<int> subdivisionIds = employeeForCopy.AllowSubdivisions.ToList();

                        if (viewModel.SelectedSubdivisionCopyChoice == CopyActionType.Add.Id)
                        {
                            subdivisionIds.AddRange(AllSubdivisions.Where(x => x.IsChecked).Select(x => x.Item.Id));
                        }

                        AllSubdivisions = subdivisions.Select(x => new CheckableItem<ComboBoxItem>(new ComboBoxItem(x.Id, x.Name), subdivisionIds.Contains(x.Id))).ToReadOnlyObservableCollection();

                        SubdivisionChanged();
                    }

                    if (viewModel.IsCopyWarehouses)
                    {
                        List<int> warehouseIds = employeeForCopy.AllowWarehouses.ToList();

                        if (viewModel.SelectedWarehousesCopyChoice == CopyActionType.Add.Id)
                        {
                            warehouseIds.AddRange(AllWarehouses.Where(x => x.IsChecked).Select(x => x.Item.Id));
                        }

                        AllWarehouses = warehouses
                            .OrderByDescending(x => x.Position)
                            .Select(x => new CheckableItem<WarehouseDto>(x, warehouseIds.Contains(x.Id)))
                            .ToReadOnlyObservableCollection();

                        WarehouseChanged();

                        AllWarehouses = AllWarehouses
                            .OrderByDescending(x => x.IsChecked)
                            .ThenByDescending(x => x.Item.Active)
                            .ThenBy(x => x.Item.Name)
                            .ToReadOnlyObservableCollection();
                    }

                    if (viewModel.IsCopyCashbox)
                    {
                        List<int> cashboxIds = employeeForCopy.AllowCashboxes.ToList();

                        if (viewModel.SelectedCashboxCopyChoice == CopyActionType.Add.Id)
                        {
                            cashboxIds.AddRange(AllCashboxes.Where(x => x.IsChecked).Select(x => x.Item.Id));
                        }

                        AllCashboxes = cashboxes
                            .OrderBy(x => x.Name)
                            .Select(x => new CheckableItem<CashboxDto>(x,  cashboxIds.Contains(x.Id)))
                            .ToReadOnlyObservableCollection();

                        CashboxChanged();

                        AllCashboxes = AllCashboxes
                            .OrderByDescending(x => x.IsChecked)
                            .ThenByDescending(x => x.Item.IsActive)
                            .ThenBy(x => x.Item.Name)
                            .ToReadOnlyObservableCollection();
                    }

                    if (viewModel.IsCopyCategory)
                    {
                        List<int> categoryIds = employeeForCopy.AllowCategories.ToList();

                        if (viewModel.SelectedCategoryCopyChoice == CopyActionType.Add.Id)
                        {
                            categoryIds.AddRange(Model.AllowCategories);
                        }

                        Model.AllowCategories = categoryIds.Distinct().ToObservableCollection();

                        HashSet<int> allowCategories = new HashSet<int>(Model.AllowCategories);

                        foreach (CategoryViewItem category in AllCategories)
                        {
                            category.Selected = allowCategories.Contains(category.Id);
                        }
                    }
                }
            }
        }
    }
}