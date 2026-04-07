using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Employee.Actions;
using Telemart.Client.Data.Requests.Features.WorkPlace;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Employee
{
    internal sealed class DirectoryEmployeesViewModel : ViewModelBase, ISupportHotkeys
    {
        private List<CustomComboBoxItem> cityFilterItems;
        private List<CustomComboBoxItem> subdivisionFilterItems;

        public DirectoryEmployeesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessenger messenger,
            IErrorHandler errorHandler,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            ILogger<DirectoryEmployeesViewModel> logger)
            : this()
        {
            ErrorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            Messenger.Register<EmployeeMessage>(this, OnEmployeeMessage);

            Employees = new ObservableRangeCollection<EmployeeViewItem>();
            Logger = logger;
        }

        public DirectoryEmployeesViewModel()
        {
            HandleLoadedCommand = new DelegateCommand(HandleLoaded);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            AddCommand = new DelegateCommand(Add);
            ActivateEmployeesByWorkPlaceTypeCommand = new AsyncCommand(ActivateEmployeesByWorkPlaceTypeAsync, () => WebClient.IsOperationAllowed(BusinessOperation.ActivateEmployees));
            ActivateAllEmployeesCommand = new AsyncCommand(ActivateAllEmployeesAsync, () => WebClient.IsOperationAllowed(BusinessOperation.ActivateEmployees));
            EditCommand = new AsyncCommand<EmployeeViewItem>(EditAsync, x => x != null);
            DeleteCommand = new AsyncCommand<EmployeeViewItem>(DeleteAsync, x => x != null);
            ShowFilterPopupHandlerCommand = new DelegateCommand<FilterPopupEventArgs>(ShowFilterPopupHandler);
        }

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IAsyncCommand DeleteCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand HandleLoadedCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand ActivateEmployeesByWorkPlaceTypeCommand { get; }

        public IAsyncCommand ActivateAllEmployeesCommand { get; }

        public IDelegateCommand ShowFilterPopupHandlerCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Departments
        {
            get { return GetProperty(() => Departments); }
            private set { SetProperty(() => Departments, value); }
        }

        public EmployeeViewItem CurrentEmployee
        {
            get { return GetProperty(() => CurrentEmployee); }
            set { SetProperty(() => CurrentEmployee, value); }
        }

        public ObservableRangeCollection<EmployeeViewItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool ActivationVisible => WebClient?.AuthenticatedEmployee.HasAnyRole(Role.Admin) == true;

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDictionaries Dictionaries { get; }

        private IMapper Mapper { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IMessenger Messenger { get; }

        private IWebClient WebClient { get; }

        private IErrorHandler ErrorHandler { get; }

        private ILogger Logger { get; }

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
                    EditCommand.Execute(CurrentEmployee);
                    handled = true;
                    break;
                case HotkeyMessageType.ShowColumnChooser:
                    IsColumnChooserVisible = !IsColumnChooserVisible;
                    handled = true;
                    break;
            }

            return handled;
        }

        private void Add()
        {
            DialogDocumentManagerService.ShowView<CreateEmployeeViewModel>(null, this);
        }

        private async Task DeleteAsync(EmployeeViewItem employeeViewItem)
        {
            const string ErrorMessage = "Ошибка при увольнении сотрудника";

            if (!MessageFacadeService.Confirm($"Вы действительно хотите уволить сотрудника {employeeViewItem.Name}?"))
            {
                return;
            }

            try
            {
                Result<EmployeeRichDto> result = await WebClient.ExecuteApiRequestAsync(new DeleteEmployee(employeeViewItem.Id));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Сотрудник уволен с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Сотрудник успешно уволен");
                }

                Messenger.Send(new EmployeeMessage(result.Data, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(ErrorMessage);
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to dismiss employee");
                MessageFacadeService.ShowNotificationError(ErrorMessage);
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to dismiss employee");
                MessageFacadeService.ShowNotificationError(ErrorMessage);
            }
        }

        private Task EditAsync(EmployeeViewItem employeeViewItem)
        {
            if (employeeViewItem != null)
            {
                Messenger.Send(new EmployeeViewMessage(employeeViewItem.Id));
            }

            return Task.CompletedTask;
        }

        private static List<CustomComboBoxItem> GetCityFilterItems(IReadOnlyCollection<EmployeeDto> employees, IReadOnlyCollection<CityDto> cities)
        {
            List<CustomComboBoxItem> items = new List<CustomComboBoxItem>
            {
                new CustomComboBoxItem { DisplayValue = "(Не задано)", EditValue = string.Empty }
            };

            HashSet<int> hashSet = new HashSet<int>(employees.Where(x => x.CityId.HasValue).Select(x => x.CityId.Value));

            IOrderedEnumerable<CustomComboBoxItem> comboBoxItems = cities
                .Where(x => hashSet.Contains(x.Id))
                .Select(x => new CustomComboBoxItem { DisplayValue = x.Name, EditValue = x.Name })
                .OrderBy(x => x.DisplayValue);

            items.AddRange(comboBoxItems);

            return items;
        }

        private void HandleLoaded()
        {
            RefreshCommand.Execute(null);
        }

        private async Task ActivateAllEmployeesAsync()
        {
            await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new ActivateEmployees(new ActivateEmployeesDto())),
                "активации всех сотрудников",
                "Сотрудники активированы",
                this,
                true,
                showNotification: true);
        }

        private async Task ActivateEmployeesByWorkPlaceTypeAsync()
        {
            List<WorkPlaceTypeDto> workPlaceTypes = await WebClient.ExecuteApiRequestAsync(new QueryWorkPlaceTypes());

            SelectItemParameter parameter = new SelectItemParameter(workPlaceTypes.Select(x => new ComboBoxItem(x.Id, x.Name)).ToArray(), "Выбор места для активации", "Рабочее место");

            SelectItemViewModel viewModel = DialogDocumentManagerService.ShowView<SelectItemViewModel>(parameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new ActivateEmployees(new ActivateEmployeesDto()
                {
                    WorkPlaceTypeIds = new[] { viewModel.SelectedItem.Value.Id }
                })),
                "активации рабочего места",
                "Рабочее место активировано",
                this,
                true,
                showNotification: true);
        }

        private void OnEmployeeMessage(EmployeeMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Employees.Insert(0, Mapper.Map<EmployeeViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    Employees.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                Employees.Clear();

                List<CityDto> citiesList = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();
                List<EmployeeDto> employeesList = await WebClient.ExecuteApiRequestAsync(new QueryEmployees()).GetPagedResultDataAsync();

                cityFilterItems = GetCityFilterItems(employeesList, citiesList);
                subdivisionFilterItems = Dictionaries.GetItems<Subdivision>()
                    .Select(x => new CustomComboBoxItem { DisplayValue = x.Name, EditValue = x })
                    .ToList();

                Cities = citiesList.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

                await LoadDepartmentsAsync();

                Employees.AddRange(employeesList.Select(Mapper.Map<EmployeeViewItem>));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed refresh DirectoryEmployeesViewModel");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task LoadDepartmentsAsync()
        {
            List<DepartmentDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

            Departments = dtos.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private void ShowFilterPopupHandler(FilterPopupEventArgs e)
        {
            switch (e.Column.FieldName)
            {
                case nameof(EmployeeViewItem.Subdivision):
                    e.ComboBoxEdit.ItemsSource = subdivisionFilterItems;
                    break;
                case nameof(EmployeeViewItem.CityId):
                    e.ComboBoxEdit.ItemsSource = cityFilterItems;
                    break;
            }
        }

        private bool ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }
    }
}