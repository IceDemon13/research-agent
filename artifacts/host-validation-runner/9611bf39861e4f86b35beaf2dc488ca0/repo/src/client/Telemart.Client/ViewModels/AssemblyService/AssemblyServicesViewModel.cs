using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.XtraReports;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.ReportFactory;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.AssemblyService.Actions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.PrintReport;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.ReportDesigner.Order;
using Telemart.Client.Reports.AssemblyService;
using Telemart.Client.Reports.Order.Assembly;
using Telemart.Client.Reports.Product;
using Telemart.Client.Reports.ReportBuilders.AssemblyService.PassportReport;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Assembly;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Constants = Telemart.Client.Common.Constants;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public class AssemblyServicesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public AssemblyServicesViewModel(
           IWebClient webClient,
           IDictionaries dictionaries,
           IMessageFacadeService messageFacadeService,
           IMapper mapper,
           IMediator mediator,
           IPrintingSettingsStore printingSettingsStore,
           IAssemblyServicePassportReportPrinter passportReportPrinter,
           IMessenger messenger,
           ILockableOperationProcessorFactory lockableOperationProcessorFactory,
           IBarcodeReportFactory barcodeReportFactory,
           IErrorHandler errorHandler,
           ICallHelper callHelper)
           : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Messenger = messenger;
            Mediator = mediator;
            PrintingSettingsStore = printingSettingsStore;
            PassportReportPrinter = passportReportPrinter;
            BarcodeReportFactory = barcodeReportFactory;
            ErrorHandler = errorHandler;
            CallHelper = callHelper;

            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand(Edit, () => SelectedAssembly != null);
            DefectCommand = new DelegateCommand(Defect, CanDefect);
            CreateDefectCallCommand = new AsyncCommand(CreateDefectCallAsync, CanDefect);

            EditAssemblyDateCommand = new AsyncCommand(EditAssemblyDateAsync, () => SelectedAssembly != null && (SelectedAssembly.OrderStateId == OrderStatus.Received.Id || SelectedAssembly.OrderStateId == OrderStatus.Confirmed.Id) && (SelectedAssembly.StateId == AssemblyServiceState.Waiting.Id || SelectedAssembly.StateId == AssemblyServiceState.Warehouse.Id) && SelectedAssembly.EmployeeLockId == null);
            EditAssemblyPlacesCommand = new AsyncCommand(EditAssemblyPlacesAsync);
            EditEmployeeCommand = new AsyncCommand(EditEmployeeAsync, () => SelectedAssembly != null && SelectedAssembly?.StateId != AssemblyServiceState.Assembled.Id && SelectedAssembly?.EmployeeLockId == null);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            DisagreeCommand = new AsyncCommand(DisagreeAsync, () => SelectedAssembly != null);
            PrintAssemblyCommand = new AsyncCommand(PrintAssemblyAsync, () => SelectedAssembly != null && SelectedAssembly.Places.HasValue);
            PrintBarcodeMovementCommand = new AsyncCommand(PrintBarcodeMovementAsync, () => SelectedAssembly != null && ((SelectedAssembly.Places.HasValue && SelectedAssembly.CompletedOn.HasValue) || SelectedAssembly.ParentAssemblyServiceId.HasValue));
            PrintOurBarcodeCommand = new AsyncCommand(PrintOurBarcodeAsync, () => SelectedAssembly != null && SelectedAssembly.ProductId.HasValue && SelectedAssembly.StateId == AssemblyServiceState.Completed.Id);
            PrintAssemblyPassportCommand = new AsyncCommand(PrintAssemblyServicePassportAsync, () => SelectedAssembly != null && SelectedAssembly.StateId == AssemblyServiceState.Completed.Id && SelectedAssembly.ProductId.HasValue);
            PrintAssemblySheetCommand = new AsyncCommand(PrintAssemblySheetAsync, () => SelectedAssembly != null);

            PrintB2BBarcodeCommand = new AsyncCommand(PrintB2BBarcodeAsync, () => SelectedAssembly != null && SelectedAssembly.EmployeeLockId == null && SelectedAssembly.ProductId.HasValue && SelectedAssembly.StateId == AssemblyServiceState.Completed.Id);
            PrintB2BSerialCommand = new AsyncCommand(PrintB2BSerialAsync, () => SelectedAssembly != null && SelectedAssembly.EmployeeLockId == null && SelectedAssembly.ProductId.HasValue && SelectedAssembly.StateId == AssemblyServiceState.Completed.Id);

            DeleteCommand = new AsyncCommand(DeleteAsync, CanDelete);
            SelectAssemblyServiceProductCommand = new DelegateCommand(SelectAssemblyServiceProduct);
            SelectProductCommand = new DelegateCommand(SelectProduct);

            Filter = new AssemblyServicesFilterViewModel();
            LockableOperationProcessor = lockableOperationProcessorFactory.Create<OrderDto>();

            Messenger.Register<AssemblyServiceMessage>(this, OnAssemblyServicesMessage);

            AllowPrintAssemblyReport = WebClient.IsOperationAllowed(BusinessOperation.AssemblyServicePrint);
            AllowEditAssemblyPlaces = WebClient.IsOperationAllowed(BusinessOperation.AssemblyServicePlacesEdit);

            Assemblies = new ObservableRangeCollection<AssemblyServicesViewItem>();
        }

        public AssemblyServicesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand EditEmployeeCommand { get; }

        public IDelegateCommand DefectCommand { get; }

        public IAsyncCommand CreateDefectCallCommand { get; }

        public IAsyncCommand EditAssemblyDateCommand { get; }

        public IAsyncCommand EditAssemblyPlacesCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand PrintAssemblyCommand { get; }

        public IAsyncCommand PrintAssemblySheetCommand { get; }

        public IAsyncCommand PrintAssemblyPassportCommand { get; }

        public IAsyncCommand PrintB2BBarcodeCommand { get; }

        public IAsyncCommand PrintB2BSerialCommand { get; }

        public IAsyncCommand PrintBarcodeMovementCommand { get; }

        public IAsyncCommand PrintOurBarcodeCommand { get; }

        public IAsyncCommand DisagreeCommand { get; }

        public IAsyncCommand DeleteCommand { get; }

        public IDelegateCommand SelectAssemblyServiceProductCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        #endregion

        public ObservableRangeCollection<AssemblyServicesViewItem> Assemblies
        {
            get { return GetProperty(() => Assemblies); }
            set { SetProperty(() => Assemblies, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<AssemblyServiceState> States
        {
            get { return GetProperty(() => States); }
            set { SetProperty(() => States, value); }
        }

        public ReadOnlyObservableCollection<OrderStatus> OrderStates
        {
            get { return GetProperty(() => OrderStates); }
            set { SetProperty(() => OrderStates, value); }
        }

        public AssemblyServicesViewItem SelectedAssembly
        {
            get { return GetProperty(() => SelectedAssembly); }
            set { SetProperty(() => SelectedAssembly, value, () => RaisePropertyChanged(nameof(IsDefectEnabled))); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public AssemblyServicesFilterViewModel Filter
        {
            get { return GetProperty(() => Filter); }
            private set { SetProperty(() => Filter, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public bool AllowPrintAssemblyReport
        {
            get { return GetProperty(() => AllowPrintAssemblyReport); }
            private set { SetProperty(() => AllowPrintAssemblyReport, value); }
        }

        public bool AllowEditAssemblyPlaces
        {
            get { return GetProperty(() => AllowEditAssemblyPlaces); }
            private set { SetProperty(() => AllowEditAssemblyPlaces, value); }
        }

        public bool IsDefectEnabled => CanDefect();

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IMapper Mapper { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IMediator Mediator { get; }

        private IMessenger Messenger { get; }

        private IAssemblyServicePassportReportPrinter PassportReportPrinter { get; }

        private LockableOperationProcessor<OrderDto> LockableOperationProcessor { get; }

        private IBarcodeReportFactory BarcodeReportFactory { get; }

        private IErrorHandler ErrorHandler { get; }

        private ICallHelper CallHelper { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

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

        protected override Task HandleLoadedAsync()
        {
            IsSearchPanelClosed = false;

            States = Dictionaries
                .GetItems<AssemblyServiceState>()
                .ToReadOnlyObservableCollection();

            OrderStates = Dictionaries
               .GetItems<OrderStatus>()
               .ToReadOnlyObservableCollection();

            CancelFilteringCommand.Execute(null);

            return Task.CompletedTask;
        }

        private bool CanDefect()
        {
            return SelectedAssembly?.OrderStateId == OrderStatus.Received.Id
            && SelectedAssembly.StateId != AssemblyServiceState.Completed.Id;
        }

        private bool CanDelete()
        {
            return SelectedAssembly?.OrderStateId == OrderStatus.Received.Id
                && (SelectedAssembly.StateId == AssemblyServiceState.Warehouse.Id || SelectedAssembly.StateId == AssemblyServiceState.Waiting.Id);
        }

        private void Edit()
        {
            Messenger.Send(new AssemblyServiceViewMessage(SelectedAssembly.Id));
        }

        private void Defect()
        {
            DialogDocumentManagerService.ShowView<AssemblyServiceDefectViewModel>(new AssemblyServiceDefectParameter(SelectedAssembly.Id), this);
        }

        private async Task CreateDefectCallAsync()
        {
            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(SelectedAssembly.OrderId));

            await CallHelper.CreateCallByOrderAsync(
                order,
                CallTypeIds.Defect,
                Priority.Normal,
                "Создание звонка по дефекту",
                string.Empty,
                this);
        }

        private async Task EditEmployeeAsync()
        {
            GetEmployeeFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetEmployeeFromUserViewModel>(new GetEmployeeFromUserParameter(SelectedAssembly.Id, SelectedAssembly.EmployeeId, "Выберите ответственного"), this);

            if (!viewModel.IsOk || viewModel.EmployeeId == SelectedAssembly.EmployeeId)
            {
                return;
            }

            try
            {
                Result<AssemblyServiceDto> result =
                await WebClient.ExecuteApiRequestAsync(new UpdateAssemblyServiceEmployee(SelectedAssembly.Id, new UpdateAssemblyServiceEmployee.UpdateAssemblyServiceEmployeeDto(SelectedAssembly.Id, viewModel.EmployeeId)));
                Messenger.Send(new AssemblyServiceMessage(result.Data, MessageType.Changed));
            }
            catch
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении ответственного сборки");
            }
        }

        private async Task EditAssemblyDateAsync()
        {
            const string ErrorText = "Ошибка при изменении даты сборки";

            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(SelectedAssembly.OrderId));

            DialogResult<ChangeDateTimeResult> changeDateTimeResult = DialogDocumentManagerService.ShowView<ChangeDateTimeViewModel, ChangeDateTimeParameter, ChangeDateTimeResult>(
                new ChangeDateTimeParameter(
                    SelectedAssembly.AssemblyDate,
                    "Выберите дату сборки",
                    null,
                    order.StateId,
                    false,
                    true),
                this);

            if (!changeDateTimeResult.IsOk)
            {
                return;
            }

            try
            {
                Result<AssemblyServiceDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateAssemblyServiceDate(
                    SelectedAssembly.Id,
                    changeDateTimeResult.Result.NewDate,
                    changeDateTimeResult.Result.OrderStateChangeReasonId,
                    changeDateTimeResult.Result.Comment));

                Messenger.Send(new AssemblyServiceMessage(result.Data, MessageType.Changed));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    string message = "Дата сборки изменена с предупреждениями";

                    MessageFacadeService.ShowNotificationWarning(message);

                    ShowValidationResultView(message, validationResultItems);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Дата сборки успешно изменена");
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(ErrorText);
                ShowValidationResultView(ErrorText, exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to edit assembly date");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to edit assembly date");
                MessageFacadeService.ShowNotificationError(ErrorText);
            }
        }

        private async Task EditAssemblyPlacesAsync()
        {
            GetTextFromUserParameter parameter =
                new GetTextFromUserParameter(
                    "Количество мест",
                    "Выберите количество мест",
                    "^[1-9]{1}$",
                    "Допустимый диапазон мест 1...9",
                    (SelectedAssembly.Places ?? 1).ToString());

            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(parameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (int.TryParse(viewModel.Content, out int placesCount))
            {
                Result<AssemblyServiceDto> placesResult = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new UpdateAssemblyServicePlaces(SelectedAssembly.Id, placesCount)),
                    "обновлении количества мест",
                    "Количество мест изменено",
                    this,
                    true,
                    showDialog: true);

                if (placesResult?.IsSuccess == true)
                {
                    Messenger.Send(new AssemblyServiceMessage(placesResult.Data, MessageType.Changed));
                }
            }
        }

        private async Task DeleteAsync()
        {
            if (!MessageFacadeService.Confirm("Вы действительно хотите удалить сборку?"))
            {
                return;
            }

            try
            {
                Result<object> result = await WebClient.ExecuteApiRequestAsync(new DeleteAssemblyService(SelectedAssembly.Id));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    string message = "Сборка удалена с предупреждениями";

                    ShowValidationResultView(message, validationResultItems);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Сборка успешно удалена");
                }

                Assemblies.Remove(SelectedAssembly);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении сборки");
                ShowValidationResultView("Ошибки при удалении сборки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to delete assembly service");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to delete assembly service");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении сборки");
            }
        }

        private void CancelFiltering()
        {
            Filter.Reset();
            RefreshCommand.Execute(null);
        }

        private async Task RefreshAsync()
        {
            try
            {
                PagedResult<AssemblyServiceDto> assemblies = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyServices(Filter.GetFilteringItem()));

                Assemblies.Clear();

                Assemblies.AddRange(assemblies.Data
                     .OrderBy(x => x.AssemblyDate)
                     .ThenBy(x => x.OrderDeliveryTimeTo)
                     .Select(x => Mapper.Map<AssemblyServicesViewItem>(x)));

                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                Employees = employees
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();

                PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

                Warehouses = warehouses.Data
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }
            catch
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task PrintAssemblyServicePassportAsync()
        {
            await PassportReportPrinter.PrintAsync(new AssemblyServicePassportReportPrinterData(SelectedAssembly.Id, true));
        }

        private async Task PrintAssemblySheetAsync()
        {
            PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

            AssemblySheetReportDataDto reportDto = await WebClient.ExecuteApiRequestAsync(new QueryAssemblySheetReport(SelectedAssembly.Id));

            OrderSingleAssemblyReportData reportData = Mapper.Map<OrderSingleAssemblyReportData>(reportDto);

            IReadOnlyCollection<AdditionalServiceProductDto> additionalServiceProducts = SelectedAssembly.AdditionalServices;

            int[] parentOrderProductIds = additionalServiceProducts.Where(x => x.ParentOrderProductId.HasValue).Select(x => x.ParentOrderProductId.Value).ToArray();

            reportData.AssemblyServiceProducts = reportDto.AssemblyServiceProducts
                .Where(x => x.OrderProductId == null || parentOrderProductIds.Contains(x.OrderProductId.Value) != true)
                .Select(x => Mapper.Map<OrderAssemblyProductReportData>(x)).ToArray();

            reportData.SetDateTimeAssemblyPrint(DateTime.Now);

            if (additionalServiceProducts?.Any() == true)
            {
                List<AssemblyAdditionalServiceProductData> assemblyAdditionalServiceProducts = new List<AssemblyAdditionalServiceProductData>();

                foreach (AdditionalServiceProductDto additionalServiceProduct in additionalServiceProducts)
                {
                    string consumableProducts = additionalServiceProduct.ConsumableProducts?.Any() == true
                        ? string.Join($",{Environment.NewLine}", additionalServiceProduct.ConsumableProducts.Select(x => x.ProductName))
                        : string.Empty;

                    assemblyAdditionalServiceProducts.Add(
                        new AssemblyAdditionalServiceProductData(
                            additionalServiceProduct.Id,
                            string.IsNullOrEmpty(consumableProducts) ? additionalServiceProduct.AdditionalServiceNameRu : $"{additionalServiceProduct.AdditionalServiceNameRu}{Environment.NewLine}Товар для оказания услуги: {consumableProducts}",
                            additionalServiceProduct.ProductName,
                            Dictionaries.GetItemById<AdditionalServiceProductState>(additionalServiceProduct.PriorityTypeId).Name,
                            Dictionaries.GetItemById<AdditionalServicePriorityType>(additionalServiceProduct.PriorityTypeId).Name,
                            OrderCommentHelper.GetJoinedComment(additionalServiceProduct.CustomerComment, additionalServiceProduct.EmployeeComment, additionalServiceProduct.SystemComment)));
                }

                reportData.SetAssemblyAdditionalServiceProducts(assemblyAdditionalServiceProducts);
            }

            IReport report = new OrderSingleAssemblyReport { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = printSettings?.Main != null
                ? new PrintReportRequest(report, true, printSettings.Main.Name, printSettings.Main.PaperSource)
                : new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }

        private async Task PrintB2BBarcodeAsync()
        {
            BarcodeReportFactoryResult result = await BarcodeReportFactory.CreateAsync(BarcodeReportFormat.B2B, SelectedAssembly.ProductName, SelectedAssembly.ProductId.Value, 1);

            if (result.Printer == null)
            {
                MessageFacadeService.ShowNotificationError("Для печати \"ШК B2B\" нужно задать принтер 50x40 в настройках");
                return;
            }

            PrintReportRequest printReportRequest = new PrintReportRequest(
                result.Report,
                false,
                result.Printer.Name,
                result.Printer.PaperSource,
                2);

            await Mediator.Send(printReportRequest);
        }

        private async Task PrintB2BSerialAsync()
        {
            PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();

            PrinterSettingsInfo printerSettings = printingSettings.Barcode50X40;

            if (printerSettings == null)
            {
                MessageFacadeService.ShowNotificationError("Для печати \"SN B2B\" нужно задать принтер 50x40 в настройках");
                return;
            }

            IReport report = new B2BSerialNumberReport
            {
                DataSource = new List<SerialNumberReportData>
                {
                    new SerialNumberReportData(SelectedAssembly.NomenclatureSeries)
                }
            };

            PrintReportRequest request = new PrintReportRequest(
                report,
                false,
                printerSettings.Name,
                printerSettings.PaperSource);

            await Mediator.Send(request);
        }

        private async Task PrintBarcodeMovementAsync()
        {
            PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

            IReport report;

            if (SelectedAssembly.ProductId.HasValue)
            {
                AssembledComputerRuleMovementReportData[] reportsData = new AssembledComputerRuleMovementReportData[SelectedAssembly.Places!.Value];

                for (int i = 0; i < SelectedAssembly.Places; i++)
                {
                    reportsData[i] = new AssembledComputerRuleMovementReportData(
                    SelectedAssembly.OrderId,
                    SelectedAssembly.Id,
                    SelectedAssembly.Places.Value,
                    i + 1,
                    SelectedAssembly.Products.Count(x => x.ProductId != Constants.AssemblyServiceProductId),
                    SelectedAssembly.NomenclatureSeries,
                    SelectedAssembly.ProductName,
                    SelectedAssembly.ProductId.Value);
                }

                report = new AssembledComputerRuleMovementReport { DataSource = reportsData };
            }
            else
            {
                AssemblyServiceMovementReportData[] reportsData = new AssemblyServiceMovementReportData[SelectedAssembly.Places!.Value];

                for (int i = 0; i < SelectedAssembly.Places; i++)
                {
                    reportsData[i] = new AssemblyServiceMovementReportData(
                    SelectedAssembly.OrderId,
                    i + 1,
                    SelectedAssembly.Places.Value,
                    SelectedAssembly.Products.Count(x => x.ProductId != Constants.AssemblyServiceProductId),
                    SelectedAssembly.Id);
                }

                report = new AssemblyServiceMovementReport { DataSource = reportsData };
            }

            PrintReportRequest printRequest = printSettings?.Sticker != null
                    ? new PrintReportRequest(report, true, printSettings.Sticker.Name, printSettings.Sticker.PaperSource)
                    : new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }

        private async Task PrintOurBarcodeAsync()
        {
            const int quantity = 1;

            if (SelectedAssembly.ProductId.HasValue)
            {
                ProductCardDto productCard = await WebClient.ExecuteApiRequestAsync(new QueryProductCard(SelectedAssembly.ProductId.Value));

                BarcodeReportFactoryResult result = await BarcodeReportFactory.CreateAsync(productCard.NameUkr ?? productCard.Name, productCard.ProductId, quantity);

                if (result.Printer == null)
                {
                    MessageFacadeService.ShowNotificationError("Для печати \"Нашего ШК\" нужно задать принтер в настройках");
                    return;
                }

                PrintReportRequest printReportRequest = new PrintReportRequest(
                    result.Report,
                    false,
                    result.Printer.Name,
                    result.Printer.PaperSource);

                await Mediator.Send(printReportRequest);
            }
        }

        private async Task PrintAssemblyAsync()
        {
            try
            {
                AssemblyServiceDto assemblyService = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyService(SelectedAssembly.Id));

                List<AssemblyServiceBarcodeReportData> barcodeReportData = new List<AssemblyServiceBarcodeReportData>();

                foreach (AssemblyServiceProductDto product in assemblyService.Products)
                {
                    for (int i = 0; i < product.Quantity; i++)
                    {
                        barcodeReportData.Add(new AssemblyServiceBarcodeReportData(product.SerialNumbers.Skip(i).FirstOrDefault(), product.FullName, product.ProductId, product.Quantity));
                    }
                }

                AssemblyServiceReportData reportData = new AssemblyServiceReportData(WebClient.AuthenticatedEmployee.Name, SelectedAssembly.OrderId, SelectedAssembly.Id, barcodeReportData);

                IReport report = new AssemblySerialsReport { DataSource = new[] { reportData } };

                PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

                PrintReportRequest printRequest = printSettings?.Main != null
                    ? new PrintReportRequest(report, true, printSettings.Main.Name, printSettings.Main.PaperSource)
                    : new PrintReportRequest(report, true);

                await Mediator.Send(printRequest);
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private async Task DisagreeAsync()
        {
            await LockableOperationProcessor.DoActionAsync(SelectedAssembly.OrderId, ReconfirmOrderInternal, false);

            void ReconfirmOrderInternal(OrderDto dto)
            {
                DialogDocumentManagerService.ShowView<OrderReconfirmViewModel>(new OrderReconfirmParameter(dto.Id), this);
            }

            await RefreshAsync();
        }

        private void ShowValidationResultView(string title, IEnumerable<ValidationResultItem> validationItems)
        {
            SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(new ValidationResultViewModelParameter(title, validationItems), this);
        }

        private void OnAssemblyServicesMessage(AssemblyServiceMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Assemblies.Add(Mapper.Map<AssemblyServicesViewItem>(message.Entity));
                    break;

                case MessageType.Changed:
                    Assemblies.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }

        private void SelectAssemblyServiceProduct()
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

                Filter.AssemblyServiceProductName = product.Name;
                Filter.AssemblyServiceProductId = product.Id;
            }
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

                Filter.ProductName = product.Name;
                Filter.ProductId = product.Id;
            }
        }
    }
}