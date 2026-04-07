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
using DevExpress.Xpf.Grid;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.Requests.Features.ServiceRepair;
using Telemart.Client.Data.Requests.Features.ServiceRepair.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Service.ServiceRepairs.FormInvoice;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRepairs
{
    public sealed class ServiceRepairsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private CustomComboBoxItem[] serviceCenterFilterItems;
        private CustomComboBoxItem[] serviceInvoiceFilterItems;

        public ServiceRepairsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            RefreshCommand = new AsyncCommand(RefreshAsync);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditServiceRequestCommand = new DelegateCommand<ServiceRepairViewItem>(EditServiceRequest);
            EditServiceInvoiceCommand = new DelegateCommand<ServiceRepairViewItem>(EditServiceInvoice);
            EditServiceRepairCommand = new DelegateCommand<ServiceRepairViewItem>(EditServiceRepair, x => x != null);
            FormInvoiceCommand = new DelegateCommand(FormInvoice);
            ShowFilterPopupHandlerCommand = new DelegateCommand<FilterPopupEventArgs>(ShowFilterPopupHandler);
            OnTableViewRowDoubleClickCommand = new DelegateCommand<RowDoubleClickEventArgs>(OnTableViewRowDoubleClick);
            ChangeStateCommand = new AsyncCommand<ServiceRepairViewItem>(ChangeStateAsync, x => x != null);

            LockableOperationProcessor = new Lazy<LockableOperationProcessor<ServiceRepairDto>>(lockableOperationProcessorFactory.Create<ServiceRepairDto>);

            Filter = new ServiceRepairsFilterViewModel(WebClient, Dictionaries);

            Messenger.Register<ServiceRepairMessage>(this, OnServiceRepairMessage);
        }

        public ServiceRepairsViewModel()
        {
        }

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand EditServiceRequestCommand { get; }

        public IDelegateCommand EditServiceInvoiceCommand { get; }

        public IDelegateCommand FormInvoiceCommand { get; }

        public IDelegateCommand EditServiceRepairCommand { get; }

        public IDelegateCommand ShowFilterPopupHandlerCommand { get; }

        public IDelegateCommand OnTableViewRowDoubleClickCommand { get; }

        public IAsyncCommand ChangeStateCommand { get; }

        #endregion

        #region INPC

        public ObservableRangeCollection<ServiceRepairViewItem> Repairs
        {
            get { return GetProperty(() => Repairs); }
            private set { SetProperty(() => Repairs, value); }
        }

        public ServiceRepairViewItem CurrentServiceRepair
        {
            get { return GetProperty(() => CurrentServiceRepair); }
            set { SetProperty(() => CurrentServiceRepair, value); }
        }

        public ReadOnlyObservableCollection<ServiceCenterDto> ServiceCenters
        {
            get { return GetProperty(() => ServiceCenters); }
            private set { SetProperty(() => ServiceCenters, value); }
        }

        public ReadOnlyObservableCollection<ServiceRequestLocation> ServiceRequestLocations
        {
            get { return GetProperty(() => ServiceRequestLocations); }
            private set { SetProperty(() => ServiceRequestLocations, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public ServiceRepairsFilterViewModel Filter
        {
            get { return GetProperty(() => Filter); }
            private set { SetProperty(() => Filter, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IDialogService WizardDialogService => GetService<IDialogService>("WizardDialogService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private Lazy<LockableOperationProcessor<ServiceRepairDto>> LockableOperationProcessor { get; }

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            if (hotkeyMessage.ModifierKeys == ModifierKeys.Alt)
            {
                switch (hotkeyMessage.Key)
                {
                    case Key.L:
                        IsSearchPanelClosed = !IsSearchPanelClosed;
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (hotkeyMessage.Key)
                {
                    case Key.F2:
                        {
                            EditServiceRepairCommand.Execute(CurrentServiceRepair);
                            handled = true;
                            break;
                        }

                    case Key.F5:
                        {
                            RefreshCommand.Execute(null);
                            handled = true;
                            break;
                        }

                    case Key.F6:
                        {
                            IsColumnChooserVisible = !IsColumnChooserVisible;
                            handled = true;
                            break;
                        }

                    case Key.F8:
                        if (ChangeStateCommand.CanExecute(CurrentServiceRepair))
                        {
                            ChangeStateCommand.Execute(CurrentServiceRepair);
                        }

                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override Task HandleLoadedAsync()
        {
            ServiceRequestLocations = Dictionaries.GetItems<ServiceRequestLocation>().ToReadOnlyObservableCollection();

            RefreshCommand.Execute(null);
            return Task.CompletedTask;
        }

        private async Task RefreshAsync()
        {
            const string LogMessage = "Failed to refresh service repairs";

            try
            {
                Repairs = null;

                await Filter.RefreshAsync();

                List<ServiceCenterDto> serviceCenters = await WebClient.ExecuteApiRequestAsync(new QueryServiceCenters(), true).GetPagedResultDataAsync();
                List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
                List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

                ServiceCenters = serviceCenters.Where(x => x.Active).OrderBy(x => x.Name).ToReadOnlyObservableCollection();
                Warehouses = warehouses
                    .OrderByDescending(x => x.Position)
                    .ToReadOnlyObservableCollection();
                Contractors = contractors
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();

                QueryServiceRepairs gatewayRequest = new QueryServiceRepairs(Filter.GetFilteringItem());

                PagedResult<ServiceRepairDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                serviceCenterFilterItems = GetServiceCenterFilterItems(result.Data, serviceCenters).ToArray();
                serviceInvoiceFilterItems = GetServiceInvoiceFilterItems(result.Data).ToArray();

                Repairs = result.Data
                    .Select(x => Mapper.Map<ServiceRepairViewItem>(x))
                    .OrderBy(x => x.DisplayProductName)
                    .ToObservableRangeCollection();
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, LogMessage);
                MessageFacadeService.ShowNotificationError(Resources.ServerConnectError);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, LogMessage);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private static IEnumerable<CustomComboBoxItem> GetServiceCenterFilterItems(
            IReadOnlyCollection<ServiceRepairDto> repairs,
            IReadOnlyCollection<ServiceCenterDto> serviceCenters)
        {
            HashSet<int> hashSet = new HashSet<int>(repairs.Where(x => x.ServiceCenterId.HasValue).Select(x => x.ServiceCenterId.Value));

            yield return new CustomComboBoxItem { DisplayValue = "(Не задано)", EditValue = string.Empty };

            IOrderedEnumerable<CustomComboBoxItem> comboBoxItems = serviceCenters
                .Where(x => hashSet.Contains(x.Id))
                .Select(x => new CustomComboBoxItem { DisplayValue = x.Name, EditValue = x.Name })
                .OrderBy(x => x.DisplayValue);

            foreach (CustomComboBoxItem item in comboBoxItems)
            {
                yield return item;
            }
        }

        private static IEnumerable<CustomComboBoxItem> GetServiceInvoiceFilterItems(IReadOnlyCollection<ServiceRepairDto> repairs)
        {
            yield return new CustomComboBoxItem { DisplayValue = "(Не задано)", EditValue = null };

            IOrderedEnumerable<CustomComboBoxItem> comboBoxItems = repairs.Where(x => x.ServiceInvoiceId.HasValue)
                .Select(x => x.ServiceInvoiceId.Value)
                .Distinct()
                .Select(x => new CustomComboBoxItem { DisplayValue = x, EditValue = x })
                .OrderBy(x => x.DisplayValue);

            foreach (CustomComboBoxItem item in comboBoxItems)
            {
                yield return item;
            }
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void EditServiceRepair(ServiceRepairViewItem repair)
        {
            if (repair != null)
            {
                Messenger.Send(new ServiceRepairViewMessage(repair.Id));
            }
        }

        private void EditServiceRequest(ServiceRepairViewItem repair)
        {
            if (repair != null)
            {
                Messenger.Send(new ServiceRequestViewMessage(repair.ServiceRequestId));
            }
        }

        private void EditServiceInvoice(ServiceRepairViewItem repair)
        {
            if (repair?.ServiceInvoiceId != null)
            {
                Messenger.Send(new ServiceInvoiceViewMessage(repair.ServiceInvoiceId.Value));
            }
        }

        private void FormInvoice()
        {
            if (Filter.SelectedWarehouseId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите склад");
                return;
            }

            FormInvoiceModel model = new FormInvoiceModel(Filter.SelectedWarehouseId.Value, Filter.SelectedServiceCenterId);

            WizardDialogViewModel<FormInvoiceModel> wizardDialogViewModel = new WizardDialogViewModel<FormInvoiceModel>(
                typeof(SearchRequestsViewModel),
                model,
                this);

            MessageResult res = WizardDialogService.ShowDialog(MessageButton.OKCancel, "Формирование серв. накладной", wizardDialogViewModel);

            if (res == MessageResult.OK)
            {
                RefreshCommand.Execute(null);
            }
        }

        private Task ChangeStateAsync(ServiceRepairViewItem item)
        {
            Func<ServiceRepairDto, Task> changeStateFunc;

            switch (item.State.Id)
            {
                case ServiceRepairState.NewId:
                    changeStateFunc = ConfirmInternalAsync;
                    break;
                case ServiceRepairState.ConfirmedId:
                    changeStateFunc = ReConfirmInternalAsync;
                    break;
                default:
                    MessageFacadeService.ShowNotificationError("Нельзя изменять статус ремонта, взятого в работу");
                    return Task.CompletedTask;
            }

            return LockableOperationProcessor.Value.DoOperationAsync(item.Id, changeStateFunc);

            async Task ConfirmInternalAsync(ServiceRepairDto dto)
            {
                if (!MessageFacadeService.Confirm($"Вы подтверждаете изменение статуса ремонта на \"{ServiceRepairState.Confirmed.Name}\"?"))
                {
                    return;
                }

                try
                {
                    Result<ServiceRepairDto> result = await WebClient.ExecuteApiRequestAsync(new ConfirmServiceRepair(dto.Id));

                    if (result.Warnings.Any())
                    {
                        IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                            .Warnings
                            .Select(x => new ValidationResultItem(x, false))
                            .ToList();

                        string message = "Ремонт согласован с ошибками";

                        MessageFacadeService.ShowNotificationWarning(message);

                        ShowValidationResultView(message, validationResultItems);
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationInfo("Ремонт успешно согласован");
                    }

                    Messenger.Send(new ServiceRepairMessage(result.Data, MessageType.Changed));
                }
                catch (UnexpectedSatusException exception)
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при согласовании ремонта");
                    ShowValidationResultView("Ошибки при согласовании ремонта", exception.GetErrorItems());
                }
                catch (UnexpectedErrorException exception)
                {
                    Logger.LogError(exception, "Failed to confirm service repair");
                    ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при согласовании ремонта");
                    Logger.LogError(exception, "Error while confirmation service repair");
                }
            }

            async Task ReConfirmInternalAsync(ServiceRepairDto dto)
            {
                if (!MessageFacadeService.Confirm($"Вы подтверждаете изменение статуса ремонта на \"{ServiceRepairState.New.Name}\"?"))
                {
                    return;
                }

                try
                {
                    Result<ServiceRepairDto> result = await WebClient.ExecuteApiRequestAsync(new ReconfirmServiceRepair(dto.Id));

                    if (result.Warnings.Any())
                    {
                        IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                            .Warnings
                            .Select(x => new ValidationResultItem(x, false))
                            .ToList();

                        string message = "Ремонт рассогласован с ошибками";

                        MessageFacadeService.ShowNotificationWarning(message);

                        ShowValidationResultView(message, validationResultItems);
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationInfo("Ремонт успешно рассогласован");
                    }

                    Messenger.Send(new ServiceRepairMessage(result.Data, MessageType.Changed));
                }
                catch (UnexpectedSatusException exception)
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при рассогласовании ремонта");
                    ShowValidationResultView("Ошибки при рассогласовании ремонта", exception.GetErrorItems());
                }
                catch (UnexpectedErrorException exception)
                {
                    Logger.LogError(exception, "Failed to reconfirm service repair");
                    ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при рассогласовании ремонта");
                    Logger.LogError(exception, "Error while reconfirmation service repair");
                }
            }
        }

        private void OnTableViewRowDoubleClick(RowDoubleClickEventArgs args)
        {
            ServiceRepairViewItem item = (ServiceRepairViewItem)((GridControl)args.Source.DataControl).CurrentItem;

            switch (args.HitInfo.Column.FieldName)
            {
                case nameof(item.ServiceRequestId):
                    EditServiceRequestCommand.Execute(item);
                    break;
                case nameof(item.ServiceInvoiceId):
                    EditServiceInvoiceCommand.Execute(item);
                    break;
                default:
                    EditServiceRepairCommand.Execute(item);
                    break;
            }
        }

        private void OnServiceRepairMessage(ServiceRepairMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:
                    {
                        Repairs.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                        break;
                    }

                default:
                    {
                        Debug.WriteLine($"Unknown order message type {message.MessageType}");
                        break;
                    }
            }
        }

        private void ShowFilterPopupHandler(FilterPopupEventArgs e)
        {
            switch (e.Column.FieldName)
            {
                case nameof(ServiceRepairViewItem.ServiceCenterId):
                    e.ComboBoxEdit.ItemsSource = serviceCenterFilterItems;
                    break;
                case nameof(ServiceRepairViewItem.ServiceInvoiceId):
                    e.ComboBoxEdit.ItemsSource = serviceInvoiceFilterItems;
                    break;
            }
        }

        private void ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);
        }
    }
}