using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.Requests.Features.ServiceInvoice;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.Requests.Features.Warehouse.Delivery;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.Warehouse.Delivery;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Service.ServiceCenters;
using Telemart.Client.ViewModels.Store.CreateScanSheetForEntities;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Service.ServiceInvoices
{
    public sealed class ServiceInvoicesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public ServiceInvoicesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IMediator mediator)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mediator = mediator ?? throw new ArgumentNullException(nameof(messenger));

            RefreshCommand = new AsyncCommand(RefreshAsync);
            AddCommand = new DelegateCommand(AddServiceInvoice);
            EditCommand = new DelegateCommand<ServiceInvoiceViewItem>(EditServiceInvoice, x => x != null);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            PrintServiceInvoiceCommand = new AsyncCommand(PrintServiceInvoiceAsync);
            CreateScanSheetCommand = new AsyncCommand(CreateScanSheetAsync);

            Filter = new ServiceInvoicesFilterViewModel(WebClient, Dictionaries);

            Messenger.Register<ServiceInvoiceMessage>(this, OnServiceInvoiceMessage);
        }

        public ServiceInvoicesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IAsyncCommand PrintServiceInvoiceCommand { get; }

        public IAsyncCommand CreateScanSheetCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<ServiceInvoiceViewItem> ServiceInvoices
        {
            get { return GetProperty(() => ServiceInvoices); }
            private set { SetProperty(() => ServiceInvoices, value); }
        }

        public ServiceInvoiceViewItem CurrentServiceInvoice
        {
            get { return GetProperty(() => CurrentServiceInvoice); }
            set { SetProperty(() => CurrentServiceInvoice, value); }
        }

        public List<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public List<ServiceCenterViewItem> ServiceCenters
        {
            get { return GetProperty(() => ServiceCenters); }
            set { SetProperty(() => ServiceCenters, value); }
        }

        public List<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
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

        public ServiceInvoicesFilterViewModel Filter
        {
            get { return GetProperty(() => Filter); }
            private set { SetProperty(() => Filter, value); }
        }

        public bool IsAllowCreate => WebClient.AuthenticatedEmployee.HasAnyRole(Role.ServiceManager, Role.Admin);

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IMediator Mediator { get; }

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDialogService WizardDialogService => GetService<IDialogService>("WizardDialogService", ServiceSearchMode.PreferParents);

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

            switch (hotkeyMessage.Key)
            {
                case Key.Insert:
                    {
                        AddCommand.Execute(null);
                        handled = true;
                        break;
                    }

                case Key.F2:
                    {
                        EditCommand.Execute(CurrentServiceInvoice);
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
            }

            return handled;
        }

        protected override Task HandleLoadedAsync()
        {
            RefreshCommand.Execute(null);
            return Task.CompletedTask;
        }

        private async Task RefreshAsync()
        {
            try
            {
                ServiceInvoices?.Clear();

                Employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
                Warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
                await RefreshServiceCentersAsync();

                await Filter.RefreshAsync();

                ServiceInvoiceFilteringItem filteringItem = Filter.GetServiceInvoiceFilteringItem();

                PagedResult<ServiceInvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new QueryServiceInvoices(filteringItem));

                IEnumerable<ServiceInvoiceViewItem> serviceInvoices = result.Data.Select(x => Mapper.Map<ServiceInvoiceViewItem>(x));

                ServiceInvoices = new ObservableCollection<ServiceInvoiceViewItem>(serviceInvoices);
            }
            catch (UnexpectedSatusException exception)
            {
                Logger.LogError(exception, "Failed to refresh service invoices");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to refresh service invoices");
                MessageFacadeService.ShowNotificationError(Resources.ServerConnectError);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to refresh service invoices");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshServiceCentersAsync()
        {
            PagedResult<ServiceCenterDto> serviceCenters = await WebClient.ExecuteApiRequestAsync(new QueryServiceCenters(), true);
            ServiceCenters = Mapper.Map<List<ServiceCenterViewItem>>(serviceCenters.Data.Where(x => x.Active));
        }

        private void AddServiceInvoice()
        {
            CreateServiceInvoiceParameter parameter = new CreateServiceInvoiceParameter();
            CreateServiceInvoiceViewModel viewModel = DialogDocumentManagerService.ShowView<CreateServiceInvoiceViewModel>(parameter, this);
            HandleAdded(viewModel.ResultServiceInvoice);
        }

        private void HandleAdded(ServiceInvoiceDto newServiceInvoice)
        {
            if (newServiceInvoice == null)
            {
                return;
            }

            MessageFacadeService.ShowNotificationInfo($"Сервисная накладная №{newServiceInvoice.Id} успешно создана");
            ServiceInvoiceViewItem serviceInvoiceViewItem = Mapper.Map<ServiceInvoiceViewItem>(newServiceInvoice);
            ServiceInvoices.Insert(0, serviceInvoiceViewItem);
        }

        private void EditServiceInvoice(ServiceInvoiceViewItem viewItem)
        {
            Messenger.Send(new ServiceInvoiceViewMessage(viewItem.Id));
        }

        private async Task PrintServiceInvoiceAsync()
        {
            try
            {
                await Mediator.Send(new PrintServiceInvoiceReportRequest(CurrentServiceInvoice.Id));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при печати");
                ShowValidationResultView("Ошибки при печати", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to print");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при печати");
                Logger.LogError(exception, "Error while printing");
            }
        }

        private async Task CreateScanSheetAsync()
        {
            List<DeliveryDto> warehouseDeliveries = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseDeliveries(), true);

            CreateScanSheetForEntitiesModel model = new CreateScanSheetForEntitiesModel(Entity.ServiceInvoiceId);

            WizardDialogViewModel<CreateScanSheetForEntitiesModel> wizardDialogViewModel = new WizardDialogViewModel<CreateScanSheetForEntitiesModel>(
                typeof(SearchCriteriaPageViewModel),
                model,
                this);

            WizardDialogService.ShowDialog(MessageButton.OKCancel, "Создание реестра", wizardDialogViewModel);
        }

        private bool ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void OnServiceInvoiceMessage(ServiceInvoiceMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:
                    {
                        ServiceInvoices.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                        break;
                    }
            }
        }
    }
}