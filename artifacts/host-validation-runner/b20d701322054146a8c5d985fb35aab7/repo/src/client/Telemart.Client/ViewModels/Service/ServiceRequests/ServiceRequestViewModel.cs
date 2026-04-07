using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports;
using Humanizer;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Audit;
using Telemart.Client.Business.Audit.Entry;
using Telemart.Client.Business.Barcode;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Business.Order;
using Telemart.Client.Business.SmsTemplates;
using Telemart.Client.Business.SmsTemplates.ServiceRequest;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.AssembledComputer;
using Telemart.Client.Data.Requests.Features.Audit;
using Telemart.Client.Data.Requests.Features.Call.Actions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Complaint;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Crm;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Requests.Features.ServiceRequest.Actions;
using Telemart.Client.Data.Requests.Features.Subdivision;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.Reports.Refund;
using Telemart.Client.Reports.SerialNumber;
using Telemart.Client.Reports.ServiceRequest;
using Telemart.Client.Reports.TradeIn;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Complaint;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.AssemblyService;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Common.PrintBarcodeParameters;
using Telemart.Client.ViewModels.Complaint;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Dialogs.AddDocuments;
using Telemart.Client.ViewModels.Money.Refund;
using Telemart.Client.ViewModels.Service.ServiceRepairs;
using Telemart.Client.ViewModels.Service.ServiceRequests.Diagnostics;
using Telemart.Client.ViewModels.Store.Call;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    internal sealed class ServiceRequestViewModel : TelemartEditorExViewModelBase<ServiceRequestDto, ServiceRequestViewMessage, ServiceRequestViewItem>
    {
        private readonly bool canTake;
        private readonly bool canDiagnoze;
        private readonly bool canChangeRequirement;
        private readonly bool canComplete;
        private readonly bool canCompensate;
        private readonly bool canReopen;
        private readonly bool canReset;
        private readonly bool canAcceptConfirm;
        private readonly bool canDenyConfirm;
        private readonly TelegramBotOptions _telegramBotOptions;

        private WarehouseDto warehouseIn;

        public ServiceRequestViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IErrorHandler errorHandler,
            IMediator mediator,
            IPrintingSettingsStore printingSettingsStore,
            IOrderReportBuilder orderReportBuilder,
            DocumentCommands documentCommands,
            IServiceRequestPrinter serviceRequestPrinter,
            TelegramBotOptions telegramBotOptions,
            IRroPrintHelper proPrintHelper)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            PrintingSettingsStore = printingSettingsStore;
            Mediator = mediator;
            OrderReportBuilder = orderReportBuilder;
            DocumentCommands = documentCommands;
            ErrorHandler = errorHandler;
            RroPrintHelper = proPrintHelper;
            AuditEntryProcessorBuilder = new ServiceRequestAuditEntryProcessorBuilder(webClient, dictionaries);

            AddDocumentCommand = new DelegateCommand(AddDocument);
            RemoveDocumentCommand = new AsyncCommand<ServiceRequestDocumentSimpleDto>(RemoveDocumentAsync, x => x != null);
            PrintDocumentCommand = new AsyncCommand<ServiceRequestDocumentSimpleDto>(PrintDocumentAsync, x => x != null);
            OpenOrderCommand = new DelegateCommand<int?>(OpenOrder, x => x.HasValue);
            RefreshAuditEntriesCommand = new AsyncCommand(RefreshAuditEntriesAsync);
            RefreshDocumentsCommand = new AsyncCommand(RefreshDocumentsAsync);
            HandleSelectionChangedCommand = new DelegateCommand<ValueChangedEventArgs<FrameworkElement>>(HandleSelectionChanged);
            RefreshCallsCommand = new AsyncCommand(RefreshCallsAsync);
            RefreshRepairsCommand = new AsyncCommand(RefreshRepairsAsync);
            AddCallCommand = new DelegateCommand(AddCall);
            EditCallCommand = new DelegateCommand<CallViewItem>(EditCall, x => x != null);
            CallCommand = new DelegateCommand<CallViewItem>(Call, x => x != null);
            EditRepairCommand = new DelegateCommand<ServiceRepairViewItem>(EditRepair, x => x != null);
            TakeRequestCommand = new AsyncCommand(TakeRequestAsync, CanTakeRequest);
            CancelRequestCommand = new AsyncCommand(CancelRequestAsync, CanCancelRequest);
            CancelCallCommand = new AsyncCommand<CallViewItem>(CancelCallAsync, x => x != null);
            DiagnoseRequestCommand = new AsyncCommand(DiagnoseRequestAsync, CanDiagnoseRequest);
            CompleteRequestCommand = new AsyncCommand(CompleteRequestAsync, CanCompleteRequest);
            CompensateRequestCommand = new AsyncCommand(CompensateRequestAsync, CanCompensateRequest);
            ChangeRequirementCommand = new AsyncCommand(ChangeRequirementAsync, CanChangeRequirement);
            ReopenRequestCommand = new AsyncCommand(ReopenRequestAsync, CanReopen);
            ResetRequestCommand = new AsyncCommand(ResetRequestAsync, CanReset);
            SendSmsCommand = new AsyncCommand<string>(SendSmsAsync, CanSendSms);
            ShowMoneyRefundCommand = new DelegateCommand<int?>(ShowMoneyRefund, x => x.HasValue);
            RefreshDiscussionsCommand = new AsyncCommand(RefreshDiscussionsAsync);
            CloseDiscussionCommand = new AsyncCommand(CloseDiscussionAsync, CanCloseDiscussion);
            AddDiscussionCommand = new AsyncCommand<string>(AddDiscussionAsync, CanAddDiscussion);
            EditDiscussionCommand = new AsyncCommand<ServiceRequestDiscussionViewItem>(EditDiscussionAsync, x => x != null);
            RefreshCrmCommand = new AsyncCommand(RefreshCrmAsync);
            RecomplectCommand = new AsyncCommand(RecomplectAsync, () => Model is not null && Model.ReadyForRecomplectation == true);
            OpenRequirementOnHistoryCommand = new DelegateCommand(OpenRequirementOnHistory);
            ShowInvoiceCommand = new DelegateCommand<int?>(ShowInvoice, CanShowInvoice);
            PrintAktCommand = new AsyncCommand(PrintAktAsync);
            PrintSerialNumberCommand = new AsyncCommand(PrintSerialNumberAsync);
            PrintWarrantyCardCommand = new AsyncCommand(PrintWarrantyCardAsync);
            PrintDefectivenessActCommand = new AsyncCommand(PrintDefectivenessActAsync);
            PrintIssuanceCertificateCommand = new AsyncCommand(PrintIssuanceCertificateAsync);
            HandleCarryInChangedCommand = new DelegateCommand(HandleCarryInChanged);
            HandleCityChangedCommand = new AsyncCommand(HandleCityChangedAsync);
            PrintTrackNumberCommand = new AsyncCommand<string>(PrintTrackNumberAsync);
            ShowServiceProductCommand = new DelegateCommand<int?>(ShowServiceProduct, x => x.HasValue);
            DenyConfirmCommand = new AsyncCommand(DenyConfirmAsync, () => canDenyConfirm && Model != null && !Model.WarrantyRemoved && Model.ServiceRepairTypeId != ServiceRepairType.Paid.Id);
            AcceptConfirmCommand = new AsyncCommand(AcceptConfirmAsync, () => canAcceptConfirm);
            ChangeProductCommand = new AsyncCommand(ChangeProductAsync, () => Model != null && Model.Requirement != ServiceRequestRequirement.TradeIn);
            RefreshComplaintsCommand = new AsyncCommand(RefreshComplaintsAsync);
            AddComplaintCommand = new DelegateCommand(AddComplaint);
            ShowRequisitesCommand = new DelegateCommand(ShowRequisites, () => ShowRequisitesVisible);
            ShowDocumentsBotQrCommand = new DelegateCommand(ShowDocumentsBotQr);
            PrintChequeCommand = new AsyncCommand(PrintChequeAsync, () => !string.IsNullOrEmpty(Model?.FiscalId));
            SelectDeliveryAddressCommand = new DelegateCommand(SelectDeliveryAddress, () => Model != null && Model.CarryOut != null && Model.CarryOut.Id != CarryType.PickupId);


            PrintReturnReportCommand = new AsyncCommand(PrintReturnReportAsync);
            PrintReturnProtocolReportCommand = new AsyncCommand(PrintReturnProtocolReportAsync);

            ServiceRequestPrinter = serviceRequestPrinter;
            _telegramBotOptions = telegramBotOptions;

            Messenger.Register<CallMessage>(this, OnCallMessage);
            Messenger.Register<ServiceRequestDocumentMessage>(this, OnDocumentMessage);
            Messenger.Register<ServiceRepairMessage>(this, OnServiceRepairMessage);
            Messenger.Register<ServiceRepairWorkflowMessage>(this, OnServiceRepairWorkflowMessage);
            Messenger.Register<ComplaintMessage>(this, OnComplaintMessage);
            Messenger.Register<RefundUpdateRequisitesMessage>(this, RefundUpdateRequisites);

            canTake = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestTake);
            canDiagnoze = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestDiagnoze);
            canChangeRequirement = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestChangeRequirement);
            canComplete = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestComplete);
            canCompensate = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestCompensate);
            canReopen = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestReopen);
            canReset = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestReset);
            canAcceptConfirm = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestAcceptConfirm);
            canDenyConfirm = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestDenyConfirm);

            Messenger.Register<ServiceRequestMessage>(this, OnServiceRequestMessage);
        }

        public ServiceRequestViewModel()
        {
        }

        #region Commands

        public IDelegateCommand ShowDocumentsBotQrCommand { get; }

        public IDelegateCommand AddCallCommand { get; }

        public IAsyncCommand AddDiscussionCommand { get; }

        public IDelegateCommand AddDocumentCommand { get; }

        public IAsyncCommand CancelCallCommand { get; }

        public IAsyncCommand CancelRequestCommand { get; }

        public IAsyncCommand ChangeRequirementCommand { get; }

        public IAsyncCommand CloseDiscussionCommand { get; }

        public IAsyncCommand CompleteRequestCommand { get; }

        public IDelegateCommand ShowRequisitesCommand { get; }

        public IAsyncCommand CompensateRequestCommand { get; }

        public IDelegateCommand ShowMoneyRefundCommand { get; }

        public IAsyncCommand DiagnoseRequestCommand { get; }

        public IDelegateCommand EditCallCommand { get; }

        public IDelegateCommand CallCommand { get; }

        public IAsyncCommand EditDiscussionCommand { get; }

        public IDelegateCommand EditRepairCommand { get; }

        public IDelegateCommand HandleSelectionChangedCommand { get; }

        public IDelegateCommand OpenOrderCommand { get; }

        public IAsyncCommand PrintDocumentCommand { get; }

        public IAsyncCommand RefreshAuditEntriesCommand { get; }

        public IAsyncCommand RefreshCallsCommand { get; }

        public IAsyncCommand RefreshDiscussionsCommand { get; }

        public IAsyncCommand RefreshDocumentsCommand { get; }

        public IAsyncCommand RefreshRepairsCommand { get; }

        public IAsyncCommand RemoveDocumentCommand { get; }

        public IAsyncCommand ReopenRequestCommand { get; }

        public IAsyncCommand ResetRequestCommand { get; }

        public IAsyncCommand RecomplectCommand { get; }

        public IAsyncCommand SendSmsCommand { get; }

        public IAsyncCommand TakeRequestCommand { get; }

        public IAsyncCommand RefreshCrmCommand { get; }

        public IDelegateCommand OpenRequirementOnHistoryCommand { get; }

        public IDelegateCommand ShowInvoiceCommand { get; }

        public IAsyncCommand PrintAktCommand { get; }

        public IDelegateCommand PrintSerialNumberCommand { get; }

        public IAsyncCommand PrintWarrantyCardCommand { get; }

        public IAsyncCommand PrintDefectivenessActCommand { get; }

        public IAsyncCommand PrintChequeCommand { get; }

        public IAsyncCommand PrintIssuanceCertificateCommand { get; }

        public IDelegateCommand HandleCarryInChangedCommand { get; }

        public IAsyncCommand HandleCityChangedCommand { get; }

        public IAsyncCommand PrintTrackNumberCommand { get; }

        public IDelegateCommand ShowServiceProductCommand { get; }

        public IAsyncCommand DenyConfirmCommand { get; }

        public IAsyncCommand AcceptConfirmCommand { get; }

        public IAsyncCommand ChangeProductCommand { get; }

        public IAsyncCommand RefreshComplaintsCommand { get; }

        public IDelegateCommand AddComplaintCommand { get; }

        public IAsyncCommand PrintReturnReportCommand { get; }

        public IAsyncCommand PrintReturnProtocolReportCommand { get; }

        public IDelegateCommand SelectDeliveryAddressCommand { get; }

        public DocumentCommands DocumentCommands { get; }

        #endregion Commands

        #region INPC

        public ReadOnlyObservableCollection<EmployeeDto> AllEmployees
        {
            get { return GetProperty(() => AllEmployees); }
            private set { SetProperty(() => AllEmployees, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> AllWarehouses
        {
            get { return GetProperty(() => AllWarehouses); }
            private set { SetProperty(() => AllWarehouses, value); }
        }

        public ReadOnlyObservableCollection<AuditEntry> AuditEntries
        {
            get { return GetProperty(() => AuditEntries); }
            private set { SetProperty(() => AuditEntries, value); }
        }

        public ObservableCollection<CallViewItem> Calls
        {
            get { return GetProperty(() => Calls); }
            private set { SetProperty(() => Calls, value); }
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public ReadOnlyObservableCollection<ServiceRequestDocumentType> DocumentTypes
        {
            get { return GetProperty(() => DocumentTypes); }
            private set { SetProperty(() => DocumentTypes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<ServiceRequestCompleteness> Completenesses
        {
            get { return GetProperty(() => Completenesses); }
            private set { SetProperty(() => Completenesses, value); }
        }

        public ReadOnlyObservableCollection<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public string CurrentTabName
        {
            get { return GetProperty(() => CurrentTabName); }
            set { SetProperty(() => CurrentTabName, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> DeclineReasons
        {
            get { return GetProperty(() => DeclineReasons); }
            private set { SetProperty(() => DeclineReasons, value); }
        }

        public string DiscussionMessage
        {
            get { return GetProperty(() => DiscussionMessage); }
            set { SetProperty(() => DiscussionMessage, value); }
        }

        public ObservableCollection<ServiceRequestDiscussionViewItem> Discussions
        {
            get { return GetProperty(() => Discussions); }
            private set { SetProperty(() => Discussions, value); }
        }

        public ObservableCollection<ServiceRequestDocumentSimpleDto> Documents
        {
            get { return GetProperty(() => Documents); }
            private set { SetProperty(() => Documents, value); }
        }

        public ReadOnlyObservableCollection<ServiceRequestLocation> Locations
        {
            get { return GetProperty(() => Locations); }
            private set { SetProperty(() => Locations, value); }
        }

        public ReadOnlyObservableCollection<ServiceRequestResolution> RequirementResolutions
        {
            get { return GetProperty(() => RequirementResolutions); }
            private set { SetProperty(() => RequirementResolutions, value); }
        }

        public ReadOnlyObservableCollection<ServiceRequestRequirement> RequirementTypes
        {
            get { return GetProperty(() => RequirementTypes); }
            private set { SetProperty(() => RequirementTypes, value); }
        }

        public IReadOnlyCollection<ServiceCenterDto> ServiceCenters
        {
            get { return GetProperty(() => ServiceCenters); }
            private set { SetProperty(() => ServiceCenters, value); }
        }

        public ReadOnlyObservableCollection<ServiceRequestState> States
        {
            get { return GetProperty(() => States); }
            private set { SetProperty(() => States, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ReadOnlyObservableCollection<ContractorDto> Suppliers
        {
            get { return GetProperty(() => Suppliers); }
            private set { SetProperty(() => Suppliers, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public WarehouseDto WarehouseIn
        {
            get { return GetProperty(() => WarehouseIn); }
            set { SetProperty(() => WarehouseIn, value, WarehouseInChanged); }
        }

        public ReadOnlyObservableCollection<ClientContactViewItem> ClientContactsHistoryItems
        {
            get { return GetProperty(() => ClientContactsHistoryItems); }
            private set { SetProperty(() => ClientContactsHistoryItems, value); }
        }

        public string HistoryActiveFilterString
        {
            get { return GetProperty(() => HistoryActiveFilterString); }
            set { SetProperty(() => HistoryActiveFilterString, value); }
        }

        public ObservableCollection<ServiceRepairViewItem> Repairs
        {
            get { return GetProperty(() => Repairs); }
            set { SetProperty(() => Repairs, value); }
        }

        public ReadOnlyObservableCollection<NewPostWarehouseViewItem> NpWarehouses
        {
            get { return GetProperty(() => NpWarehouses); }
            private set { SetProperty(() => NpWarehouses, value); }
        }

        public int SelectedTabIndex
        {
            get { return GetProperty(() => SelectedTabIndex); }
            set { SetProperty(() => SelectedTabIndex, value); }
        }

        public ObservableCollection<ComplaintViewItem> Complaints
        {
            get { return GetProperty(() => Complaints); }
            private set { SetProperty(() => Complaints, value); }
        }

        public bool CanCancelPresaleContractor => Contractors.FirstOrDefault(x => x.Id == Model.ContractorId)?.Name != Constants.PresaleContractor
                                                  || WebClient.IsOperationAllowed(BusinessOperation.PresaleCancelServiceRequest);

        #endregion INPC

        #region DialogSettings

        public override int Height => 675;

        public override int MinHeight => 675;

        public override int MinWidth => 1084;

        public override int Width => 1084;

        #endregion

        public bool OnConfirmationVisible =>
            (canAcceptConfirm || canDenyConfirm) &&
            Model?.EmployeeLockId == null &&
            Model?.StateId == ServiceRequestState.OnConfirmation.Id;

        public bool ChangeProductVisible => Model != null
            && Model.StateId == ServiceRequestState.New.Id
            && Model.EmployeeLockId == null
            && WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestCreate);

        public bool ShowRequisitesVisible => Model != null && Model.Requirement == ServiceRequestRequirement.ReturnMoney;

        public bool IsDeliveryChangeEnabled => Model?.Group == null;

        protected override string CreatedActionMessage { get; } = "создана";

        protected override string EntityName { get; } = "Заявка";

        protected override string UpdatedActionMessage { get; } = "сохранена";

        private IServiceRequestPrinter ServiceRequestPrinter { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IMediator Mediator { get; }

        private IRroPrintHelper RroPrintHelper { get; }

        private IErrorHandler ErrorHandler { get; }

        private IDialogService WizardDialogService => GetService<IDialogService>("HighWizardDialogService", ServiceSearchMode.PreferParents);

        private IOrderReportBuilder OrderReportBuilder { get; }

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>();

        private IDocumentManagerService SizeableNotMinimizeDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableNotMinimizeDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IAuditEntryProcessorBuilder AuditEntryProcessorBuilder { get; }

        public static void BuildMetadata(MetadataBuilder<ServiceRequestViewModel> builder)
        {
            builder.Property(x => x.Model).Required();
            builder.Property(x => x.WarehouseIn)
                .MatchesInstanceRule((x, y) => x != null, () => "Поле не заполнено либо заполнено невалидным значением");
        }

        public override void OnDestroy()
        {
            Messenger.Unregister<CallMessage>(this);

            base.OnDestroy();
        }

        protected override void AfterSetData()
        {
            Model.PurchasedSummary = string.Empty;

            if (Model.InvoiceId.HasValue && Model.PurchasedFromContractorId.HasValue && Model.PurchasedOn.HasValue)
            {
                string contractor = Contractors.FirstOrDefault(x => x.Id == Model.PurchasedFromContractorId.Value)?.Name
                    ?? Model.PurchasedFromContractorId.Value.ToString();

                Model.PurchasedSummary = $"{contractor} {Model.PurchasedOn.Value:dd.MM.yy} {Model.InvoiceId.Value}";
            }

            WarehouseIn = AllWarehouses.FirstOrDefault(x => x.Id == Model.WarehouseInId);

            RaisePropertiesChanged(nameof(OnConfirmationVisible), nameof(ChangeProductVisible));

            RefreshSummaryItems();

            HandleCarryInChanged();
        }

        protected override Task<Result<ServiceRequestDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(ServiceRequestDto dto, MessageType messageType)
        {
            return new ServiceRequestMessage(dto, messageType);
        }

        protected override Task<ServiceRequestDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(id));
        }

        protected override IEnumerable<string> GetMembersToIgnore()
        {
            yield return nameof(ServiceRequestViewItem.PurchasedSummary);
            yield return nameof(ServiceRequestViewItem.CallsCount);
            yield return nameof(ServiceRequestViewItem.NewCallsCount);
            yield return nameof(ServiceRequestViewItem.CallsCountString);
            yield return nameof(ServiceRequestViewItem.ComplaintsCount);
            yield return nameof(ServiceRequestViewItem.NewComplaintsCount);
            yield return nameof(ServiceRequestViewItem.ComplaintsCountString);
            yield return nameof(ServiceRequestViewItem.DiscussionsCount);
            yield return nameof(ServiceRequestViewItem.DocumentsCount);
            yield return nameof(ServiceRequestViewItem.SelectedServiceProductId);
        }

        protected override async Task HandleLoadedAsync()
        {
            DeclineReasons = Dictionaries.GetItems<ServiceRequestRejectReason>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            States = Dictionaries.GetItems<ServiceRequestState>().ToReadOnlyObservableCollection();
            RequirementTypes = Dictionaries.GetItems<ServiceRequestRequirement>().ToReadOnlyObservableCollection();
            RequirementResolutions = Dictionaries.GetItems<ServiceRequestResolution>().ToReadOnlyObservableCollection();
            Completenesses = Dictionaries.GetItems<ServiceRequestCompleteness>().ToReadOnlyObservableCollection();
            Locations = Dictionaries.GetItems<ServiceRequestLocation>().ToReadOnlyObservableCollection();
            DocumentTypes = Dictionaries.GetItems<ServiceRequestDocumentType>().ToReadOnlyObservableCollection();
            CarryTypes = Dictionaries.GetItems<CarryType>()
                .Where(x => x.Id is CarryType.PickupId or CarryType.NpWarehouseId or CarryType.NpDeliveryId or CarryType.NpPostBoxId)
                .ToReadOnlyObservableCollection();

            await Task.WhenAll(
                RefreshContractorsAsync(),
                RefreshCitiesAsync(),
                RefreshEmployeesAsync(),
                RefreshServiceCentersAsync(),
                RefreshWarehousesAsync());

            await base.HandleLoadedAsync();

            RaisePropertiesChanged(nameof(IsDeliveryChangeEnabled), nameof(ShowRequisitesVisible));

            RefreshRepairsCommand.Execute(null);

            RefreshDocumentsCommand.Execute(null);
        }

        protected override Task<LockResponse<ServiceRequestDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockServiceRequest(id));
        }

        protected override Task<LockResponse<ServiceRequestDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockServiceRequest(id));
        }

        protected override bool CanEdit()
        {
            return !Model.IsCompleted;
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            SummaryItems = new[]
            {
                new SummaryViewItem("Подразд.", "Телемарт"),
                new SummaryViewItem("Контрагент", "MTI"),
                new SummaryViewItem("Менеджер", "Сазонов Илья"),
                new SummaryViewItem("Создал", "Created by"),
                new SummaryViewItem("Принял", "Принял"),
                new SummaryViewItem("Изменено", $"{DateTime.Now:dd.MM.yy HH:mm}"),
                new SummaryViewItem("Принадлеж.", "Employee name"),
                new SummaryViewItem("Располож.", "some location")
            };
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override void SetEditTitle()
        {
            string name = Model.StateId == ServiceRequestState.New.Id
                ? "обращение"
                : "заявка";

            Title = $"Серв. {name} №{Model.Id.ToString(CultureInfo.InvariantCulture)} от {Model.CreatedOn:g}";
        }

        protected override Task<Result<ServiceRequestDto>> UpdateEntityAsync()
        {
            ServiceRequestSaveDto saveDto = Mapper.Map<ServiceRequestSaveDto>(Model);

            saveDto.Documents = Documents?
                .Select(x => new ServiceRequestDocumentSimpleSaveDto(x.Id, x.TypeId))
                .ToReadOnlyCollection();

            UpdateServiceRequest gatewayRequest = new UpdateServiceRequest(saveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        private static IEnumerable<CallViewItem> GetSortedCalls(IEnumerable<CallViewItem> calls)
        {
            return calls
                .OrderByDescending(x => x.CompletedOn == null)
                .ThenByDescending(x => x.CompletedOn)
                .ThenByDescending(x => x.Id);
        }

        private static IEnumerable<ServiceRepairViewItem> GetSortedRepairs(IEnumerable<ServiceRepairViewItem> repairs)
        {
            return repairs
                .OrderByDescending(x => x.CompletedOn == null)
                .ThenByDescending(x => x.CompletedOn)
                .ThenByDescending(x => x.Id);
        }

        private void AddCall()
        {
            CreateCallParameter parameter = new CreateCallParameter(
                CallDocumentType.ServiceRequest,
                Model.OrderId,
                Model.Id,
                Model.SubdivisionId,
                Model.ContractorId,
                Model.Fio,
                Model.Phone,
                Model.Phone2);

            parameter = parameter.WithPriority(Priority.Low);

            CreateCallViewModel viewModel = DialogDocumentManagerService.ShowView<CreateCallViewModel>(parameter, this);

            if (viewModel.IsOk && viewModel.ResultCall != null)
            {
                CallViewItem callViewItem = Mapper.Map<CallViewItem>(viewModel.ResultCall);

                Calls.Add(callViewItem);
                Calls = GetSortedCalls(Calls).ToObservableCollection();

                MessageFacadeService.ShowNotificationInfo($"Звонок №{viewModel.ResultCall.Id} успешно создан");

                Model.NewCallsCount = (Model.NewCallsCount ?? 0) + 1;
                Model.CallsCount = (Model.CallsCount ?? 0) + 1;
            }
        }

        private async Task AddDiscussionAsync(string message)
        {
            try
            {
                ServiceRequestDiscussionCreateDto dto = new ServiceRequestDiscussionCreateDto
                {
                    ServiceRequestId = Model.Id,
                    Message = message.Trim(),
                    ContractorContactId = null
                };

                CreateServiceRequestDiscussion gatewayRequest = new CreateServiceRequestDiscussion(dto);

                Result<ServiceRequestDiscussionDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                Discussions.Add(Mapper.Map<ServiceRequestDiscussionViewItem>(result.Data));
                Model.DiscussionsCount = Discussions.Count;

                DiscussionMessage = null;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении сообщения");
                ShowValidationResultView("Ошибки при сохранении сообщения", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create service request discussion");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to create service request discussion");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении сообщения");
            }
        }

        private void AddDocument()
        {
            const int MaxDocumentsCount = 25;
            const int MaxFileLengthMb = 7;

            if (Model.DocumentsCount >= MaxDocumentsCount)
            {
                MessageFacadeService.ShowNotificationWarning($"К заявке можно добавить не больше чем {MaxDocumentsCount} файлов");
                return;
            }

            if (OpenFileDialogService.ShowDialog())
            {
                if (OpenFileDialogService.Files.Count() + Model.DocumentsCount > MaxDocumentsCount)
                {
                    MessageFacadeService.ShowNotificationWarning($"К заявке можно добавить не больше чем {MaxDocumentsCount} файлов");
                    return;
                }

                if (!OpenFileDialogService.Files.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Выберите хотя бы 1 файл");
                    return;
                }

                List<IFileInfo> notValidFiles = OpenFileDialogService.Files.Where(x => x.Length > MaxFileLengthMb.Megabytes().Bytes).ToList();

                if (notValidFiles.Any())
                {
                    ShowValidationResultView(
                        "Ошибки при добавлении файлов",
                        notValidFiles.Select(x => new ValidationResultItem($"Файл \"{x.GetFullName()}\" должен быть меньше {MaxFileLengthMb} MB", true)).ToArray());
                }
                else
                {
                    AddDocumentsParameter parameter = new AddDocumentsParameter(
                        Model.Id,
                        OpenFileDialogService.Files.Select(x => x.GetFullName()).ToList());

                    ServiceRequestAddDocumentViewModel viewModel = new ServiceRequestAddDocumentViewModel(
                        WebClient,
                        Dictionaries,
                        MessageFacadeService,
                        Messenger);

                    NonModalSizeableDialogDocumentManagerService.ShowView("AddDocumentsView", viewModel, parameter, this);
                }
            }
        }

        private void ShowRequisites()
        {
            Fio fio = new Fio(Model.Fio);

            RefundRequisitesParameter parameter = new RefundRequisitesParameter(
                new RequisitesViewItem()
                {
                    CardNumber = Model.Requisites?.CardNumber,
                    Iban = Model.Requisites?.Iban,
                    Inn = Model.Requisites?.Inn,
                    FirstName = Model.Requisites?.FirstName ?? fio.FirstName,
                    LastName = Model.Requisites?.LastName ?? fio.LastName,
                    MiddleName = Model.Requisites?.MiddleName ?? fio.MiddleName,
                    Enabled = Model.EmployeeLockId.HasValue,
                    Required = false,
                    Visible = true
                },
                Model.Id);

            if (Model.EmployeeLockId is null)
            {
                MessageFacadeService.ShowNotificationInfo(
                    "Изменение реквизитов доступно\nв режиме редактирования заявки");
            }

            NonModalDialogDocumentManagerService.ShowView<RefundRequisitesViewModel>(parameter, this);
        }

        private void AddComplaint()
        {
            ComplaintCreateParameter parameter = new ComplaintCreateParameter(
                Model.OrderId,
                Model.Id,
                null,
                Model.ContractorId,
                new[] { new ComboBoxItem(Model.ProductId, Model.ProductName) },
                Model.Fio,
                Model.Phone,
                Model.Phone2,
                Model.Email);

            NonModalDialogDocumentManagerService.ShowView<ComplaintCreateViewModel>(parameter, this);
        }

        private bool CanAddDiscussion(string message)
        {
            return !string.IsNullOrWhiteSpace(message) && message.Trim().Length >= 2;
        }

        private bool CanCancelRequest()
        {
            return Model != null
                   && Model.StateId == ServiceRequestState.New.Id
                   && Model.EmployeeLockId == null
                   && CanCancelPresaleContractor;
        }

        private async Task CancelCallAsync(CallViewItem callViewItem)
        {
            if (callViewItem.CallState == CallState.Canceled)
            {
                MessageFacadeService.ShowNotificationWarning("Звонок уже отменен");
                return;
            }

            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Причина отмены",
                "Причина");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (fromUserViewModel.IsOk)
            {
                if (string.IsNullOrWhiteSpace(fromUserViewModel.Content))
                {
                    MessageFacadeService.ShowNotificationWarning("Не заполнена причина отмены");
                    return;
                }
            }
            else
            {
                return;
            }

            try
            {
                Result<CallDto> result = await WebClient.ExecuteApiRequestAsync(new CancelCall(callViewItem.Id, fromUserViewModel.Content.Trim()));
                CallDto newCall = result.Data;

                MessageFacadeService.ShowNotificationInfo($"Звонок №{newCall.Id} успешно отменен");

                Messenger.Send(new CallMessage(result.Data, MessageType.Changed));

                CallViewItem existingCall = Calls.FirstOrDefault(x => x.Id == newCall.Id);
                Mapper.Map(newCall, existingCall);

                Model.NewCallsCount -= 1;
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки при отмене звонка", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving call");
                MessageFacadeService.ShowNotificationError("Ошибка при отмене звонка");
            }
        }

        private Task CancelRequestAsync()
        {
            return ExecuteLockableOperationAsync(
                async _ =>
                {
                    GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                        new GetTextFromUserParameter("Причина", "Отмена заявки"),
                        this);

                    if (viewModel.IsOk)
                    {
                        CancelServiceRequest gatewayRequest = new CancelServiceRequest(Model.Id, viewModel.Content);
                        await WebClient.ExecuteApiRequestAsync(gatewayRequest);
                    }
                });
        }

        private async Task ChangeProductAsync()
        {
            if (Model.BundleId.HasValue)
            {
                MessageFacadeService.ShowNotificationError("Запрещено менять товар из бандла");
                return;
            }

            if (!string.IsNullOrWhiteSpace(Model.SerialNumber))
            {
                AssembledComputersFilteringItem assembledComputersFilteringItem = new AssembledComputersFilteringItem
                {
                    NomenclatureSeries = Model.SerialNumber
                };

                List<AssembledComputerDto> assembledComputers = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputers(assembledComputersFilteringItem));

                if (assembledComputers.Any(x => x.ProductId == Model.ProductId && x.NomenclatureSeriesAccounting))
                {
                    MessageFacadeService.ShowMessageBoxError("Запрещено менять товар по которому ведется учет серий номенклатур. Замену товаров необхидомо осуществлять на форме 'Создание новой комплектации'");
                    return;
                }
            }

            await ExecuteLockableOperationAsync(
                lockedEntity =>
                {
                    DialogDocumentManagerService.ShowView<ServiceRequestChangeProductViewModel>(new ServiceRequestChangeProductParameter(lockedEntity.Id, lockedEntity.ProductName), this);
                });
        }

        private bool CanChangeRequirement()
        {
            return Model != null
                   && Model.StateId == ServiceRequestState.New.Id
                   && canChangeRequirement
                   && !IsLockedByCurrentEmployee
                   && (Model.ServiceRepairTypeId == ServiceRepairType.Warranty.Id || Model.ServiceRepairTypeId is null);
        }

        private bool CanCompensateRequest()
        {
            return Model != null
                   && (Model.StateId == ServiceRequestState.InProgress.Id || Model.StateId == ServiceRequestState.InRepair.Id)
                   && canCompensate
                   && Model.EmployeeLockId == null
                   && (Model.ServiceRepairTypeId != ServiceRepairType.Paid.Id);
        }

        private bool CanCompleteRequest()
        {
            return Model != null
                && Model.StateId == ServiceRequestState.Ready.Id
                && canComplete
                && Model.EmployeeLockId == null;
        }

        private bool CanDiagnoseRequest()
        {
            return Model != null
                && Model.StateId == ServiceRequestState.Accepted.Id
                && Model.EmployeeLockId == null
                && canDiagnoze;
        }

        private bool CanReopen()
        {
            return Model != null
                   && Model.StateId == ServiceRequestState.Cancelled.Id
                   && canReopen
                   && Model.EmployeeLockId == null;
        }

        private bool CanReset()
        {
            return Model != null
                && (Model.StateId == ServiceRequestState.Accepted.Id || (Model.StateId == ServiceRequestState.Ready.Id && WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestResetButtonInReadyState)))
                && Model.EmployeeLockId == null
                && canReset
                && (Model.ServiceRepairTypeId == ServiceRepairType.Warranty.Id || Model.ServiceRepairTypeId is null);
        }

        private bool CanSendSms(string phone)
        {
            return !string.IsNullOrWhiteSpace(phone);
        }

        private bool CanTakeRequest()
        {
            return Model != null
                && Model.StateId == ServiceRequestState.New.Id
                && canTake
                && Model.EmployeeLockId == null;
        }

        private Task ChangeRequirementAsync()
        {
            if (Model.BundleId.HasValue)
            {
                MessageFacadeService.ShowMessageBoxWarning("Для сервисной заявке по товару из бандла запрещено изменять требование");
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(_ =>
            {
                DialogDocumentManagerService.ShowView<ChangeRequirementViewModel>(Model, this);
            });
        }

        private bool CanCloseDiscussion()
        {
            return Model != null && Model.DiscussionState != ServiceRequestDiscussionState.NoMessage.Id;
        }

        private async Task CloseDiscussionAsync()
        {
            if (!MessageFacadeService.Confirm("Вопрос решен?"))
            {
                return;
            }

            try
            {
                CloseServiceRequestDiscussion gatewayRequest = new CloseServiceRequestDiscussion(Model.Id);

                Result<ServiceRequestDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                Model.DiscussionState = result.Data.DiscussionState;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to close service request discussion");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private Task CompensateRequestAsync()
        {
            return ExecuteLockableOperationAsync(_ =>
            {
                DialogDocumentManagerService.ShowView<CompensateServiceRequestViewModel>(Model, this);
            });
        }

        private Task CompleteRequestAsync()
        {
            return ExecuteLockableOperationAsync(async _ =>
            {
                int groupServiceRequestsCount = 0;

                if (Model.Group != null)
                {
                    ServiceRequestsFilterViewModel serviceRequestFilter = new ServiceRequestsFilterViewModel(WebClient, Dictionaries) { GroupId = Model.Group.Value.Id };
                    PagedResult<ServiceRequestDto> groupServiceRequests = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequests(serviceRequestFilter.GetFilteringItem()));
                    groupServiceRequestsCount = groupServiceRequests.Data.Count;
                }

                if (groupServiceRequestsCount > 0)
                {
                    if (ShowValidationResultView("Завершение заявки", new[] { new ValidationResultItem($"Заявка находится в группе. Если данная заявка последняя не завершенная, то после ее завершения\nгруппа завершается автоматически. Заявок в группе {groupServiceRequestsCount}.\nПродолжить завершение заявки?", false) }))
                    {
                        if (Model.CarryOut != null && Model.CarryOut.Id != CarryType.PickupId)
                        {
                            DialogDocumentManagerService.ShowView<CompleteServiceRequestViewModel>(Model.Id, this);
                        }
                        else
                        {
                            Result<ServiceRequestDto> result = await WebClient.ExecuteApiRequestAsync(new CompleteServiceRequest(Model.Id, null));

                            if (result.Warnings.Any())
                            {
                                ShowValidationResultView(
                                    "Предупрежедения при завершении заявки",
                                    result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                            }

                            SetData(result.Data);
                        }
                    }
                }
                else
                {
                    if (MessageFacadeService.Confirm("Вы уверены, что хотите завершить заявку?"))
                    {
                        if (Model.CarryOut != null && Model.CarryOut.Id != CarryType.PickupId)
                        {
                            DialogDocumentManagerService.ShowView<CompleteServiceRequestViewModel>(Model.Id, this);
                        }
                        else
                        {
                            Result<ServiceRequestDto> result = await WebClient.ExecuteApiRequestAsync(new CompleteServiceRequest(Model.Id, null));

                            if (result.Warnings.Any())
                            {
                                ShowValidationResultView(
                                    "Предупрежедения при завершении заявки",
                                    result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                            }

                            SetData(result.Data);
                        }
                    }
                }
            });
        }

        private void ShowMoneyRefund(int? moneyRefundId)
        {
            Messenger.Send(new RefundViewMessage(moneyRefundId!.Value));
        }

        private void ShowServiceProduct(int? id)
        {
            if (id is null)
            {
                return;
            }

            Messenger.Send(new ServiceProductViewMessage(id.Value));
        }

        private async Task DiagnoseRequestAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(
                Model,
                false,
                propertyFilter: (p) => p.DisplayName != nameof(ServiceRequestViewItem.Requisites)))
            {
                MessageFacadeService.ShowNotificationWarning("Не заполнены обязательные поля");
                return;
            }

            await ExecuteLockableOperationAsync(
                async _ =>
                {
                    RequisitesViewItem requisites = Model.Requisites is null
                        ? new RequisitesViewItem()
                        : ReflectionObjectCloner.Clone(Model.Requisites);

                    AssembledComputersFilteringItem assembledComputersFilteringItem = new AssembledComputersFilteringItem
                    {
                        NomenclatureSeries = Model.SerialNumber
                    };

                    List<AssembledComputerDto> assembledComputers = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputers(assembledComputersFilteringItem));

                    DiagnoseServiceRequestModel diagnoseServiceRequestModel = new DiagnoseServiceRequestModel
                    {
                        Id = Model.Id,
                        Requirement = Model.Requirement,
                        RequirementPaymentId = Model.RequirementPaymentId,
                        RequirementSummary = Model.RequirementSummary,
                        StatedDefect = Model.StatedDefect,
                        Appearance = Model.Appearance,
                        CompletenessComment = Model.CompletenessComment,
                        OrderId = Model.OrderId,
                        ContractorId = Model.ContractorId,
                        ProductId = Model.ProductId,
                        ShowBonusAmount = Model.Requirement == ServiceRequestRequirement.TradeIn,
                        BonusAmount = (int)(Model.TradeInBuyoutAmount ?? 0),
                        SerialNumber = Model.SerialNumber,
                        NomenclatureSeriesAccounting = assembledComputers.Any(x => x.ProductId.HasValue && x.NomenclatureSeriesAccounting),
                        ProductName = Model.ProductName,
                        Requisites = requisites
                    };

                    WizardDialogViewModel<DiagnoseServiceRequestModel> dialogViewModel = new WizardDialogViewModel<DiagnoseServiceRequestModel>(
                        typeof(ResolutionPageViewModel),
                        diagnoseServiceRequestModel,
                        this);

                    WizardDialogService.ShowDialog(MessageButton.OKCancel, "Диагностика", dialogViewModel);
                },
                () => CanDiagnoseAsync(Model.Id));

            async Task<IReadOnlyCollection<ValidationResultItem>> CanDiagnoseAsync(int serviceRequestId)
            {
                IReadOnlyCollection<ValidationResultItem> result = null;

                try
                {
                    Result<ServiceRequestDto> canDiagnoseResult = await WebClient.ExecuteApiRequestAsync(new CanDiagnoseServiceRequest(serviceRequestId));

                    if (canDiagnoseResult.Warnings.Any())
                    {
                        result = canDiagnoseResult.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray();
                    }
                }
                catch (UnexpectedSatusException exception)
                {
                    result = exception.GetErrorItems();
                }
                catch (UnexpectedErrorException exception)
                {
                    result = new[] { new ValidationResultItem(Resources.ServerUnavailable, true) };

                    Logger.LogError(exception, "Diagnostic error");
                }
                catch (Exception exception)
                {
                    result = new[] { new ValidationResultItem("Ошибка при диагностике", true) };

                    Logger.LogError(exception, "Diagnostic error");
                }

                return result;
            }
        }

        private void EditCall(CallViewItem callViewItem)
        {
            if (callViewItem.CallState != CallState.New)
            {
                MessageFacadeService.ShowNotificationWarning("Звонок уже завершен");
            }
            else
            {
                Messenger.Send(new CallViewMessage(callViewItem.Id));
            }
        }

        private void Call(CallViewItem viewItem)
        {
            Messenger.Send(new OutcomingCallViewMessage(viewItem));
        }

        private void EditRepair(ServiceRepairViewItem repairViewItem)
        {
            Messenger.Send(new ServiceRepairViewMessage(repairViewItem.Id));
        }

        private async Task EditDiscussionAsync(ServiceRequestDiscussionViewItem discussion)
        {
            try
            {
                if (discussion.CreatedBy != WebClient.AuthenticatedEmployee.Id)
                {
                    MessageFacadeService.ShowNotificationWarning("Можно редактировать только свои сообщения");
                    return;
                }

                if ((DateTime.Now - discussion.CreatedOn).TotalHours > 1)
                {
                    MessageFacadeService.ShowNotificationWarning("Изменение сообщения доступно лишь в течении часа с момента создания");
                    return;
                }

                GetTextFromUserParameter parameter = new GetTextFromUserParameter(
                    "Изменение сообщения",
                    "Сообщение",
                    @"^.{2,}$",
                    "Сообщение должно содержать хотя бы 2 символа",
                    discussion.Message);

                GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(parameter, this);

                if (!fromUserViewModel.IsOk)
                {
                    return;
                }

                UpdateServiceRequestDiscussion gatewayRequest = new UpdateServiceRequestDiscussion(
                    Model.Id,
                    discussion.Id,
                    fromUserViewModel.Content);

                Result<ServiceRequestDiscussionDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                discussion.Message = result.Data.Message;

                MessageFacadeService.ShowNotificationInfo("Сообщение успешно изменено");
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки при редактировании сообщения", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving service request discussion");
                MessageFacadeService.ShowNotificationError("Ошибка при редактировании сообщения");
            }
        }

        private IReadOnlyCollection<ISmsTemplate> GetSmsTemplates(int serviceRequestId, string trackNumber, string warehouseAddress, string warehouseInfo)
        {
            SmsTemplate[] smsTemplates = Dictionaries.GetItems<SmsTemplate>().ToArray();

            List<ISmsTemplate> messageTemplates = new List<ISmsTemplate>();

            foreach (SmsTemplate smsTemplate in smsTemplates.Where(x => x.ShowInServiceRequest).OrderBy(x => x.Position))
            {
                switch (smsTemplate.Id)
                {
                    case SmsTemplate.ServiceRequestNumberId:
                        messageTemplates.Add(new NewServiceRequestSmsTemplate(serviceRequestId, smsTemplate));
                        break;
                    case SmsTemplate.ServiceRequestCardRefundId:
                        messageTemplates.Add(new CardRefundSmsTemplate(serviceRequestId, smsTemplate));
                        break;
                    case SmsTemplate.ServiceRequestTtnId:
                        messageTemplates.Add(new TrackNumberSmsTemplate(serviceRequestId, trackNumber, smsTemplate));
                        break;
                    case SmsTemplate.ServiceRequestRefundId:
                        messageTemplates.Add(new RefundPickupSmsTemplate(serviceRequestId, warehouseAddress, warehouseInfo, smsTemplate));
                        break;
                    case SmsTemplate.ServiceRequestProductPickupId:
                        messageTemplates.Add(new ProductPickupSmsTemplate(serviceRequestId, warehouseAddress, warehouseInfo, smsTemplate));
                        break;
                    case SmsTemplate.ServiceRequestNotReachedId:
                        messageTemplates.Add(new NotReachedSmsTemplate(serviceRequestId, smsTemplate));
                        break;
                }
            }

            return messageTemplates.ToArray();
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems(ServiceRequestViewItem serviceRequest)
        {
            ContractorDto contractor = Contractors.FirstOrDefault(x => x.Id == serviceRequest.ContractorId);
            Subdivision subdivision = Dictionaries.GetItemById<Subdivision>(serviceRequest.SubdivisionId);
            string manager = AllEmployees.FirstOrDefault(x => x.Id == contractor?.EmployeeId)?.Name;
            EmployeeDto createdBy = AllEmployees.FirstOrDefault(x => x.Id == serviceRequest.CreatedBy);
            string location = GetLocationString();

            yield return new SummaryViewItem("Подразд.", subdivision?.Name);
            yield return new SummaryViewItem("Контрагент", contractor?.Name);
            yield return new SummaryViewItem("Менеджер", manager);
            yield return new SummaryViewItem("Создал", createdBy?.Name);

            if (serviceRequest.ReceivedBy.HasValue && serviceRequest.ReceivedOn.HasValue)
            {
                EmployeeDto receivedBy = AllEmployees.FirstOrDefault(x => x.Id == serviceRequest.ReceivedBy);
                DateTime receivedOn = serviceRequest.ReceivedOn.Value;

                yield return new SummaryViewItem("Принял", $"{receivedBy?.Name} ({receivedOn:dd.MM.yy})");

                if (!string.IsNullOrWhiteSpace(serviceRequest.Inspection))
                {
                    yield return new SummaryViewItem("Осмотр", serviceRequest.Inspection);
                }
            }

            if (serviceRequest.DiagnosticBy.HasValue && serviceRequest.DiagnosticOn.HasValue)
            {
                EmployeeDto diagnosticBy = AllEmployees.FirstOrDefault(x => x.Id == serviceRequest.DiagnosticBy);
                DateTime diagnosticOn = serviceRequest.DiagnosticOn.Value;

                yield return new SummaryViewItem("Диагностика", $"{diagnosticBy?.Name} ({diagnosticOn:dd.MM.yy})");
            }

            if (serviceRequest.DateX.HasValue)
            {
                int level = serviceRequest.State.InProgressFlag && serviceRequest.DateX < DateTime.Now
                    ? SummaryViewItem.RedLevel
                    : SummaryViewItem.NormalLevel;

                yield return new SummaryViewItem("Дата Х", $"{serviceRequest.DateX:dd.MM.yy HH:mm}", level);
            }

            yield return new SummaryViewItem("Изменено", $"{serviceRequest.LastActivityOn:dd.MM.yy HH:mm}");

            if (!string.IsNullOrEmpty(location))
            {
                yield return new SummaryViewItem("Располож.", location);
            }

            if (serviceRequest.ServiceRepairId.HasValue)
            {
                string serviceCenter = ServiceCenters.FirstOrDefault(x => x.Id == serviceRequest.ServiceRepairId)?.Name;
                yield return new SummaryViewItem("Серв. центр", serviceCenter);
            }

            yield return new SummaryViewItem("Статус", serviceRequest.State.Name);

            if (!string.IsNullOrWhiteSpace(serviceRequest.CustomerStateText))
            {
                yield return new SummaryViewItem("Детализация", serviceRequest.CustomerStateText);
            }

            if (serviceRequest.Group != null)
            {
                yield return new SummaryViewItem("Группа", $"{serviceRequest.Group.Value.Id}");
            }

            if (serviceRequest.BundleId != null)
            {
                yield return new SummaryViewItem("Бандл", $"{serviceRequest.BundleId}");
            }

            if (!string.IsNullOrWhiteSpace(serviceRequest.ServiceActNumber))
            {
                yield return new SummaryViewItem("Акт", serviceRequest.ServiceActNumber);
            }

            if (serviceRequest.CompletedOnMoneyRefund)
            {
                yield return new SummaryViewItem("Возвр. ДС", SignalsConstants.TrueStr);
            }

            if (!string.IsNullOrEmpty(serviceRequest.FiscalId))
            {
                yield return new SummaryViewItem("Фиск. чек", SignalsConstants.TrueStr);
            }
        }

        private void HandleSelectionChanged(ValueChangedEventArgs<FrameworkElement> e)
        {
            CurrentTabName = e.NewValue.Name;

            switch (e.NewValue.Name)
            {
                case "DocumentsLayoutGroup":
                    if (Documents == null)
                    {
                        RefreshDocumentsCommand.Execute(null);
                    }

                    break;

                case "CallsLayoutGroup":
                    if (Calls == null)
                    {
                        RefreshCallsCommand.Execute(null);
                    }

                    break;

                case "RepairsLayoutGroup":
                    if (Repairs == null)
                    {
                        RefreshRepairsCommand.Execute(null);
                    }

                    break;

                case "DiscussionsLayoutGroup":
                    if (Discussions == null)
                    {
                        RefreshDiscussionsCommand.Execute(null);
                    }

                    break;

                case "HistoryLayoutGroup":
                    if (AuditEntries == null)
                    {
                        RefreshAuditEntriesCommand.Execute(null);
                    }

                    break;

                case "CrmTab":
                    if (ClientContactsHistoryItems == null)
                    {
                        RefreshCrmCommand.Execute(null);
                    }

                    break;
                case "ComplaintsLayoutGroup":
                    if (Complaints == null)
                    {
                        RefreshComplaintsCommand.Execute(null);
                    }

                    break;
            }
        }

        private void OnCallMessage(CallMessage message)
        {
            CallDto dto = message.Entity;

            if (dto.ServiceRequestId == Model.Id)
            {
                switch (message.MessageType)
                {
                    case MessageType.Changed:
                        Calls?.DoActionWithItem(x => x.Id == dto.Id, x => Mapper.Map(dto, x));
                        break;
                }
            }
        }

        private void OnDocumentMessage(ServiceRequestDocumentMessage message)
        {
            if (message.Entity.ServiceRequestId == Model.Id)
            {
                switch (message.MessageType)
                {
                    case MessageType.Added:
                        Documents?.Add(message.Entity);
                        Model.DocumentsCount = Documents?.Count;
                        break;
                }
            }
        }

        private void OnServiceRepairMessage(ServiceRepairMessage message)
        {
            ServiceRepairDto dto = message.Entity;

            if (dto.ServiceRequestId == Model.Id)
            {
                switch (message.MessageType)
                {
                    case MessageType.Added:
                        Repairs?.Add(Mapper.Map<ServiceRepairViewItem>(dto));
                        break;
                    case MessageType.Changed:
                        Repairs?.DoActionWithItem(x => x.Id == dto.Id, x => Mapper.Map(dto, x));
                        break;
                }
            }
        }

        private void OnServiceRepairWorkflowMessage(ServiceRepairWorkflowMessage message)
        {
            ServiceRepairDto dto = message.Entity;

            if (dto.ServiceRequestId == Model.Id && !IsLockedByCurrentEmployee)
            {
                ServiceRequestDto source = WebClient.ExecuteApiRequest(new QueryServiceRequest(Model.Id));
                SetData(source);
            }
        }

        private void OnComplaintMessage(ComplaintMessage message)
        {
            ComplaintDto dto = message.Entity;

            if (dto.ServiceRequestId == Model.Id)
            {
                switch (message.MessageType)
                {
                    case MessageType.Added:
                        Complaints?.Add(Mapper.Map<ComplaintViewItem>(dto));
                        break;
                    case MessageType.Changed:
                        Complaints?.DoActionWithItem(x => x.Id == dto.Id, x => Mapper.Map(dto, x));
                        break;
                }
            }

            Model.ComplaintsCount = Complaints?.Count;
            Model.NewComplaintsCount = Complaints?.Count(x => x.State == ComplaintState.New);
        }

        private void OpenOrder(int? orderId)
        {
            Messenger.Send(new OrderEditViewMessage(orderId!.Value));
        }

        private async Task PrintDocumentAsync(ServiceRequestDocumentSimpleDto documentObj)
        {
            byte[] data;
            string ext;

            if (documentObj.TradeInDocument)
            {
                TradeInDocumentDto document = await WebClient.ExecuteApiRequestAsync(new QueryTradeInDocument(documentObj.Id));

                data = document.Data;
                ext = document.Ext;
            }
            else if (documentObj.TradeInEDocument)
            {
                TradeInEDocumentDto document = await WebClient.ExecuteApiRequestAsync(new QueryTradeInEDocument(documentObj.Id));

                data = document.Bytes;
                ext = documentObj.Ext;
            }
            else
            {
                ServiceRequestDocumentDto document = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequestDocument(documentObj.Id));

                data = document.Data;
                ext = document.Ext;
            }

            await FileHelper.OpenAsFileAsync(data, ext);
        }

        private async Task RefreshAuditEntriesAsync()
        {
            AuditEntries = null;

            try
            {
                IReadOnlyCollection<AuditEntryDto> auditEntries = await WebClient.ExecuteApiRequestAsync(new QueryAuditEntries("ServiceRequest", Model.Id));

                IAuditEntryProcessor auditEntryProcessor = await AuditEntryProcessorBuilder.BuildAsync();

                AuditEntries = auditEntryProcessor.Process(auditEntries).ToReadOnlyObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get service request history");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshCallsAsync()
        {
            Calls = null;

            try
            {
                List<CallDto> calls = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequestCalls(Model.Id));
                Calls = GetSortedCalls(calls.Select(x => Mapper.Map<CallViewItem>(x))).ToObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get service request calls");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshRepairsAsync()
        {
            Repairs = null;

            try
            {
                List<ServiceRepairDto> repairs = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequestRepairs(Model.Id));
                Repairs = GetSortedRepairs(repairs.Select(x => Mapper.Map<ServiceRepairViewItem>(x))).ToObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get service request repairs");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync().ConfigureAwait(false);

            Cities = cities
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractorsList = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            Contractors = contractorsList.ToReadOnlyObservableCollection();
            Suppliers = contractorsList
                .Where(x => x.IsSupplier && x.Active && !x.IsFolder)
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshDiscussionsAsync()
        {
            Discussions = null;

            try
            {
                List<ServiceRequestDiscussionDto> discussions = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequestDiscussions(Model.Id));

                Discussions = discussions
                    .Select(x => Mapper.Map<ServiceRequestDiscussionViewItem>(x))
                    .OrderBy(x => x.CreatedOn)
                    .ToObservableCollection();

                Model.DiscussionsCount = Discussions.Count;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get service request discussions");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshDocumentsAsync()
        {
            Documents = null;

            try
            {
                QueryServiceRequestDocuments gatewayRequest = new QueryServiceRequestDocuments(Model.Id);
                List<ServiceRequestDocumentSimpleDto> documents = await WebClient.ExecuteApiRequestAsync(gatewayRequest);
                Documents = new ObservableCollection<ServiceRequestDocumentSimpleDto>(documents);

                if (Model.TradeInId.HasValue)
                {
                    List<TradeInDocumentSimpleDto> tradeInDocuments = await WebClient.ExecuteApiRequestAsync(new QueryTradeInDocuments(Model.TradeInId.Value));

                    Documents.AddRange(tradeInDocuments.Select(x => new ServiceRequestDocumentDto()
                    {
                        Id = x.Id,
                        Name = $"{x.Name} (Trade-In)",
                        Ext = x.Ext,
                        TradeInDocument = true,
                        TypeId = x.ServiceRequestDocumentTypeId ?? 0,
                        CreatedBy = x.CreatedBy,
                        CreatedOn = x.CreatedOn
                    }));

                    await ErrorHandler.HandleErrorsAsync(
                        async _ => await WebClient.ExecuteApiRequestAsync(new QueryTradeInEDocuments(Model.TradeInId.Value)),
                        "получении Trade-In E-документов",
                        null,
                        this,
                        true,
                        onSuccess: (tradeInEDocumentsResult, _) => {
                            Documents.AddRange(tradeInEDocumentsResult.Data.Select(x => new ServiceRequestDocumentDto()
                            {
                                Id = x.Id,
                                Name = $"{x.Name} (Trade-In)",
                                Ext = "pdf",
                                TradeInEDocument = true,
                                TypeId = 0,
                                CreatedBy = x.CreatedBy,
                                CreatedOn = x.CreatedOn
                            }));

                            return Task.CompletedTask;
                        });
                }

                Model.DocumentsCount = Documents.Count;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get service request documents");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RecomplectAsync()
        {
            ServiceRequestNomenclatureSeriesParameter parameter = new ServiceRequestNomenclatureSeriesParameter(Model.ProductId, Model.ProductName, Model.SerialNumber, false, Model.Id);

            ServiceRequestNomenclatureSeriesViewModel viewModel = DialogDocumentManagerService.ShowView<ServiceRequestNomenclatureSeriesViewModel>(parameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Result<ServiceRequestDto> result = await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new RecomplectServiceRequest(Model.Id, viewModel.GetSaveDto())), "перекомплектации", "Перекомплектация завершена", this, true);

            if (result?.IsSuccess != true)
            {
                return;
            }

            SetData(result.Data);
        }

        private async Task RefreshCrmAsync()
        {
            ClientContactsHistoryItems = null;

            try
            {
                List<ClientContactDto> clientContacts = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequestClientContacts(Model.Id));

                ClientContactsHistoryItems = clientContacts
                    .Select(x => Mapper.Map<ClientContactViewItem>(x))
                    .OrderByDescending(x => x.Date)
                    .ToReadOnlyObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get serviceReqeust crm info");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshComplaintsAsync()
        {
            Complaints = null;

            try
            {
                IFilteringItem filteringItem = new ComplaintsFilteringItem { ServiceRequestIds = Model.Id.ToString() };

                List<ComplaintDto> complaints = await WebClient.ExecuteApiRequestAsync(new QueryComplaints(filteringItem)).GetPagedResultDataAsync();
                Complaints = complaints
                    .Select(x => Mapper.Map<ComplaintViewItem>(x))
                    .ToObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get service request complaints");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employeesList = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            AllEmployees = employeesList.ToReadOnlyObservableCollection();
        }

        private async Task RefreshServiceCentersAsync()
        {
            PagedResult<ServiceCenterDto> serviceCenters = await WebClient.ExecuteApiRequestAsync(new QueryServiceCenters());
            ServiceCenters = serviceCenters.Data;
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems(Model);
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehousesList = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            AllWarehouses = warehousesList.ToReadOnlyObservableCollection();
        }

        private async Task RemoveDocumentAsync(ServiceRequestDocumentSimpleDto document)
        {
            if (document.TradeInDocument || document.TradeInEDocument)
            {
                MessageFacadeService.ShowMessageBoxError("Запрещено удалять Trade-In документ из сервисной заявки");
                return;
            }

            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteServiceRequestDocument(document.Id));

                Documents.Remove(document);
                Model.DocumentsCount = Documents.Count;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to delete a document");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении документа");
            }
        }

        private Task ReopenRequestAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите восстановить заявку?"))
            {
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(
                async lockedEntity =>
                {
                    ReopenServiceRequest gatewayRequest = new ReopenServiceRequest(lockedEntity.Id);
                    Result<ServiceRequestDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);
                    SetData(result.Data);
                });
        }

        private Task ResetRequestAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите переоткрыть заявку?"))
            {
                return Task.CompletedTask;
            }

            if (IDataErrorInfoHelper.HasErrors(Model))
            {
                MessageFacadeService.ShowNotificationWarning("Не заполнены обязательные поля");
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(
                async lockedEntity =>
                {
                    ResetServiceRequest gatewayRequest = new ResetServiceRequest(lockedEntity.Id);
                    Result<ServiceRequestDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);
                    SetData(result.Data);
                });
        }

        private async Task SendSmsAsync(string phone)
        {
            Subdivision subdivision = Dictionaries.GetItemById<Subdivision>(Model.SubdivisionId);

            if (subdivision == Subdivision.Retail)
            {
                MessageFacadeService.ShowNotificationWarning("У заявки недопустимое подразделение");
                return;
            }

            if (Model.WarehouseInId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Не заполнен склад в заявке");
                return;
            }

            warehouseIn ??= await WebClient.ExecuteApiRequestAsync(new QueryWarehouse(Model.WarehouseInId.Value));

            SendSmsParameter sendSmsParameter = new SendSmsParameter(
                Model.OrderId,
                Model.Id,
                null,
                subdivision,
                phone,
                Model.Phone2,
                GetSmsTemplates(Model.Id, Model.TtnOut, warehouseIn.AddressUa, warehouseIn.Info));

            DialogDocumentManagerService.ShowView<SendSmsViewModel>(sendSmsParameter, this);
        }

        private async Task TakeRequestAsync()
        {
            if (!string.IsNullOrWhiteSpace(Model.SerialNumber))
            {
                AssembledComputersFilteringItem assembledComputersFilteringItem = new AssembledComputersFilteringItem
                {
                    NomenclatureSeries = Model.SerialNumber
                };

                List<AssembledComputerDto> assembledComputers = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputers(assembledComputersFilteringItem));

                if (assembledComputers.Any(x => x.ProductId == Model.ProductId && x.NomenclatureSeriesAccounting == false))
                {
                    if (!MessageFacadeService.Confirm("По товару не ведется учет серий номенклатур. На ремонт будет отправлен готовый ПК. Продолжить?"))
                    {
                        return;
                    }
                }
            }

            await ExecuteLockableOperationAsync(
                _ =>
                {
                    DialogDocumentManagerService.ShowView<TakeServiceRequestViewModel>(Model.Clone(), this);
                    return Task.CompletedTask;
                },
                () => CanTakeAsync(Model.Id));

            async Task<IReadOnlyCollection<ValidationResultItem>> CanTakeAsync(int serviceRequestId)
            {
                IReadOnlyCollection<ValidationResultItem> result = null;

                try
                {
                    Result<ServiceRequestDto> canTakeResult = await WebClient.ExecuteApiRequestAsync(new CanTakeServiceRequest(serviceRequestId));

                    if (canTakeResult.Warnings.Any())
                    {
                        result = canTakeResult.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray();
                    }
                }
                catch (UnexpectedSatusException exception)
                {
                    result = exception.GetErrorItems();
                }
                catch (UnexpectedErrorException exception)
                {
                    result = new[] { new ValidationResultItem(Resources.ServerUnavailable, true) };
                    Logger.LogError(exception, "Diagnostic error");
                }
                catch (Exception exception)
                {
                    result = new[] { new ValidationResultItem("Ошибка при диагностике", true) };
                    Logger.LogError(exception, "Diagnostic error");
                }

                return result;
            }
        }

        private Task DenyConfirmAsync()
        {
            return ExecuteLockableOperationAsync(lockedEntity => WebClient.ExecuteApiRequestAsync(new DenyConfirmServiceRequest(lockedEntity.Id)));
        }

        private Task AcceptConfirmAsync()
        {
            return ExecuteLockableOperationAsync(lockedEntity => WebClient.ExecuteApiRequestAsync(new AcceptConfirmServiceRequest(lockedEntity.Id)));
        }

        private string GetLocationString()
        {
            string location = string.Empty;

            if (Model.Location.HasValue)
            {
                location = Dictionaries.GetItemById<ServiceRequestLocation>(Model.Location.Value).Name;

                if (!string.IsNullOrWhiteSpace(Model.LocationText))
                {
                    location = $"{location} ({Model.LocationText})";
                }
            }

            return location;
        }

        private void OpenRequirementOnHistory()
        {
            SelectedTabIndex = 6;
            HistoryActiveFilterString = $"[{nameof(AuditEntry.PropertyName)}] IN ('Требование')";
        }

        private void ShowInvoice(int? invoiceId)
        {
            Messenger.Send(new InvoiceEditViewMessage(invoiceId!.Value));
        }

        private bool CanShowInvoice(int? invoiceId)
        {
            return invoiceId.HasValue;
        }

        private Task PrintAktAsync()
        {
            return ServiceRequestPrinter.PrintAsync(Model);
        }

        private async Task PrintSerialNumberAsync()
        {
            PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();

            PrinterSettingsInfo printerSettings = printingSettings.Barcode;

            if (printerSettings != null)
            {
                OurServiceBarcode barcode = new OurServiceBarcode(Model.Id);

                IReport report = new SerialNumberReport
                {
                    DataSource = new List<SerialNumberReportData>
                    {
                        new SerialNumberReportData(barcode.ServiceRequestId, barcode.BarcodeText)
                    }
                };

                PrintReportRequest request = new PrintReportRequest(report, false, printerSettings.Name, printerSettings.PaperSource);

                await Mediator.Send(request);
            }
            else
            {
                MessageFacadeService.ShowNotificationError("Сначала задайте принтеры в настройках");
            }
        }

        private void OnServiceRequestMessage(ServiceRequestMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:

                    if (Model != null)
                    {
                        Model.WarrantyRemoved = message.Entity.WarrantyRemoved;
                    }

                    break;
            }
        }

        private async Task PrintDefectivenessActAsync()
        {
            try
            {
                SubdivisionDto subdivision = await WebClient.ExecuteApiRequestAsync(new QuerySubdivision(Model.SubdivisionId));

                ServiceRequestDefectivenessReportData reportData = new ServiceRequestDefectivenessReportData(
                    WebClient.AuthenticatedEmployee.Name,
                    Model.ReceivedOn,
                    Model.PurchasedOn,
                    subdivision.Organization.Name,
                    Model.ProductFullName,
                    Model.SerialNumber,
                    Model.StatedDefect,
                    Model.CompletenessComment);

                IReport report = new ServiceRequestDefectivenessReport { DataSource = new[] { reportData } };

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
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to print");
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private async Task PrintReturnReportAsync()
        {
            try
            {
                OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(Model.OrderId));

                OrderProductDto orderProduct = order.Products.FirstOrDefault(x => x.Product.Id == Model.ProductId);

                if (orderProduct == null)
                {
                    MessageFacadeService.ShowNotificationWarning("Товар не найден в заказе");
                    return;
                }

                ServiceRequestReturnReportData reportData = new ServiceRequestReturnReportData(Model.Id, Model.CreatedOn, Model.Fio, Model.Phone, orderProduct.Product.NameFullUkr, orderProduct.PriceOut);

                IReport report = new ServiceRequestReturnReport { DataSource = new[] { reportData } };

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
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to print");
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private async Task PrintReturnProtocolReportAsync()
        {
            if (Model.Requirement == ServiceRequestRequirement.TradeIn)
            {
                MessageFacadeService.ShowNotificationError("Документ не доступен для требования Trade-In");
                return;
            }

            try
            {
                OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(Model.OrderId));

                OrderProductDto orderProduct = order.Products.FirstOrDefault(x => x.Product.Id == Model.ProductId);

                if (orderProduct == null)
                {
                    MessageFacadeService.ShowNotificationWarning("Товар не найден в заказе");
                    return;
                }

                string fio = string.IsNullOrEmpty(Model.Requisites?.FirstName) || string.IsNullOrEmpty(Model.Requisites?.MiddleName)
                    ? order.Fio
                    : $"{Model.Requisites.LastName} {Model.Requisites.FirstName} {Model.Requisites.MiddleName}";

                StatementReturnRefundData data = new StatementReturnRefundData(fio, order.Phone, order.Email, order.CreatedOn, orderProduct.Product.Name, Model.Requisites?.Iban, Model.Requisites?.Inn, Model.Requisites?.CardNumber);

                StatementReturnRefundReport report = new StatementReturnRefundReport() { DataSource = new[] { data } };

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
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to print");
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private async Task PrintWarrantyCardAsync()
        {
            try
            {
                OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(Model.OrderId));

                string serialNumber = string.Empty;

                if (!string.IsNullOrWhiteSpace(Model.SerialNumber) && !Model.SerialNumber.StartsWith("SR-", true, CultureInfo.InvariantCulture))
                {
                    serialNumber = Model.SerialNumber;
                }

                IDictionary<int, string> warranties = Dictionaries.GetItems<Warranty>().ToDictionary(x => x.Id, x => x.NameUa);

                IReport report = await OrderReportBuilder.BuildProductWarrantyCardReportAsync(
                    order,
                    Model.ProductId,
                    serialNumber,
                    warranties,
                    true);

                PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

                PrintReportRequest printRequest = printSettings?.WarrantyCard != null
                    ? new PrintReportRequest(report, true, printSettings.WarrantyCard.Name, printSettings.WarrantyCard.PaperSource)
                    : new PrintReportRequest(report, true);

                await Mediator.Send(printRequest);
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to print");
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private async Task PrintIssuanceCertificateAsync()
        {
            var serviceRepair = Repairs.MaxBy(x => x.CreatedOn);

            await ServiceRequestIssuanceCertificateReport.PrintAsync(
                Model,
                serviceRepair,
                ServiceCenters.FirstOrDefault(x => x.Id == serviceRepair?.ServiceCenterId),
                WebClient);
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChangedInternal(sender, e);
            switch (e.PropertyName)
            {
                case nameof(ServiceRequestViewItem.CarryOut):
                    Model.DeliveryDataOut = null;
                    Model.SendTo = GetSendTo();
                    break;
                case nameof(ServiceRequestViewItem.WarehouseInId):
                    HandleWarehouseChanged();
                    break;
                case nameof(ServiceRequestViewItem.CityId):
                    Model.DeliveryDataOut = null;
                    Model.SendTo = GetSendTo();
                    break;
                case nameof(ServiceRequestViewItem.DeliveryDataOut):
                    Model.SendTo = GetSendTo();
                    break;
            }
        }

        private void HandleWarehouseChanged()
        {
            int? cityId = null;

            if (Model.CarryIn != null)
            {
                cityId = WarehouseIn?.CityId ?? ModelOriginal.CityId;
            }

            Model.CityId = cityId;
            ModelOriginal.CityId = cityId;

            HandleCityChangedCommand.Execute(null);
        }

        private void HandleCarryInChanged()
        {
            ReadOnlyObservableCollection<WarehouseDto> warehousesByCarryType = null;

            if (Model.CarryIn != null)
            {
                warehousesByCarryType = AllWarehouses
                    .Where(x => WarehouseIn?.Id == x.Id || x.Active == 1)
                    .ToReadOnlyObservableCollection();
            }

            RaisePropertyChanged(nameof(WarehouseIn));

            Warehouses = warehousesByCarryType;
        }

        private async Task HandleCityChangedAsync()
        {
            ReadOnlyObservableCollection<NewPostWarehouseViewItem> npWarehouses = null;

            if (Model.CityId.HasValue)
            {
                try
                {
                    List<NpWarehouseDto> cityWarehouses = await WebClient
                        .ExecuteApiRequestAsync(new QueryNpWarehouses(Model.CityId.Value));

                    npWarehouses = cityWarehouses
                        .Where(x => x.Active)
                        .Select(x => Mapper.Map<NewPostWarehouseViewItem>(x))
                        .ToReadOnlyObservableCollection();
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                    MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                }
            }

            NpWarehouses = npWarehouses;

            Model.SendTo = GetSendTo();
        }

        private async Task PrintTrackNumberAsync(string trackNumber)
        {
            if (Model.CarryIn != null)
            {
                ITrackNumberProvider trackNumberProvider = Model.CarryIn.GetTrackNumberProvider();

                await trackNumberProvider.PrintAsync(trackNumber, true);
            }
        }

        private void RefundUpdateRequisites(RefundUpdateRequisitesMessage message)
        {
            if (message.DocumentId != Model.Id || Model.EmployeeLockId is null)
            {
                return;
            }

            Model.Requisites ??= new RequisitesViewItem();

            Model.Requisites.FirstName = message.FirstName;
            Model.Requisites.LastName = message.LastName;
            Model.Requisites.MiddleName = message.MiddleName;
            Model.Requisites.Iban = message.Iban;
            Model.Requisites.CardNumber = message.CardNumber;
            Model.Requisites.Inn = message.Inn;
        }

        private void ShowDocumentsBotQr()
        {
            QrCodeParameter parameter = new QrCodeParameter(DocumentsBotHelper.GetUrl(Model.Id, Entity.ServiceRequestId, Dictionaries, _telegramBotOptions), "Telegram бот");

            DialogDocumentManagerService.ShowView<QrCodeViewModel>(parameter, this);
        }

        private async Task PrintChequeAsync()
        {
            if (Model.LegalEntityId == null)
            {
                MessageFacadeService.ShowNotificationError("В заказе сервисной заявки не заполнено юр. лицо");
                return;
            }

            LegalEntityDto legalEntity = await WebClient.ExecuteApiRequestAsync(new QueryLegalEntity(Model.LegalEntityId.Value));

            if (legalEntity.FiscalCashboxId == null)
            {
                MessageFacadeService.ShowNotificationError("В юр. лице заказа не заполнена фискальная каса");
                return;
            }

            int cashboxId = legalEntity.FiscalCashboxId.Value;

            await PrintOrderOnFiscalRegistrarAsync(cashboxId);
        }

        private async Task PrintOrderOnFiscalRegistrarAsync(int cashboxId)
        {
            Result resultSentRroCheck = await RroPrintHelper.SentCheckAsync(Model.FiscalId, Model.Phone, Model.Email, cashboxId, this, true);

            if (resultSentRroCheck.IsSuccess)
            {
                await WebClient.ExecuteApiRequestAsync(new ConfirmServiceRequestOnFiscalRegistrar(Model.Id, Model.FiscalId, true));
            }
        }

        private void WarehouseInChanged()
        {
            Model.WarehouseInId = WarehouseIn?.Id;

            if (Model.CarryOut?.Id != CarryType.PickupId)
            {
                return;
            }

            if (WarehouseIn != null)
            {
                Model.DeliveryDataOut = new DeliveryDataDto
                {
                    CityId = WarehouseIn.CityId.ToString(),
                    PlaceId = WarehouseIn.Id.ToString(),
                    Street = null,
                    House = null,
                    Flat = null,
                    Extra = null,
                    MaxAllowedWeight = WarehouseIn.MaxPackageWeight,
                    Address = WarehouseIn.Address,
                    AddressUkr = WarehouseIn.AddressUa,
                    AddressEn = WarehouseIn.AddressEn
                };
            }
            else
            {
                Model.DeliveryDataOut = null;
            }
        }

        private void SelectDeliveryAddress()
        {
            if (Model.CityId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите город");
                return;
            }

            if (Model.CarryOut == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите способ доставки");
                return;
            }

            CarryType carryType = Dictionaries.GetItemById<CarryType>(Model.CarryOut.Id);

            SelectDeliveryDataParameter parameter = new SelectDeliveryDataParameter(
                carryType.Id,
                Model.CityId.Value,
                Model.DeliveryDataOut?.Address,
                Model.DeliveryDataOut);

            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)App.Current.MainWindow.DataContext;

            if (carryType.Kind == CarryTypeKind.Courier)
            {
                SelectDeliveryAddressViewModel viewModel = mainWindowViewModel.WorkspaceViewModel.DialogDocumentManagerService.ShowView<SelectDeliveryAddressViewModel>(
                    parameter,
                    mainWindowViewModel);

                if (viewModel.IsOk)
                {
                    Model.DeliveryDataOut = viewModel.GetDeliveryServiceData();
                }
            }
            else if (carryType.Kind == CarryTypeKind.Pickup)
            {
                SelectDeliveryWarehouseViewModel viewModel = mainWindowViewModel.WorkspaceViewModel.SizeableDialogDocumentManagerService.ShowView<SelectDeliveryWarehouseViewModel>(parameter, mainWindowViewModel);

                if (viewModel.IsOk)
                {
                    Model.DeliveryDataOut = viewModel.GetDeliveryServiceData();
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            Model.SendTo = GetSendTo();
        }

        private string GetSendTo()
        {
            return string.Join(" ", GetStringParts());

            IEnumerable<string> GetStringParts()
            {
                yield return Model.CarryOut?.Name;

                yield return Cities.FirstOrDefault(x => x.Id == Model.CityId).DisplayValue;

                yield return Model.DeliveryDataOut?.ToString();
            }
        }
    }
}