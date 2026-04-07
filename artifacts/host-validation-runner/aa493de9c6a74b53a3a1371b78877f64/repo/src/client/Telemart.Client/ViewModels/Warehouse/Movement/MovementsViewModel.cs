using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.XtraReports;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Business.Delivery.TrackNumberProviders;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Movement;
using Telemart.Client.Data.Requests.Features.Movement.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.ReportDesigner;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.MovementReport;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public sealed class MovementsViewModel : TelemartViewModelBase, ISupportHotkeys, IDataErrorInfo
    {
        private IReadOnlyCollection<EmployeeDto> employeesList;
        private IReadOnlyCollection<WarehouseDto> warehousesList;

        private LockableOperationProcessor<MovementDto> lockableOperationProcessor;

        public MovementsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory,
            IMapper mapper,
            IMessenger messenger,
            IPrintingSettingsStore printingSettingsStore,
            IMediator mediator,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Messenger = messenger;
            Mediator = mediator;
            ErrorHandler = errorHandler;
            LockableOperationProcessorFactory = lockableOperationProcessorFactory;
            PrintingSettingsStore = printingSettingsStore;

            AddCommand = new AsyncCommand(AddAsync);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand<MovementViewItem>(Edit, x => x != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            EditMovementLogisticsCommand = new AsyncCommand<MovementViewItem>(EditMovementLogisticsAsync, x => x != null);
            PrintTtnCommand = new AsyncCommand(PrintTttAsync, () => CurrentItem is not null && (CurrentItem.CarryId == CarryType.NpDeliveryId || CurrentItem.CarryId == CarryType.NpWarehouseId || CurrentItem.CarryId == CarryType.TeksId) && !string.IsNullOrWhiteSpace(CurrentItem.TrackNumber));
            PrintCommand = new AsyncCommand(PrintAsync, () => CurrentItem is not null);
            PrintInvoiceTtnCommand = new AsyncCommand(PrintInvoiceTttAsync, () => CurrentItem?.CarryId == CarryType.TeksId && !string.IsNullOrEmpty(CurrentItem?.TrackNumber));
            PrintSendingCommand = new AsyncCommand(PrintSendingAsync, () => CurrentItem is not null && (CurrentItem.StateId == MovementState.Arrived.Id || CurrentItem.StateId == MovementState.Left.Id || CurrentItem.StateId == MovementState.Received.Id));
            MassScanCommand = new AsyncCommand(MassScanAsync);
            TrackWaybillCommand = new AsyncCommand<MovementViewItem>(TrackWaybillAsync, CanTrackWaybill);

            Messenger.Register<MovementMessage>(this, OnMovementMessage);

            AllWarehouses = new ObservableRangeCollection<ComboBoxItem>();
            Employees = new ObservableRangeCollection<ComboBoxItem>();
            FromWarehouses = new ObservableRangeCollection<ComboBoxItem>();
            States = new ObservableRangeCollection<ComboBoxItem>();
            ToWarehouses = new ObservableRangeCollection<ComboBoxItem>();

            States.AddRange(Dictionaries.GetItems<MovementState>().Select(x => new ComboBoxItem(x.Id, x.Name)));
        }

        public MovementsViewModel(IErrorHandler errorHandler)
        {
            ErrorHandler = errorHandler;
        }

        #region Commands

        public IAsyncCommand AddCommand { get; }

        public IAsyncCommand PrintTtnCommand { get; }

        public IAsyncCommand PrintInvoiceTtnCommand { get; }

        public IAsyncCommand PrintSendingCommand { get; }

        public IAsyncCommand PrintCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand EditMovementLogisticsCommand { get; }

        public IAsyncCommand MassScanCommand { get; }

        public IAsyncCommand TrackWaybillCommand { get; }

        #endregion

        #region Collections

        public ObservableRangeCollection<ComboBoxItem> AllWarehouses
        {
            get { return GetProperty(() => AllWarehouses); }
            set { SetProperty(() => AllWarehouses, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> FromWarehouses
        {
            get { return GetProperty(() => FromWarehouses); }
            set { SetProperty(() => FromWarehouses, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> States
        {
            get { return GetProperty(() => States); }
            set { SetProperty(() => States, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> ToWarehouses
        {
            get { return GetProperty(() => ToWarehouses); }
            set { SetProperty(() => ToWarehouses, value); }
        }

        #endregion

        #region INPC

        public MovementViewItem CurrentItem
        {
            get { return GetProperty(() => CurrentItem); }
            set { SetProperty(() => CurrentItem, value); }
        }

        public DateTime? DateInAfter
        {
            get { return GetProperty(() => DateInAfter); }
            set { SetProperty(() => DateInAfter, value); }
        }

        public DateTime? DateInBefore
        {
            get { return GetProperty(() => DateInBefore); }
            set { SetProperty(() => DateInBefore, value); }
        }

        public DateTime? DateOutAfter
        {
            get { return GetProperty(() => DateOutAfter); }
            set { SetProperty(() => DateOutAfter, value); }
        }

        public DateTime? DateOutBefore
        {
            get { return GetProperty(() => DateOutBefore); }
            set { SetProperty(() => DateOutBefore, value); }
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

        public ObservableCollection<MovementViewItem> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        public string MovementNumbers
        {
            get { return GetProperty(() => MovementNumbers); }
            set { SetProperty(() => MovementNumbers, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedFromWarehouses
        {
            get { return GetProperty(() => SelectedFromWarehouses); }
            set { SetProperty(() => SelectedFromWarehouses, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedStates
        {
            get { return GetProperty(() => SelectedStates); }
            set { SetProperty(() => SelectedStates, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedToWarehouses
        {
            get { return GetProperty(() => SelectedToWarehouses); }
            set { SetProperty(() => SelectedToWarehouses, value); }
        }

        public string TrackNumber
        {
            get { return GetProperty(() => TrackNumber); }
            set { SetProperty(() => TrackNumber, value); }
        }

        #endregion

        string IDataErrorInfo.Error => string.Empty;

        private ILockableOperationProcessorFactory LockableOperationProcessorFactory { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private LockableOperationProcessor<MovementDto> LockableOperationProcessor
        {
            get
            {
                return lockableOperationProcessor ??= LockableOperationProcessorFactory.Create<MovementDto>();
            }
        }

        private IErrorHandler ErrorHandler { get; }

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IMediator Mediator { get; }

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<MovementsViewModel> builder)
        {
            builder.Property(x => x.MovementNumbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

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
                    case HotkeyMessageType.Add:
                        AddCommand.Execute(null);
                        handled = true;
                        break;
                    case HotkeyMessageType.Edit:
                        EditCommand.Execute(CurrentItem);
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
            if (Items != null)
            {
                return;
            }

            IsSearchPanelClosed = true;

            await Task.WhenAll(
                RefreshWarehousesAsync(),
                RefreshEmployeesAsync());

            ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        protected override void OnInitializeInDesignMode()
        {
            Items = new ObservableCollection<MovementViewItem>();
        }

        private Task AddAsync()
        {
            CreateMovementViewModel viewModel = DialogDocumentManagerService.ShowView<CreateMovementViewModel>(null, this);

            if (viewModel.IsOk)
            {
                OnMovementMessage(new MovementMessage(viewModel.CreatedMovement, MessageType.Added));
            }

            return Task.CompletedTask;
        }

        private void CancelFiltering()
        {
            ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void Edit(MovementViewItem viewItem)
        {
            Messenger.Send(new MovementViewMessage(viewItem.Id));
        }

        private MovementFilteringItem GetFilteringItem()
        {
            MovementFilteringItem item = new MovementFilteringItem();

            item.MovementNumbers = MovementNumbers;
            item.DateInBefore = DateInBefore;
            item.DateInAfter = DateInAfter;
            item.DateOutBefore = DateOutBefore;
            item.DateOutAfter = DateOutAfter;
            item.FromWarehouses = SelectedFromWarehouses.Select(x => x.Id).ToList();
            item.ToWarehouses = SelectedToWarehouses.Select(x => x.Id).ToList();
            item.States = SelectedStates.Select(x => x.Id).ToList();
            item.Ttn = TrackNumber;

            return item;
        }

        private void OnMovementMessage(MovementMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    {
                        Items.Insert(0, Mapper.Map(message.Entity, new MovementViewItem()));
                        break;
                    }

                case MessageType.Changed:
                    {
                        Items.DoActionWithItem(x => x.Id == message.Entity.Id, x => Mapper.Map(message.Entity, x));
                        break;
                    }

                default:
                    {
                        Debug.WriteLine($"Unknown movement message type {message.MessageType}");
                        break;
                    }
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                Items = null;

                await Task.WhenAll(RefreshWarehousesAsync(), RefreshEmployeesAsync());

                PagedResult<MovementDto> movements = await WebClient.ExecuteApiRequestAsync(new QueryMovements(GetFilteringItem()));
                IEnumerable<MovementViewItem> viewItems = movements.Data.Select(x => Mapper.Map(x, new MovementViewItem()));
                Items = new ObservableCollection<MovementViewItem>(viewItems);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(employees, employeesList))
            {
                return;
            }

            Employees.Clear();
            employeesList = employees;
            Employees.AddRange(employeesList.Select(x => new ComboBoxItem(x.Id, x.Name)));
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(warehouses, warehousesList))
            {
                return;
            }

            warehousesList = warehouses;

            AllWarehouses.Clear();
            FromWarehouses.Clear();
            ToWarehouses.Clear();

            List<ComboBoxItem> allowedWarehouses = warehousesList
                .Where(x => x.Active == 1 && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id))
                .OrderByDescending(x => x.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToList();

            AllWarehouses.AddRange(warehousesList.Select(x => new ComboBoxItem(x.Id, x.Name)));
            FromWarehouses.AddRange(allowedWarehouses);
            ToWarehouses.AddRange(allowedWarehouses);
        }

        private async Task PrintAsync()
        {
            try
            {
                PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();
                PrinterSettingsInfo printerSettings = printingSettings.Main;

                MovementReportDto reportDto = await WebClient.ExecuteApiRequestAsync(new QueryMovementReportData(CurrentItem.Id));

                IReadOnlyCollection<MovementProductGroupReportData> categories = reportDto.Groups
                    .OrderByDescending(x => x.Products.Any(y => y.TypeId == ProductType.AssemblyServiceId))
                    .Select(x => new MovementProductGroupReportData(
                        x.Name,
                        x.Products
                            .OrderByDescending(y => y.TypeId == ProductType.AssemblyServiceId)
                            .Select(Mapper.Map<MovementProductReportData>)
                            .ToArray()))
                    .ToArray();

                MovementReportData reportData = new MovementReportData(
                    reportDto.Id,
                    reportDto.StateId,
                    reportDto.CreatedOn,
                    reportDto.WarehouseFromName,
                    reportDto.WarehouseToName,
                    DateTime.Now,
                    categories);

                IReport report = new MovementReport { DataSource = new[] { reportData } };

                PrintReportRequest printReportRequest = new PrintReportRequest(
                    report,
                    true,
                    printerSettings?.Name,
                    printerSettings?.PaperSource);

                await Mediator.Send(printReportRequest);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при формировании отчета");
                MessageFacadeService.ShowValidationResultView("Ошибки при формировании отчета", exception.GetErrorItems(), this);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create report");
                MessageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, this);
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при формировании отчета");
                Logger.LogError(exception, "Error while creating report");
            }
        }

        private async Task MassScanAsync()
        {
            MovementFilteringItem filteringItem = new MovementFilteringItem()
            {
                States = new List<int>()
                {
                    MovementState.Arrived.Id
                }
            };

            PagedResult<MovementDto> arrivedMovements = await WebClient.ExecuteApiRequestAsync(new QueryMovements(filteringItem));

            MovementDto[] movementsToScan = arrivedMovements.Data
                .Where(x => x.EmployeeLockId is null)
                    .GroupBy(x => x.WarehouseToId)
                    .Where(x => x.Count() > 1)
                    .SelectMany(x => x)
                    .ToArray();

            if (!movementsToScan.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нет прибывших перемещений\nдля массового сканирования");
                return;
            }

            SelectMovementsParameter selectMovementsParameter = new SelectMovementsParameter(movementsToScan, "Выберите перемещения для массовой сверки");

            SelectMovementsViewModel selectMovementsViewModel = SizeableDialogDocumentManagerService.ShowView<SelectMovementsViewModel>(selectMovementsParameter, this);

            if (!selectMovementsViewModel.IsOk)
            {
                return;
            }

            int[] selectedMovementIds = selectMovementsViewModel.SelectedMovements.Select(x => x.Id).ToArray();

            bool errorOccuredWhileLockingMovements = false;

            foreach (int selectedMovementId in selectedMovementIds)
            {
                LockResponse<MovementDto> lockResponse = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new LockMovement(selectedMovementId)),
                    "блокировке перемещения",
                    null,
                    this,
                    true);

                if (lockResponse?.Success != true)
                {
                    errorOccuredWhileLockingMovements = true;
                    continue;
                }

                Messenger.Send(new MovementMessage(lockResponse.Dto, MessageType.Changed));
            }

            if (!errorOccuredWhileLockingMovements)
            {
                MovementsMassScanParameter movementsMassScanParameter = new MovementsMassScanParameter(movementsToScan.Where(x => selectedMovementIds.Contains(x.Id)).ToArray());

                SizeableDialogDocumentManagerService.ShowView<MovementsMassScanViewModel>(movementsMassScanParameter, this);
            }

            foreach (int selectedMovementId in selectedMovementIds)
            {
                LockResponse<MovementDto> unlockResponse = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new UnlockMovement(selectedMovementId)),
                    "разблокировке перемещения",
                    null,
                    this,
                    true);

                if (unlockResponse?.Success == true)
                {
                    Messenger.Send(new MovementMessage(unlockResponse.Dto, MessageType.Changed));
                }
            }
        }

        private async Task PrintSendingAsync()
        {
            const int maxLenghtWarehouseName = 55;

            Dictionary<int, string> warehouseNames = warehousesList.ToDictionary(x => x.Id, x => string.IsNullOrEmpty(x.NameUkr) ? x.Name : x.NameUkr);

            if (CurrentItem.Places.HasValue == false)
            {
                MessageFacadeService.ShowNotificationInfo("В выбранном перемещении отсутствует количество мест");
                return;
            }

            MovementPlaceReportData[] reportDatas = Enumerable.Range(1, CurrentItem.Places.Value)
                .Select(x => new MovementPlaceReportData(x, CurrentItem.Places.Value, CurrentItem.Id, warehouseNames[CurrentItem.WarehouseFromId].CutString(maxLenghtWarehouseName, true), warehouseNames[CurrentItem.WarehouseToId].CutString(maxLenghtWarehouseName, true)))
                .ToArray();

            PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();
            PrinterSettingsInfo printerSettings = printingSettings.Sticker;

            IReport report = new MovementPlaceReport { DataSource = reportDatas };

            PrintReportRequest printReportRequest = new PrintReportRequest(
                report,
                true,
                printerSettings?.Name,
                printerSettings?.PaperSource);

            await Mediator.Send(printReportRequest);
        }

        private void ResetFilterValues()
        {
            DateInBefore = null;
            DateInAfter = null;
            DateOutBefore = null;
            DateOutAfter = null;
            MovementNumbers = string.Empty;
            SelectedFromWarehouses = new ObservableCollection<ComboBoxItem>();
            SelectedToWarehouses = new ObservableCollection<ComboBoxItem>();
            SelectedStates = new ObservableCollection<ComboBoxItem>
            {
                new ComboBoxItem(MovementState.New.Id, MovementState.New.Name),
                new ComboBoxItem(MovementState.Left.Id, MovementState.Left.Name),
                new ComboBoxItem(MovementState.Arrived.Id, MovementState.Arrived.Name)
            };
        }

        private Task EditMovementLogisticsAsync(MovementViewItem viewItem)
        {
            return LockableOperationProcessor.DoActionAsync(
                viewItem.Id,
                x => DialogDocumentManagerService.ShowView<EditMovementLogisticsViewModel>(viewItem.Id, this),
                false);
        }

        private async Task PrintTttAsync()
        {
            ITrackNumberProvider trackNumberProvider = CurrentItem.Carry.GetTrackNumberProvider();

            await trackNumberProvider.PrintAsync(CurrentItem.TrackNumber, true);
        }

        private async Task PrintInvoiceTttAsync()
        {
            ITrackNumberProvider trackNumberProvider = CurrentItem.Carry.GetTrackNumberProvider();

            if (CurrentItem.Carry.Id == CarryType.TeksId && trackNumberProvider is TeksTrackNumberProvider provider)
            {
                provider.InvoiceTtn = true;
            }

            await trackNumberProvider.PrintAsync(CurrentItem.TrackNumber, true);
        }

        private bool CanTrackWaybill(MovementViewItem item)
        {
            return !string.IsNullOrWhiteSpace(item?.TrackNumber);
        }

        private async Task TrackWaybillAsync(MovementViewItem item)
        {
            ITrackNumberProvider provider = item.Carry.GetTrackNumberProvider();

            if(item.Carry.IsNovaposhta() && provider is NpTrackNumberProvider trackProvider)
            {
                trackProvider.Phone = warehousesList.FirstOrDefault(x => x.Id == item.WarehouseToId)?.Phone;
            }

            await provider.TrackAsync(item.Id, item.TrackNumber);
        }
    }
}