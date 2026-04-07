using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Requests.Features.ServiceRequest.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse.Delivery;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse.Delivery;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store.CreateScanSheetForEntities;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public sealed class ServiceRequestsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public ServiceRequestsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory,
            IServiceRequestPrinter serviceRequestPrinter)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            ServiceRequestPrinter = serviceRequestPrinter;
            LockableOperationProcessorFactory = lockableOperationProcessorFactory;

            RefreshCommand = new AsyncCommand(RefreshAsync);
            ResetFilterCommand = new DelegateCommand(Reset);
            AddCommand = new DelegateCommand(Add);
            AddManyCommand = new DelegateCommand(AddMany);
            EditCommand = new DelegateCommand<ServiceRequestViewItem>(Edit, x => x != null && AllowOpen);
            JoinRequestsCommand = new AsyncCommand(JoinRequestsAsync, () => SelectedServiceRequest?.Group == null && SelectedServiceRequest != null);
            SplitRequestsCommand = new AsyncCommand(SplitRequestsAsync, () => SelectedServiceRequest?.Group != null && SelectedServiceRequest != null);
            PrintCommand = new AsyncCommand<ServiceRequestViewItem>(PrintAsync, x => x != null);
            CreateScanSheetCommand = new AsyncCommand(CreateScanSheetAsync);
            SelectProductCommand = new DelegateCommand(SelectProduct);
            CancelProductCommand = new DelegateCommand(() => Filter.Product = null);
            SetCustomerCommand = new AsyncCommand(SetCustomerAsync, () => SelectedServiceRequest != null && AllowSetCustomer);

            Subdivisions = Dictionaries.GetItems<Subdivision>().ToReadOnlyObservableCollection();
            RequirementTypes = Dictionaries.GetItems<ServiceRequestRequirement>().ToReadOnlyObservableCollection();
            ResolutionTypes = Dictionaries.GetItems<ServiceRequestResolution>().ToReadOnlyObservableCollection();

            Filter = new ServiceRequestsFilterViewModel(webClient, dictionaries);

            Messenger.Register<ServiceRequestMessage>(this, OnServiceRequestMessage);

            AllowCreate = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestCreate);
            AllowSetCustomer = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestSetCustomer);
            AllowOpen = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestGetOne);
        }

        public ServiceRequestsViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand AddManyCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand JoinRequestsCommand { get; }

        public IAsyncCommand SetCustomerCommand { get; }

        public IAsyncCommand SplitRequestsCommand { get; }

        public IAsyncCommand PrintCommand { get; }

        public IAsyncCommand CreateScanSheetCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand ResetFilterCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        public IDelegateCommand CancelProductCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> ActiveEmployees
        {
            get { return GetProperty(() => ActiveEmployees); }
            private set { SetProperty(() => ActiveEmployees, value); }
        }

        public IReadOnlyCollection<ContractorDto> AllContractors
        {
            get { return GetProperty(() => AllContractors); }
            private set { SetProperty(() => AllContractors, value); }
        }

        public IReadOnlyCollection<EmployeeDto> AllEmployees
        {
            get { return GetProperty(() => AllEmployees); }
            private set { SetProperty(() => AllEmployees, value); }
        }

        public ReadOnlyObservableCollection<CityDto> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<ServiceRequestDiscussionState> ServiceRequestDiscussionStates
        {
            get { return GetProperty(() => ServiceRequestDiscussionStates); }
            private set { SetProperty(() => ServiceRequestDiscussionStates, value); }
        }

        public ServiceRequestsFilterViewModel Filter
        {
            get { return GetProperty(() => Filter); }
            private set { SetProperty(() => Filter, value); }
        }

        public bool IsSearchPanelOpened
        {
            get { return GetProperty(() => IsSearchPanelOpened); }
            set { SetProperty(() => IsSearchPanelOpened, value); }
        }

        public ServiceRequestViewItem SelectedServiceRequest
        {
            get { return GetProperty(() => SelectedServiceRequest); }
            set { SetProperty(() => SelectedServiceRequest, value, () => RaisePropertiesChanged(nameof(ToolsEnabled))); }
        }

        public ObservableRangeCollection<ServiceRequestViewItem> ServiceRequests
        {
            get { return GetProperty(() => ServiceRequests); }
            private set { SetProperty(() => ServiceRequests, value); }
        }

        public ReadOnlyObservableCollection<ServiceRequestState> States
        {
            get { return GetProperty(() => States); }
            private set { SetProperty(() => States, value); }
        }

        public ReadOnlyObservableCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public ReadOnlyObservableCollection<ServiceRequestRequirement> RequirementTypes
        {
            get { return GetProperty(() => RequirementTypes); }
            private set { SetProperty(() => RequirementTypes, value); }
        }

        public ReadOnlyObservableCollection<ServiceRequestResolution> ResolutionTypes
        {
            get { return GetProperty(() => ResolutionTypes); }
            private set { SetProperty(() => ResolutionTypes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Groups
        {
            get { return GetProperty(() => Groups); }
            private set { SetProperty(() => Groups, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> RejectReasons
        {
            get { return GetProperty(() => RejectReasons); }
            private set { SetProperty(() => RejectReasons, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        #endregion

        public bool AllowCreate { get; }

        public bool AllowSetCustomer { get; }

        public bool AllowOpen { get; }

        public bool ToolsEnabled => SelectedServiceRequest != null;

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IServiceRequestPrinter ServiceRequestPrinter { get; }

        private ILockableOperationProcessorFactory LockableOperationProcessorFactory { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDialogService WizardDialogService => GetService<IDialogService>("WizardDialogService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsSearchPanelOpened = !IsSearchPanelOpened;
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Add:
                        AddCommand.Execute(null);
                        handled = true;
                        break;
                    case HotkeyMessageType.Refresh:
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;
                    case HotkeyMessageType.Edit:
                        EditCommand.Execute(SelectedServiceRequest);
                        handled = true;
                        break;
                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            IsSearchPanelOpened = true;
            RefreshCommand.Execute(null);
            await base.HandleLoadedAsync();
        }

        private void Add()
        {
            Messenger.Send(new ServiceRequestCreateViewMessage());
        }

        private void AddMany()
        {
            Messenger.Send(new ServiceRequestCreateManyViewMessage());
        }

        private void Edit(ServiceRequestViewItem viewItem)
        {
            Messenger.Send(new ServiceRequestViewMessage(viewItem.Id));
        }

        private bool ShowValidationResultView(string title, IEnumerable<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }

        private async Task SplitRequestsAsync()
        {
            try
            {
                bool confirm = MessageFacadeService.Confirm("Вы действительно хотите разорвать объединение для данной заявки?");

                if (confirm is false)
                {
                    return;
                }

                Result<ServiceRequestDto> result = await WebClient.ExecuteApiRequestAsync(new SplitServiceRequestGroup(SelectedServiceRequest.Id));
                MessageFacadeService.ShowNotificationInfo("Заявка успешно разъеденена");

                await RefreshAsync();

                SelectedServiceRequest = ServiceRequests.FirstOrDefault(x => x.Id == result.Data.Id);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при разъединении заявок");
                ShowValidationResultView("Ошибка при разъединении заявок", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to split service requests");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при разъединении заявок");
                Logger.LogError(exception, "Error while splitting service requests");
            }
        }

        private async Task JoinRequestsAsync()
        {
            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                    new GetTextFromUserParameter("Номер сервисной заявки", "Введите номер заявки", "^[0-9]{1,9}$", "Не валидное значение. "), this);

            bool isNumber = int.TryParse(viewModel.Content, out int toId);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (!isNumber)
            {
                MessageFacadeService.ShowMessageBoxError("Должно быть указано число");
                return;
            }

            if (toId == SelectedServiceRequest.Id)
            {
                MessageFacadeService.ShowNotificationError("Нельзя указывать собственный id");
                return;
            }

            try
            {
                Result<ServiceRequestDto> result = await WebClient.ExecuteApiRequestAsync(new JoinServiceRequestGroup(SelectedServiceRequest.Id, toId));
                MessageFacadeService.ShowNotificationInfo("Заявка успешно обьеденена");

                await RefreshAsync();

                SelectedServiceRequest = ServiceRequests.FirstOrDefault(x => x.Id == result.Data.Id);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при объединении заявок");
                ShowValidationResultView("Ошибка при объединении заявок", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to join service requests");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при разъединении заявок");
                Logger.LogError(exception, "Error while joining service requests");
            }
        }

        private void OnServiceRequestMessage(ServiceRequestMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    {
                        ServiceRequestViewItem viewItem = Mapper.Map<ServiceRequestViewItem>(message.Entity);
                        ServiceRequests.Insert(0, viewItem);
                        break;
                    }

                case MessageType.Changed:
                    {
                        ServiceRequests.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                        break;
                    }

                default:
                    {
                        Debug.WriteLine($"Unknown order message type {message.MessageType}");
                        break;
                    }
            }
        }

        private Task PrintAsync(ServiceRequestViewItem serviceRequest)
        {
            return ServiceRequestPrinter.PrintAsync(serviceRequest);
        }

        private async Task CreateScanSheetAsync()
        {
            List<DeliveryDto> warehouseDeliveries = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseDeliveries(), true);

            CreateScanSheetForEntitiesModel model = new CreateScanSheetForEntitiesModel(Entity.ServiceRequestId);

            WizardDialogViewModel<CreateScanSheetForEntitiesModel> wizardDialogViewModel = new WizardDialogViewModel<CreateScanSheetForEntitiesModel>(
                typeof(SearchCriteriaPageViewModel),
                model,
                this);

            WizardDialogService.ShowDialog(MessageButton.OKCancel, "Создание реестра", wizardDialogViewModel);
        }

        private async Task RefreshAsync()
        {
            try
            {
                RejectReasons = Dictionaries.GetItems<ServiceRequestRejectReason>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
                States = Dictionaries.GetItems<ServiceRequestState>().OrderBy(x => x.Position).ToReadOnlyObservableCollection();
                ServiceRequestDiscussionStates = Dictionaries.GetItems<ServiceRequestDiscussionState>().ToReadOnlyObservableCollection();

                await Task.WhenAll(
                    RefreshContractorsAsync(),
                    RefreshCitiesAsync(),
                    RefreshEmployeesAsync());

                await Filter.RefreshAsync();

                IFilteringItem filter = Filter.GetFilteringItem();

                PagedResult<ServiceRequestDto> serviceRequests = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequests(filter));

                ServiceRequests = serviceRequests.Data.Select(x => Mapper.Map<ServiceRequestViewItem>(x)).ToObservableRangeCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to load service requests");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync().ConfigureAwait(false);
            Cities = cities.OrderBy(x => x.Position).ThenBy(x => x.Name).ToReadOnlyObservableCollection();
        }

        private async Task RefreshContractorsAsync()
        {
            AllContractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
        }

        private Task SetCustomerAsync()
        {
            return LockableOperationProcessorFactory.Create<ServiceRequestDto>().DoActionAsync(SelectedServiceRequest.Id, SetCustomer);

            void SetCustomer(ServiceRequestDto serviceRequest)
            {
                DialogDocumentManagerService.ShowView<SetCustomerViewModel>(new SetCustomerParameter("Привязка клиента к сервисной заявке", SetCustomerAsync), this);
            }
        }

        private async Task<bool> SetCustomerAsync(CustomerDto customer)
        {
            bool success = false;

            DelayedConfirmViewModel viewModel = DialogDocumentManagerService
                .ShowView<DelayedConfirmViewModel>($"Вы уверены что хотите привязать СЗ №{SelectedServiceRequest.Id} к клиенту \"{customer.Fio}\"?", this);

            if (viewModel.IsOk)
            {
                try
                {
                    Result<ServiceRequestDto> result = await WebClient.ExecuteApiRequestAsync(new ServiceRequestSetCustomer(SelectedServiceRequest.Id, customer.Id));

                    if (result.Warnings.Any())
                    {
                        IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                            .Warnings
                            .Select(x => new ValidationResultItem(x, false))
                            .ToList();

                        const string message = "Клиент привязан с ошибками";

                        MessageFacadeService.ShowValidationResultView(message, validationResultItems, this);

                        MessageFacadeService.ShowNotificationWarning(message);
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationInfo("Клиент успешно привязан");
                    }

                    success = true;
                }
                catch (UnexpectedSatusException exception)
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при привязке клиента");
                    MessageFacadeService.ShowValidationResultView("Ошибки при привязке клиента", exception.GetErrorItems(), this);
                }
                catch (UnexpectedErrorException exception)
                {
                    Logger.LogError(exception, "Failed to set customer to service request");
                    MessageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, this);
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при привязке клиента");
                    Logger.LogError(exception, "Error while seting customer to service request");
                }
            }

            return success;
        }

        private async Task RefreshEmployeesAsync()
        {
            AllEmployees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync().ConfigureAwait(false);

            ActiveEmployees = AllEmployees
                .Where(x => x.Active)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private void Reset()
        {
            Filter.Reset();
            RefreshCommand.Execute(null);
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();

                Filter.Product = product.Name;
            }
        }
    }
}