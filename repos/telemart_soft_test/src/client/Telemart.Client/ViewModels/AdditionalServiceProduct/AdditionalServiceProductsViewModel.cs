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
using Telemart.Client.Business;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.AdditionalServiceProduct
{
    public class AdditionalServiceProductsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public AdditionalServiceProductsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IErrorHandler errorHandler,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMediator mediator,
            IPrintingSettingsStore printingSettingsStore,
            IMessenger messenger,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory,
            ICallHelper callHelper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
            Mapper = mapper;
            Messenger = messenger;
            PrintingSettingsStore = printingSettingsStore;
            Mediator = mediator;
            CallHelper = callHelper;

            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand(Edit, () => SelectedProduct != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            CreateDefectCallCommand = new AsyncCommand(CreateDefectCallAsync, () => CanDefect);
            DefectCommand = new AsyncCommand(DefectAsync, () => CanDefect);
            DisagreeCommand = new AsyncCommand(DisagreeAsync, () => SelectedProduct != null && SelectedProduct.OrderStateId == OrderStatus.Confirmed.Id);
            EditEmployeeCommand = new AsyncCommand(EditEmployeeAsync, () => SelectedProduct != null && SelectedProduct.EmployeeLockId == null);
            PrintBarcodeCommand = new AsyncCommand(PrintBarcodeAsync, () => SelectedProduct != null);
            EditDateCommand = new AsyncCommand(EditDateAsync, () => SelectedProduct != null && (SelectedProduct.OrderStateId == OrderStatus.Received.Id || SelectedProduct.OrderStateId == OrderStatus.Confirmed.Id) && SelectedProduct.StateId != AdditionalServiceProductState.CompletedId && SelectedProduct.EmployeeLockId == null);
            EditWarehouseCommand = new AsyncCommand(EditWarehouseAsync, () => SelectedProduct != null && SelectedProduct.OrderStateId == OrderStatus.Received.Id && SelectedProduct.StateId != AdditionalServiceProductState.CompletedId && SelectedProduct.EmployeeLockId == null);

            Messenger.Register<EntityMessage<AdditionalServiceProductDto>>(this, OnAdditionalServiceProductMessage);

            Filter = new AdditionalServiceProductsFilterViewModel();
            LockableOperationProcessor = lockableOperationProcessorFactory.Create<OrderDto>();

            IsEditDateVisible = WebClient.IsOperationAllowed(BusinessOperation.AdditionalServiceProductUpdateDate);
            IsEditWarehouseVisible = WebClient.IsOperationAllowed(BusinessOperation.AdditionalServiceProductUpdateWarehouse);
            IsEditEmployeeVisible = WebClient.IsOperationAllowed(BusinessOperation.AdditionalServiceProductUpdateEmployee);
        }

        public AdditionalServiceProductsViewModel(IErrorHandler errorHandler)
        {
            ErrorHandler = errorHandler;
        }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IAsyncCommand CreateDefectCallCommand { get; }

        public IAsyncCommand DefectCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand PrintBarcodeCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand DisagreeCommand { get; }

        public IAsyncCommand EditEmployeeCommand { get; }

        public IAsyncCommand EditDateCommand { get; }

        public IAsyncCommand EditWarehouseCommand { get; }

        #region Properties

        public AdditionalServiceProductsViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value, () => RaisePropertyChanged(nameof(CanDefect))); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> AllWarehouses
        {
            get { return GetProperty(() => AllWarehouses); }
            private set { SetProperty(() => AllWarehouses, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ActiveEmployees
        {
            get { return GetProperty(() => ActiveEmployees); }
            private set { SetProperty(() => ActiveEmployees, value); }
        }

        public ReadOnlyObservableCollection<AdditionalServiceProductState> States
        {
            get { return GetProperty(() => States); }
            set { SetProperty(() => States, value); }
        }

        public ReadOnlyObservableCollection<OrderStatus> OrderStates
        {
            get { return GetProperty(() => OrderStates); }
            set { SetProperty(() => OrderStates, value); }
        }

        public ObservableRangeCollection<AdditionalServiceProductsViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public AdditionalServiceProductsFilterViewModel Filter
        {
            get { return GetProperty(() => Filter); }
            private set { SetProperty(() => Filter, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public bool IsEditDateVisible
        {
            get { return GetProperty(() => IsEditDateVisible); }
            set { SetProperty(() => IsEditDateVisible, value); }
        }

        public bool IsEditWarehouseVisible
        {
            get { return GetProperty(() => IsEditWarehouseVisible); }
            set { SetProperty(() => IsEditWarehouseVisible, value); }
        }

        public bool IsEditEmployeeVisible
        {
            get { return GetProperty(() => IsEditEmployeeVisible); }
            set { SetProperty(() => IsEditEmployeeVisible, value); }
        }

        public bool CanDefect => SelectedProduct != null && SelectedProduct.StateId == AdditionalServiceProductState.Warehouse.Id;

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IMediator Mediator { get; }

        private IErrorHandler ErrorHandler { get; }

        private ICallHelper CallHelper { get; }

        private LockableOperationProcessor<OrderDto> LockableOperationProcessor { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;
            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsSearchPanelClosed = !IsSearchPanelClosed;
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Refresh:
                        RefreshCommand.Execute(null);
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
            }

            return handled;
        }

        protected override void OnInitializeInDesignMode()
        {
            Products = new ObservableRangeCollection<AdditionalServiceProductsViewItem>
            {
                new AdditionalServiceProductsViewItem { Date = DateTime.Now.AddMinutes(-30) },
                new AdditionalServiceProductsViewItem { Date = DateTime.Now.AddMinutes(30) },
                new AdditionalServiceProductsViewItem { Date = DateTime.Now.AddMinutes(30) },
                new AdditionalServiceProductsViewItem { Date = DateTime.Now.AddMinutes(60) },
                new AdditionalServiceProductsViewItem()
            };
        }

        protected override async Task HandleLoadedAsync()
        {
            States = Dictionaries
                .GetItems<AdditionalServiceProductState>()
                .ToReadOnlyObservableCollection();

            OrderStates = Dictionaries
            .GetItems<OrderStatus>()
            .ToReadOnlyObservableCollection();

            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

            Warehouses = warehouses.Data
                .Where(x => x.Active == 1 && (x.TypeId == WarehouseKind.Main.Id || x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.Assembly.Id || x.TypeId == WarehouseKind.Service.Id))
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            AllWarehouses = warehouses.Data
                .ToReadOnlyObservableCollection();

            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                .ToReadOnlyObservableCollection();

            ActiveEmployees = Employees.Where(x => x.Active).ToReadOnlyObservableCollection();

            await RefreshAsync();

            await base.HandleLoadedAsync();
        }

        private void CancelFiltering()
        {
            Filter.Reset();
            RefreshCommand.Execute(null);
        }

        private void Edit()
        {
            Messenger.Send(new AdditionalServiceProductViewMessage(SelectedProduct.Id));
        }

        private async Task RefreshAsync()
        {
            PagedResult<AdditionalServiceProductDto> products = await WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceProducts(Filter.GetFilteringItem()));

            Products = products.Data
                .OrderBy(x => x.OrderDeliveryTimeTo)
                .Select(x => Mapper.Map<AdditionalServiceProductsViewItem>(x))
                .ToObservableRangeCollection();
        }

        private async Task CreateDefectCallAsync()
        {
            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(SelectedProduct.OrderId));

            if (order.StateId != OrderStatus.Received.Id)
            {
                MessageFacadeService.ShowNotificationWarning("Заказ должен быть в статусе \"Принят\"");
                return;
            }

            await CallHelper.CreateCallByOrderAsync(
                order,
                CallTypeIds.Defect,
                Priority.Normal,
                "Создание звонка по дефекту",
                string.Empty,
                this);
        }

        private async Task DefectAsync()
        {
            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(SelectedProduct.OrderId));

            if (order.StateId != OrderStatus.Received.Id)
            {
                MessageFacadeService.ShowNotificationWarning("Заказ должен быть в статусе \"Принят\"");
                return;
            }

            AdditionalServiceProductDto additionalServiceProduct = await WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceProduct(SelectedProduct.Id));

            if (additionalServiceProduct.StateId != AdditionalServiceProductState.Warehouse.Id)
            {
                MessageFacadeService.ShowNotificationWarning("Оказание услуги должно быть в статусе \"На складе\"");
                return;
            }

            if (!SelectedProduct.Scanned)
            {
                MessageFacadeService.ShowNotificationWarning("Товар должен быть просканирован");
                return;
            }

            DialogDocumentManagerService.ShowView<AdditionalServiceProductDefectViewModel>(new AdditionalServiceProductDefectParameter(SelectedProduct.Id, SelectedProduct.ProductName, SelectedProduct.SerialNumber, SelectedProduct.KeepSerial, SelectedProduct.AdditionalServiceId, SelectedProduct.WarehouseId, SelectedProduct.OrderId), this);
        }

        private async Task PrintBarcodeAsync()
        {
            PrintAdditionalServiceBarcodeReportRequest request;

            if (SelectedProduct.PrimaryAdditionalServiceProductId.HasValue)
            {
                request = new PrintAdditionalServiceBarcodeReportRequest(
                    SelectedProduct.Id,
                    SelectedProduct.OrderId,
                    null,
                    SelectedProduct.Date);
            }
            else
            {
                request = new PrintAdditionalServiceBarcodeReportRequest(
                    SelectedProduct.Id,
                    SelectedProduct.OrderId,
                    SelectedProduct.OrderDeliveryTimeTo,
                    SelectedProduct.CompletedOn);
            }

            await Mediator.Send(request);
        }

        private async Task DisagreeAsync()
        {
            await LockableOperationProcessor.DoActionAsync(SelectedProduct.OrderId, ReconfirmOrderInternal, false);

            void ReconfirmOrderInternal(OrderDto dto)
            {
                DialogDocumentManagerService.ShowView<OrderReconfirmViewModel>(new OrderReconfirmParameter(dto.Id), this);
            }

            await RefreshAsync();
        }

        private async Task EditEmployeeAsync()
        {
            string currentEmployeeName = Employees.FirstOrDefault(x => x.Id == SelectedProduct.EmployeeId).DisplayValue;

            ChangeItemViewModel viewModel = DialogDocumentManagerService
               .ShowView<ChangeItemViewModel>(new ChangeItemParameter(Employees.Where(x => x.Active).ToReadOnlyObservableCollection(), currentEmployeeName, "Изменение ответственного услуги"), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                Result<AdditionalServiceProductDto> result =
                await WebClient.ExecuteApiRequestAsync(new UpdateAdditionalServiceProductEmployee(SelectedProduct.Id, new UpdateAdditionalServiceProductEmployee.UpdateAdditionalServiceProductEmployeeDto(SelectedProduct.Id, viewModel.NewItem.Value.Id)));

                Messenger.Send(new EntityMessage<AdditionalServiceProductDto>(result.Data, MessageType.Changed));

                MessageFacadeService.ShowNotificationInfo("Ответственный услуги успешно изменен");
            }
            catch
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении ответственного");
            }
        }

        private async Task EditWarehouseAsync()
        {
            const string ErrorText = "Ошибка при изменении склада оказания услуги";

            ReadOnlyObservableCollection<ComboBoxItem> additionalServiceWarehouses = AllWarehouses
                .Where(x => x.Active == 1 && x.WarehousePerformances.Any(x => x.AdditionalServiceId == SelectedProduct.AdditionalServiceId))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            string currentWarehouseName = AllWarehouses.FirstOrDefault(x => x.Id == SelectedProduct.WarehouseId)?.Name;

            ChangeItemViewModel viewModel = DialogDocumentManagerService
                .ShowView<ChangeItemViewModel>(new ChangeItemParameter(additionalServiceWarehouses, currentWarehouseName, "Изменение склада оказания услуги"), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                Result<AdditionalServiceProductDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateAdditionalServiceProductWarehouse(SelectedProduct.Id, viewModel.NewItem.Value.Id));

                Messenger.Send(new EntityMessage<AdditionalServiceProductDto>(result.Data, MessageType.Changed));

                MessageFacadeService.ShowNotificationInfo("Склад услуги успешно изменен");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(ErrorText);
                MessageFacadeService.ShowValidationResultView(ErrorText, exception.GetErrorItems(), this);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to edit additional service product date");
                MessageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, this);
            }
            catch
            {
                MessageFacadeService.ShowNotificationError(ErrorText);
            }
        }

        private async Task EditDateAsync()
        {
            const string ErrorText = "Ошибка при изменении дедлайна услуги";

            DialogResult<ChangeDateTimeResult> changeDateTimeResult = DialogDocumentManagerService.ShowView<ChangeDateTimeViewModel, ChangeDateTimeParameter, ChangeDateTimeResult>(
               new ChangeDateTimeParameter(
                   SelectedProduct.Date,
                   "Изменение дедлайна услуги",
                   x => x?.TimeOfDay < TimeSpan.FromHours(7) || x?.TimeOfDay > TimeSpan.FromHours(22)
                       ? "Выберите рабочее время"
                       : string.Empty,
                   SelectedProduct.OrderStateId,
                   true,
                   false),
               this);

            if (!changeDateTimeResult.IsOk)
            {
                return;
            }

            try
            {
                Result<AdditionalServiceProductDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateAdditionalServiceProductDate(
                    SelectedProduct.Id,
                    changeDateTimeResult.Result.NewDate,
                    changeDateTimeResult.Result.OrderStateChangeReasonId,
                    changeDateTimeResult.Result.Comment));

                Messenger.Send(new EntityMessage<AdditionalServiceProductDto>(result.Data, MessageType.Changed));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    string message = "Дата дедлайна услуги изменена с предупреждениями";

                    MessageFacadeService.ShowNotificationWarning(message);

                    SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(new ValidationResultViewModelParameter(message, validationResultItems), this);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Дедлайн успешно изменен");
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(ErrorText);
                MessageFacadeService.ShowValidationResultView(ErrorText, exception.GetErrorItems(), this);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to edit additional service product date");
                MessageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, this);
            }
            catch
            {
                MessageFacadeService.ShowNotificationError(ErrorText);
            }
        }

        private void OnAdditionalServiceProductMessage(EntityMessage<AdditionalServiceProductDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:

                    AdditionalServiceProductsViewItem additionalService = Mapper.Map<AdditionalServiceProductsViewItem>(message.Entity);
                    Products.Add(additionalService);

                    break;
                case MessageType.Changed:

                    Products.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }
    }
}