using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Business.Order;
using Telemart.Client.Common.Asterisk;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Navigation;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Equipment;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Asterisk;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.Requests.Features.Call.Actions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.Requests.Features.WorkPlace;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Oktell;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.AdditionalServiceProduct;
using Telemart.Client.ViewModels.AssemblyService;
using Telemart.Client.ViewModels.Backlog;
using Telemart.Client.ViewModels.Cashbox;
using Telemart.Client.ViewModels.Cities;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Common.Product;
using Telemart.Client.ViewModels.Complaint;
using Telemart.Client.ViewModels.Dialogs.Call;
using Telemart.Client.ViewModels.Dialogs.PdfPreview;
using Telemart.Client.ViewModels.Directories.Contractor;
using Telemart.Client.ViewModels.Directories.Contractor.ParserSettings;
using Telemart.Client.ViewModels.Directories.Employee;
using Telemart.Client.ViewModels.Directories.Organization;
using Telemart.Client.ViewModels.Discussions;
using Telemart.Client.ViewModels.History.Phone;
using Telemart.Client.ViewModels.History.SerialNumber;
using Telemart.Client.ViewModels.ModuleAnalytics;
using Telemart.Client.ViewModels.Money.Refund;
using Telemart.Client.ViewModels.Novaposhta.NovaposhtaBill;
using Telemart.Client.ViewModels.Reporting;
using Telemart.Client.ViewModels.Service.ServiceCenters;
using Telemart.Client.ViewModels.Service.ServiceInvoices;
using Telemart.Client.ViewModels.Service.ServiceProducts;
using Telemart.Client.ViewModels.Service.ServiceRepairs;
using Telemart.Client.ViewModels.Service.ServiceRequests;
using Telemart.Client.ViewModels.Service.ServiceRequests.Create;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Store.Call;
using Telemart.Client.ViewModels.Store.Invoice;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.ReturnInvoice;
using Telemart.Client.ViewModels.SupplierBill;
using Telemart.Client.ViewModels.TradeIn;
using Telemart.Client.ViewModels.Validation;
using Telemart.Client.ViewModels.Warehouse;
using Telemart.Client.ViewModels.Warehouse.Movement;
using Telemart.Client.Views;
using Telemart.Client.Views.Reporting;
using Telemart.Client.Views.Showcase;

namespace Telemart.Client.ViewModels
{
    internal sealed class WorkspaceViewModel : ViewModelBase
    {
        private readonly ConcurrentDictionary<string, object> openedDocuments = new ConcurrentDictionary<string, object>();

        public WorkspaceViewModel(
            IMessenger messenger,
            IMessageFacadeService messageFacadeService,
            OrderViewProvider orderViewProvider,
            IWebClient webClient,
            IDictionaries dictionaries,
            IMapper mapper,
            ICallServiceClient callServiceClient,
            ILogger<WorkspaceViewModel> logger,
            IEquipmentSettingsStore equipmentSettingsStore,
            CallTrackOptions callTrackOptions)
            : this(equipmentSettingsStore)
        {
            Dictionaries = dictionaries;
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            OrderViewProvider = orderViewProvider ?? throw new ArgumentNullException(nameof(orderViewProvider));
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Logger = logger;
            CallTrackOptions = callTrackOptions;

            CallDialog = new CallDialogViewModel(WebClient, Dictionaries, MessageFacadeService, callServiceClient, Messenger);

            Messenger.Register<KeyEventMessage>(this, RedirectKeyEventMessage);
            Messenger.Register<OrderEditViewMessage>(this, OnOrderEdit);
            Messenger.Register<CreateReturnInvoiceMessage>(this, OnCreateReturnInvoice);
            Messenger.Register<OrderEditReopenViewMessage>(this, OnOrderEditReopen);
            Messenger.Register<OrderCreateViewMessage>(this, OnOrderCreate);
            Messenger.Register<OrderCopyViewMessage>(this, OnOrderCopy);
            Messenger.Register<InvoiceEditViewMessage>(this, OnInvoiceEdit);
            Messenger.Register<ServiceRequestCreateViewMessage>(this, OnServiceRequestCreate);
            Messenger.Register<ServiceRequestCreateManyViewMessage>(this, OnServiceRequestCreateMany);
            Messenger.Register<ServiceRequestCreateFromOrderViewMessage>(this, OnServiceRequestFromOrder);
            Messenger.Register<ServiceRequestCreateFromPhoneHistoryMessage>(this, OnServiceRequestFromPnoneHistory);
            Messenger.Register<ServiceRequestViewMessage>(this, OnServiceRequestEdit);
            Messenger.Register<ServiceRepairViewMessage>(this, OnServiceRepairEdit);
            Messenger.Register<ContractorViewMessage>(this, OnContractorMessage);
            Messenger.Register<ParserSettingsViewMessage>(this, OnParserSettingsMessage);
            Messenger.Register<ShowReportMessage>(this, OnShowReport);
            Messenger.Register<CallViewMessage>(this, OnCallEdit);
            Messenger.Register<OutcomingCallViewMessage>(this, OnOutcomingCall);
            Messenger.Register<OrganizationViewMessage>(this, OnOrganizationEdit);
            Messenger.Register<MovementViewMessage>(this, OnMovementEdit);
            Messenger.Register<ServiceCenterViewMessage>(this, OnServiceCenterView);
            Messenger.Register<ServiceInvoiceViewMessage>(this, OnServiceInvoiceViewMessage);
            Messenger.Register<EmployeeViewMessage>(this, OnEmployeeViewMessage);
            Messenger.Register<ShowModuleMessage>(this, OnShowModuleMessage);
            Messenger.Register<RefundViewMessage>(this, OnRefundEdit);
            Messenger.Register<DiscussionViewMessage>(this, OnDiscussionEdit);
            Messenger.Register<TradeInViewMessage>(this, OnTradeInEdit);
            Messenger.Register<ReportViewMessage>(this, OnReportViewMessage);
            Messenger.Register<WikiHelpMessage>(this, OnWikiHelpOpen);
            Messenger.Register<ModuleAnalyticsParameter>(this, OnModuleAnalyticsParameter);
            Messenger.Register<TelewikiHelpMessage>(this, OnTelewikiHelpOpen);
            Messenger.Register<DocumentDiscussionsParameter>(this, OnDocumentDiscussionsParameter);
            Messenger.Register<ModuleAnalyticsPositionParameter>(this, OnModuleAnalyticsPosition);
            Messenger.Register<UpdateCurrencyRatesViewMessage>(this, OnUpdateCurrencyRatesViewMessage);
            Messenger.Register<WorkPlaceViewMessage>(this, OnWorkPlaceViewMessage);
            Messenger.Register<NpScanSheetViewMessage>(this, OnNpScanSheetViewMessage);
            Messenger.Register<ServiceProductViewMessage>(this, OnServiceProductViewMessage);
            Messenger.Register<SerialNumberHistoryViewMessage>(this, OnSerialNumberHistoryViewMessage);
            Messenger.Register<SupplierBillViewMessage>(this, OnSupplierBillViewMessage);
            Messenger.Register<NpBillDocumentViewMessage>(this, OnNpBillDocumentViewMessage);
            Messenger.Register<BacklogTaskViewMessage>(this, OnBacklogTaskViewMessage);
            Messenger.Register<ProductCardViewMessage>(this, OnProductCardViewMessage);
            Messenger.Register<ComplaintViewMessage>(this, OnComplaintViewMessage);
            Messenger.Register<PhoneHistoryViewMessage>(this, OnPhoneHistoryViewMessage);
            Messenger.Register<CityViewMessage>(this, OnCityViewMessage);
            Messenger.Register<CallDialogParameter>(this, OnCallDialogMessage);
            Messenger.Register<CallDialogEndMessage>(this, OnCallDialogEnd);
            Messenger.Register<PdfPreviewParameter>(this, OnShowPdfPreview);
            Messenger.Register<PasswordSendMessage>(this, OnPasswordSend);
            Messenger.Register<ReturnInvoiceEditViewMessage>(this, OnReturnInvoiceEditView);
            Messenger.Register<AdditionalServiceProductViewMessage>(this, OnAdditionalServiceProductView);
            Messenger.Register<WarehouseEditParameter>(this, OnWarehouseView);
            Messenger.Register<CashboxEditParameter>(this, OnCashboxView);
            Messenger.Register<AssemblyServiceViewMessage>(this, OnAssemblyServiceView);
            Messenger.Register<SendWikiCookiesMessage>(this, OnSendWikiCookies);
            Messenger.Register<ShowcaseHistoriesMessage>(this, OnShowcaseHistoriesOppen);
            Messenger.Register<SetUnavailableAsterisStatusMessage>(this, OnSetUnavailableAsterisStatus);
            Messenger.Register<EndAsteriskCallMessage>(this, OnEndAsteriskCall);
        }

        public WorkspaceViewModel(IEquipmentSettingsStore equipmentSettingsStore)
        {
            EquipmentSettingsStore = equipmentSettingsStore;
            ShowModuleCommand = new DelegateCommand<NavigationMenuItem>(ShowModule, x => x != null);
            ShowModuleSimultaneouslyCommand = new DelegateCommand<NavigationMenuItem>(ShowModuleSimultaneously, x => x != null);
            HandleLoadedCommand = new DelegateCommand(HandleLoaded);
            ShowWorkPlaceViewCommand = new AsyncCommand(ShowWorkPlaceViewInternalAsync);
        }

        #region Commands

        public IDelegateCommand HandleLoadedCommand { get; }

        public IDelegateCommand ShowModuleCommand { get; }

        public IDelegateCommand ShowModuleSimultaneouslyCommand { get; }

        public IAsyncCommand ShowWorkPlaceViewCommand { get; }

        #endregion

        public ObservableCollection<NavigationMenuGroup> NavGroups
        {
            get { return GetProperty(() => NavGroups); }
            set { SetProperty(() => NavGroups, value); }
        }

        public CallDialogViewModel CallDialog
        {
            get { return GetProperty(() => CallDialog); }
            private set { SetProperty(() => CallDialog, value); }
        }

        public bool IsVisibilityCallDialog
        {
            get { return GetProperty(() => IsVisibilityCallDialog); }
            set { SetProperty(() => IsVisibilityCallDialog, value); }
        }

        public ReadOnlyCollection<Cookie> Cookies
        {
            get { return GetProperty(() => Cookies); }
            private set { SetProperty(() => Cookies, value); }
        }

        public IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService");

        public IDocumentManagerService DocumentManagerService => GetService<IDocumentManagerService>();

        public IDocumentManagerService DefaultPositionDialogDocumentManagerService => GetService<IDocumentManagerService>("DefaultPositionDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public IDocumentManagerService NonModalSizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("NotModalSizeableDocumentManagerService");

        public IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService");

        public ViewModelBase ViewModel => this;

        private ILogger<WorkspaceViewModel> Logger { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IMessenger Messenger { get; }

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        private IMapper Mapper { get; }

        private CallTrackOptions CallTrackOptions { get; }

        private IEquipmentSettingsStore EquipmentSettingsStore { get; }

        private IDocumentManagerService NonModalDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalDialogDocumentManagerService");

        private IDocumentManagerService NotModalSizeableDocumentManagerService => GetService<IDocumentManagerService>("NotModalSizeableDocumentManagerService");

        private IDocumentManagerService NonModalWithLocationDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalWithLocationDialogDocumentManagerService");

        private IDocumentManagerService NonModalWithLocationSizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalWithLocationSizeableDialogDocumentManagerService");

        private IDocumentManagerService NonModalWithLocationMinimizedSizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalWithLocationMinimizedSizeableDialogDocumentManagerService");

        private IDocumentManagerService OrderDocumentManagerService => GetService<IDocumentManagerService>("OrderDocumentManagerService");

        private IDocumentManagerService NoBorderDialogDocumentManagerService => GetService<IDocumentManagerService>("NoBorderDialogDocumentManagerService");

        private IDialogService WizardDialogService => GetService<IDialogService>("WizardDialogService");

        private IDialogService CreateManyServiceRequestWizardDialogService => GetService<IDialogService>("CreateManyServiceRequestWizardDialogService");

        private OrderViewProvider OrderViewProvider { get; }

        private string UserPassword { get; set; }

        public bool CloseDocuments()
        {
            bool success = true;

            foreach (IDocument document in GetNonModalDocuments())
            {
                if (document.Content is IDocumentContent documentContent)
                {
                    CancelEventArgs args = new CancelEventArgs();

                    documentContent.OnClose(args);

                    if (args.Cancel)
                    {
                        success = false;
                        break;
                    }
                }

                document.Close();
            }

            DocumentManagerService.Documents.ToArray().ForEach(x => x.Close());

            return success;
        }

        public void HideDocuments()
        {
            foreach (IDocument document in GetNonModalDocuments())
            {
                document.Hide();
            }
        }

        public void ShowDocuments()
        {
            foreach (IDocument document in GetNonModalDocuments())
            {
                document.Show();
            }
        }

        public void ShowModuleInternal(NavigationMenuItem navItem, object parameter, bool canBeOpenedSimultaneously)
        {
            string prefix = navItem.GetViewType().Name;

            if (navItem.ViewModel != null)
            {
                prefix += $"_{navItem.ViewModel.GetType().Name}";
            }

            IDocument[] moduleDocuments = DocumentManagerService.Documents
                .Where(x => x.Id?.ToString()?.StartsWith(prefix) == true)
                .ToArray();

            if (moduleDocuments.Length == 0 || (canBeOpenedSimultaneously && moduleDocuments.Length < navItem.CountOfSimultaneouslyOpenTabs))
            {
                string documentId = $"{prefix}_{Guid.NewGuid():N}";
                IDocument document = FindDocumentByIdOrCreate(navItem, documentId, navItem.ViewModel, parameter);
                document.Show();
            }
            else
            {
                IDocument firstDocument = moduleDocuments.First();

                if (firstDocument == DocumentManagerService.ActiveDocument)
                {
                    moduleDocuments.Last().Show();
                }
                else
                {
                    firstDocument.Show();
                }
            }
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            List<NavigationMenuItem> menuItems = new List<NavigationMenuItem>
            {
                new NavigationMenuItem("Заказы", "cart", typeof(WorkspaceView), Array.Empty<Role>(), Array.Empty<BusinessOperation>(), 2),
                new NavigationMenuItem("Закупки", "lorry", typeof(WorkspaceView), Array.Empty<Role>(), Array.Empty<BusinessOperation>()),
                new NavigationMenuItem("Звонки", "headphone", typeof(WorkspaceView), Array.Empty<Role>(), Array.Empty<BusinessOperation>(), 2),
                new NavigationMenuItem("Прайс-лист", "table_excel", typeof(WorkspaceView), Array.Empty<Role>(), Array.Empty<BusinessOperation>()),
                new NavigationMenuItem("Ценники", "three_tags", typeof(WorkspaceView), Array.Empty<Role>(), Array.Empty<BusinessOperation>()),
                new NavigationMenuItem("Цены", "coins", typeof(WorkspaceView), Array.Empty<Role>(), Array.Empty<BusinessOperation>(), 2)
            };

            NavGroups = new ObservableCollection<NavigationMenuGroup>
            {
                new NavigationMenuGroup("Магазин", menuItems)
            };
        }

        private IEnumerable<IDocument> GetNonModalDocuments()
        {
            foreach (IDocumentManagerService nonModalDocumentManagerService in GetNonModalDocumentManagerServices())
            {
                foreach (IDocument document in nonModalDocumentManagerService.Documents.ToArray())
                {
                    yield return document;
                }
            }
        }

        private IEnumerable<IDocumentManagerService> GetNonModalDocumentManagerServices()
        {
            yield return OrderDocumentManagerService;
            yield return NonModalDialogDocumentManagerService;
            yield return NotModalSizeableDocumentManagerService;
            yield return NonModalWithLocationDialogDocumentManagerService;
        }

        private IDocument FindDocumentByIdOrCreate(NavigationMenuItem navItem, string documentId, object viewModel, object parameter)
        {
            IDocumentManagerService documentManagerService = navItem.GetNavigationMode() switch
            {
                NavigationMode.Tab => DocumentManagerService,
                NavigationMode.Dialog => DialogDocumentManagerService,
                NavigationMode.SizeableDialog => SizeableDialogDocumentManagerService,
                NavigationMode.NotModalSizeableDialog => NotModalSizeableDocumentManagerService,
                _ => throw new NotSupportedException(),
            };

            return FindDocumentByIdOrCreate(documentManagerService, navItem, documentId, viewModel, parameter);
        }

        private IDocument FindDocumentByIdOrCreate(IDocumentManagerService documentManagerService, NavigationMenuItem navItem, string documentId, object viewModel, object parameter)
        {
            return documentManagerService.FindDocumentByIdOrCreate(
                documentId,
                _ =>
                {
                    IDocument document = documentManagerService.CreateDocument(navItem.GetViewType().Name, viewModel, parameter, this);
                    document.Id = documentId;
                    document.Title = new ModuleHeader(navItem.Image, navItem.Header, navItem.HasImage);
                    document.DestroyOnClose = true;

                    return document;
                });
        }

        private void HandleLoaded()
        {
        }

        private void OnWikiHelpOpen(WikiHelpMessage message)
        {
            NavigationMenuItem navItem = new NavigationMenuItem(
                message.Title,
                string.Empty,
                typeof(WatsNewView),
                Array.Empty<Role>(),
                Array.Empty<BusinessOperation>(),
                100);

            ShowModuleInternal(navItem, message.RelativeUrl, true);
        }

        private void OnModuleAnalyticsParameter(ModuleAnalyticsParameter message)
        {
            if (message.PositionId == ModuleAnalyticsPosition.Maximized.Id)
            {
                NonModalWithLocationSizeableDialogDocumentManagerService.ShowEditorView<ModuleAnalyticsViewModel>(message.EntityId, message, message.ParentViewModel ?? this);
            }

            if (message.PositionId == ModuleAnalyticsPosition.Minimized.Id)
            {
                NonModalWithLocationMinimizedSizeableDialogDocumentManagerService.ShowEditorView<ModuleAnalyticsViewModel>(message.EntityId, message, message.ParentViewModel ?? this);
            }
        }

        private void OnModuleAnalyticsPosition(ModuleAnalyticsPositionParameter parameter)
        {
            DialogDocumentManagerService.ShowView<ModuleAnalyticsPositionViewModel>(parameter, parameter.ParentViewModel ?? this);
        }

        private void OnDocumentDiscussionsParameter(DocumentDiscussionsParameter message)
        {
            DialogDocumentManagerService.ShowView<DocumentDiscussionsViewModel>(message, this);
        }

        private void OnTelewikiHelpOpen(TelewikiHelpMessage message)
        {
            NavigationMenuItem navItem = new NavigationMenuItem(
                message.ModeleName,
                "bookshelf",
                typeof(WatsNewView),
                Array.Empty<Role>(),
                Array.Empty<BusinessOperation>(),
                100);

            message.TelewikiParameter.SetUserPassword(UserPassword, true);

            ShowModuleInternal(navItem, message.TelewikiParameter, true);
        }

        private void OnCallEdit(CallViewMessage message)
        {
            NonModalDialogDocumentManagerService.ShowEditorView<CallViewModel>(message.Id, message, this);
        }

        private void OnOutcomingCall(OutcomingCallViewMessage message)
        {
            Task.Factory
                .StartNew(
                    () => OnOutcomingCallAsync(message),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext())
                .ContinueWith(
                    t =>
                    {
                        if (t.Status == TaskStatus.Faulted)
                        {
                            MessageFacadeService.ShowNotificationError("Ошибка при обработке данных");
                        }
                    });
        }

        private async Task OnOutcomingCallAsync(OutcomingCallViewMessage message)
        {
            PagedResult<EmployeeDto> employeesPagedResult = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);
            IReadOnlyDictionary<int, string> employees = employeesPagedResult.Data.ToDictionary(x => x.Id, x => x.Name);

            CallViewItem[] callsToProcess = CheckCalls(message.Calls).ToArray();

            for (int i = 0; i < callsToProcess.Length; i++)
            {
                CallViewItem call = callsToProcess[i];

                try
                {
                    await WebClient.ExecuteApiRequestAsync(new CanCall(call.Id));

                    if (call.CallState.Id == CallState.New.Id)
                    {
                        LockResponse<CallDto> lockResponse = await WebClient.ExecuteApiRequestAsync(new LockCall(call.Id));

                        Messenger.Send(new CallMessage(lockResponse.Dto, MessageType.Changed));

                        if (lockResponse.Success)
                        {
                            CallViewItem viewItem = Mapper.Map<CallViewItem>(lockResponse.Dto);

                            NonModalWithLocationDialogDocumentManagerService.ShowEditorView<OutcomingCallViewModel>(viewItem.Id, new object[] { viewItem, i + 1, callsToProcess.Length }, this);
                        }
                        else
                        {
                            MessageFacadeService.ShowNotificationWarning($"Звонок №{call.Id} уже заблокирован пользователем {lockResponse.Dto.EmployeeLock.Name}");
                        }
                    }
                    else
                    {
                        NonModalWithLocationDialogDocumentManagerService.ShowEditorView<OutcomingCallViewModel>(call.Id, new object[] { call, i + 1, callsToProcess.Length }, this);
                    }
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
                    Logger.LogError(exception, "Failed to block call");
                    MessageFacadeService.ShowNotificationError($"Ошибка при блокировании звонка №{call.Id}");
                }
            }

            IEnumerable<CallViewItem> CheckCalls(CallViewItem[] calls)
            {
                foreach (CallViewItem item in calls)
                {
                    if (item.CallState == CallState.Canceled)
                    {
                        MessageFacadeService.ShowNotificationWarning($"Звонок №{item.Id} уже отменен");
                        continue;
                    }

                    if (item.EmployeeCompletedById.HasValue)
                    {
                        string completedBy = employees.GetValueOrDefault(item.EmployeeCompletedById.Value);
                        MessageFacadeService.ShowNotificationWarning($"{completedBy} совершил(а) звонок №{item.Id} {item.CompletedOn?.ToString(DateFormattingRules.FullDateTimeFormat)}");
                    }

                    if (item.EmployeeRespId.HasValue && item.EmployeeRespId.Value != WebClient.AuthenticatedEmployee.Id)
                    {
                        string responsible = employees.GetValueOrDefault(item.EmployeeRespId.Value);

                        if (!MessageFacadeService.Confirm($"За звонок №{item.Id} ответственен сотрудник \"{responsible}\", продолжить?"))
                        {
                            continue;
                        }
                    }

                    yield return item;
                }
            }
        }

        private void OnRefundEdit(RefundViewMessage message)
        {
            NonModalDialogDocumentManagerService.ShowEditorView<RefundViewModel>(message.Id, message, this);
        }

        private void OnDiscussionEdit(DiscussionViewMessage message)
        {
            SizeableDialogDocumentManagerService.ShowView<DiscussionViewModel>(
                new DiscussionParameter(message.Id, 0, 0),
                this);
        }

        private void OnTradeInEdit(TradeInViewMessage message)
        {
            NonModalDialogDocumentManagerService.ShowEditorView<TradeInEditViewModel>(message.Id, message, this);
        }

        private void OnParserSettingsMessage(ParserSettingsViewMessage message)
        {
            SizeableDialogDocumentManagerService.ShowView<ParserSettingsViewModel>(new ParserSettingsParameter(message.Id, 0), this);
        }

        private void OnContractorMessage(ContractorViewMessage message)
        {
            if (message.IsNew)
            {
                DialogDocumentManagerService.ShowEditorView<CreateContractorViewModel>(message.Id, message, this);
            }
            else
            {
                NotModalSizeableDocumentManagerService.ShowEditorView<ContractorViewModel>(message.Id, message, this);
            }
        }

        private void OnInvoiceEdit(InvoiceEditViewMessage message)
        {
            NotModalSizeableDocumentManagerService.ShowEditorView<InvoiceViewModel>(message.Id, message.Id, this);
        }

        private void OnMovementEdit(MovementViewMessage message)
        {
            NotModalSizeableDocumentManagerService.ShowEditorView<MovementViewModel>(message.Id, message, this);
        }

        private void OnServiceCenterView(ServiceCenterViewMessage message)
        {
            NonModalDialogDocumentManagerService.ShowEditorView<ServiceCenterViewModel>(message.Id, message, this);
        }

        private void OnServiceInvoiceViewMessage(ServiceInvoiceViewMessage message)
        {
            NotModalSizeableDocumentManagerService.ShowEditorView<ServiceInvoiceViewModel>(message.Id, message, this);
        }

        private void OnReportViewMessage(ReportViewMessage message)
        {
            SizeableDialogDocumentManagerService.ShowEditorView<ReportEditorViewModel>(message.Id, message, this);
        }

        private void OnOrderCopy(OrderCopyViewMessage message)
        {
            ShowViewAsync<OrderViewModel, OnOrderBeforeEditMessage>(
                OrderDocumentManagerService,
                0,
                () => OrderViewProvider.AddOrderAsync(message),
                OrderViewProvider.OrderViewName);
        }

        private void OnOrderCreate(OrderCreateViewMessage message)
        {
            ShowViewAsync<OrderViewModel, OnOrderBeforeEditMessage>(
                OrderDocumentManagerService,
                0,
                () => OrderViewProvider.AddOrderAsync(message),
                OrderViewProvider.OrderViewName);
        }

        private void OnOrderEdit(OrderEditViewMessage message)
        {
            ShowViewAsync<OrderViewModel, OnOrderBeforeEditMessage>(
                OrderDocumentManagerService,
                message.Id,
                () => OrderViewProvider.EditOrderAsync(message.Id, message.EditMode),
                OrderViewProvider.OrderViewName);
        }

        private void OnCreateReturnInvoice(CreateReturnInvoiceMessage message)
        {
            NonModalDialogDocumentManagerService.ShowView<CreateReturnInvoiceViewModel>(new CreateReturnInvoiceParameter(message.InvoiceDto), this);
        }

        private void OnOrderEditReopen(OrderEditReopenViewMessage message)
        {
            int documentIdToClose = message.IsAdd
                ? 0
                : message.OrderId;

            string documentKey = $"{nameof(OrderViewModel)}_{documentIdToClose.ToString(CultureInfo.InvariantCulture)}";

            IDocument document = OrderDocumentManagerService.FindDocumentById(documentKey);

            document?.Close();

            ShowViewAsync<OrderViewModel, OnOrderBeforeEditMessage>(
                OrderDocumentManagerService,
                message.OrderId,
                () => OrderViewProvider.EditOrderAsync(message.OrderId, false),
                OrderViewProvider.OrderViewName);
        }

        private void OnOrganizationEdit(OrganizationViewMessage message)
        {
            NonModalDialogDocumentManagerService.ShowEditorView<OrganizationViewModel>(message.Id, message, this);
        }

        private void OnServiceRequestCreate(ServiceRequestCreateViewMessage message)
        {
            CreateServiceRequestModel model = new CreateServiceRequestModel(Dictionaries, Mapper);

            WizardDialogViewModel<CreateServiceRequestModel> wizardDialogViewModel = new WizardDialogViewModel<CreateServiceRequestModel>(
                typeof(SelectOrderPageViewModel),
                model,
                this);

            WizardDialogService.ShowDialog(MessageButton.OKCancel, "Создание заявки", wizardDialogViewModel);
        }

        private void OnServiceRequestCreateMany(ServiceRequestCreateManyViewMessage message)
        {
            CreateManyServiceRequestModel model = new CreateManyServiceRequestModel(Dictionaries, WebClient, Mapper, MessageFacadeService);

            WizardDialogViewModel<CreateManyServiceRequestModel> wizardDialogViewModel = new WizardDialogViewModel<CreateManyServiceRequestModel>(
                typeof(CreateManyServiceRequestStep1ViewModel),
                model,
                this);

            CreateManyServiceRequestWizardDialogService.ShowDialog(MessageButton.OKCancel, "Создание заявок", wizardDialogViewModel);
        }

        private void OnServiceRequestFromOrder(ServiceRequestCreateFromOrderViewMessage message)
        {
            try
            {
                OrderDto order = WebClient.ExecuteApiRequest(new QueryOrder(message.OrderId));
                ContractorDto orderContractor = WebClient.ExecuteApiRequest(new QueryContractor(order.ClientId));
                IReadOnlyCollection<ProductAttributesDto> attributes = WebClient.ExecuteApiRequest(new QueryOrderProductAttributes(order.Id));
                List<CashboxDto> cashboxes = WebClient.ExecuteApiRequest(new QueryCashboxes(), true);

                CreateServiceRequestModel model = new CreateServiceRequestModel(Dictionaries, Mapper);

                model.SetOrderData(Dictionaries, order, orderContractor, attributes, cashboxes);
                model.ProductId = message.ProductId;
                model.SetSerials(message.ProductId);

                WizardDialogViewModel<CreateServiceRequestModel> wizardDialogViewModel = new WizardDialogViewModel<CreateServiceRequestModel>(
                    typeof(SelectProductPageViewModel),
                    model,
                    this);

                WizardDialogService.ShowDialog(MessageButton.OKCancel, "Создание заявки", wizardDialogViewModel);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void OnServiceRequestFromPnoneHistory(ServiceRequestCreateFromPhoneHistoryMessage message)
        {
            try
            {
                OrderDto order = WebClient.ExecuteApiRequest(new QueryOrder(message.OrderId));
                ContractorDto orderContractor = WebClient.ExecuteApiRequest(new QueryContractor(order.ClientId));
                IReadOnlyCollection<ProductAttributesDto> attributes = WebClient.ExecuteApiRequest(new QueryOrderProductAttributes(order.Id));
                List<CashboxDto> cashboxes = WebClient.ExecuteApiRequest(new QueryCashboxes(), true);

                CreateServiceRequestModel model = new CreateServiceRequestModel(Dictionaries, Mapper);

                model.SetOrderData(Dictionaries, order, orderContractor, attributes, cashboxes);

                WizardDialogViewModel<CreateServiceRequestModel> wizardDialogViewModel = new WizardDialogViewModel<CreateServiceRequestModel>(
                    typeof(SelectProductPageViewModel),
                    model,
                    this);

                WizardDialogService.ShowDialog(MessageButton.OKCancel, "Создание заявки", wizardDialogViewModel);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void OnServiceRequestEdit(ServiceRequestViewMessage message)
        {
            NotModalSizeableDocumentManagerService.ShowEditorView<ServiceRequestViewModel>(message.Id, message, this);
        }

        private void OnServiceRepairEdit(ServiceRepairViewMessage message)
        {
            NonModalDialogDocumentManagerService.ShowEditorView<ServiceRepairViewModel>(message.Id, message, this);
        }

        private void OnEmployeeViewMessage(EmployeeViewMessage message)
        {
            SizeableDialogDocumentManagerService.ShowEditorView<UpdateEmployeeViewModel>(message.Id, message, this);
        }

        private void OnShowModuleMessage(ShowModuleMessage message)
        {
            NavigationMenuItem item = NavGroups
                .SelectMany(x => x.NavItems.Where(y => y.GetViewType() == message.ViewType))
                .FirstOrDefault();

            if (item != null)
            {
                ShowModule(item);
            }
        }

        private void OnUpdateCurrencyRatesViewMessage(UpdateCurrencyRatesViewMessage message)
        {
            DialogDocumentManagerService.ShowView<UpdateCurrencyRatesViewModel>(null, this);
        }

        private void OnWorkPlaceViewMessage(WorkPlaceViewMessage message)
        {
            try
            {
                ShowWorkPlaceViewCommand.Execute(null);
            }
            catch (Exception e)
            {
                MessageFacadeService.ShowNotificationError("Непредвиденна ошибка");
                Logger.LogError(e, "Failed to set work place");
            }
        }

        private void OnNpScanSheetViewMessage(NpScanSheetViewMessage message)
        {
            DialogDocumentManagerService.ShowView<NpScanSheetViewModel>(message, this);
        }

        private void OnServiceProductViewMessage(ServiceProductViewMessage message)
        {
            NonModalDialogDocumentManagerService.ShowEditorView<ServiceProductViewModel>(message.Id, message, this);
        }

        private void OnSerialNumberHistoryViewMessage(SerialNumberHistoryViewMessage message)
        {
            NonModalDialogDocumentManagerService.ShowEditorView<SerialNumberHistoryViewModel>(message.Id, message.SerialNumber, this);
        }

        private void OnSupplierBillViewMessage(SupplierBillViewMessage message)
        {
            NotModalSizeableDocumentManagerService.ShowEditorView<SupplierBillViewModel>(message.Id, message, this);
        }

        private void OnNpBillDocumentViewMessage(NpBillDocumentViewMessage message)
        {
            DialogDocumentManagerService.ShowEditorView<NovaposhtaBillDocumentViewModel>(message.BillId, message, this);
        }

        private void OnBacklogTaskViewMessage(BacklogTaskViewMessage message)
        {
            DialogDocumentManagerService.ShowView<BacklogTaskEditViewModel>(message, this);
        }

        private void OnComplaintViewMessage(ComplaintViewMessage message)
        {
            NonModalDialogDocumentManagerService.ShowEditorView<ComplaintViewModel>(message.Id, message, this);
        }

        private void OnProductCardViewMessage(ProductCardViewMessage message)
        {
            SizeableDialogDocumentManagerService.ShowView<ProductCardViewModel>(message, this);
        }

        private void OnPhoneHistoryViewMessage(PhoneHistoryViewMessage message)
        {
            ISupportParameter viewModel = NotModalSizeableDocumentManagerService.ShowView<PhoneHistoryViewModel>(null, this);

            viewModel.Parameter = message.Phones;
        }

        private void OnCityViewMessage(CityViewMessage message)
        {
            DialogDocumentManagerService.ShowView<CityViewModel>(message, this);
        }

        private void OnShowPdfPreview(PdfPreviewParameter message)
        {
            SizeableDialogDocumentManagerService.ShowView<PdfPreviewViewModel>(message, this);
        }

        private void OnAdditionalServiceProductView(AdditionalServiceProductViewMessage message)
        {
            NotModalSizeableDocumentManagerService.ShowEditorView<AdditionalServiceProductViewModel>(message.Id, new AdditionalServiceProductParameter(message.Id), this);
        }

        private void OnWarehouseView(WarehouseEditParameter message)
        {
            DialogDocumentManagerService.ShowEditorView<WarehouseViewModel>(message.Id, message, this);
        }

        private void OnCashboxView(CashboxEditParameter message)
        {
            DialogDocumentManagerService.ShowEditorView<CashboxViewModel>(message.Id, message, this);
        }

        private void OnReturnInvoiceEditView(ReturnInvoiceEditViewMessage message)
        {
            NonModalSizeableDialogDocumentManagerService
                .ShowView<ReturnInvoiceViewModel>(new ReturnInvoiceParameter(message.ReturnInvoiceId), this);
        }

        private void OnAssemblyServiceView(AssemblyServiceViewMessage message)
        {
            NotModalSizeableDocumentManagerService.ShowEditorView<AssemblyServiceViewModel>(message.AssemblyServiceId, new AssemblyServiceParameter(message.AssemblyServiceId), this);
        }

        private void OnCallDialogMessage(CallDialogParameter message)
        {
            IsVisibilityCallDialog = true;
            CallDialog.SetParameterAsync(message);
        }

        private void OnCallDialogEnd(CallDialogEndMessage message)
        {
            if (IsVisibilityCallDialog)
            {
                CallDialog.Clear();
                IsVisibilityCallDialog = false;
                Messenger.Send(new EndAsteriskCallMessage());
            }
        }

        private void OnShowReport(ShowReportMessage message)
        {
            NavigationMenuItem navItem = new (
                $"{message.ReportId}.Отчет \"{message.ReportName}\"",
                string.Empty,
                typeof(ReportView),
                Array.Empty<Role>(),
                Array.Empty<BusinessOperation>(),
                100);

            ShowModuleInternal(navItem, message.ReportId, true);
        }

        private async Task ShowWorkPlaceViewInternalAsync()
        {
            EquipmentSettingsInfo equipmentSettings = await EquipmentSettingsStore.LoadAsync();

            Guid uniqueDeviceGuid = equipmentSettings!.UniqueDeviceGuid!.Value;

            WorkPlaceDto workPlaceDto = await WebClient.ExecuteApiRequestAsync(new QueryWorkPlace(uniqueDeviceGuid.ToString()));

            if (WebClient.WorkPlaceId.HasValue || workPlaceDto is null || workPlaceDto.DeviceTypeId == WorkPlaceDeviceType.Laptop.Id)
            {
                NoBorderDialogDocumentManagerService.ShowView<WorkPlaceViewModel>(workPlaceDto, this);
            }
            else
            {
                WebClient.SetWorkPlaceId(workPlaceDto.Id);
                Messenger.Send(new UpdateWorkPlaceMessage(workPlaceDto));
            }
        }

        private void RedirectKeyEventMessage(KeyEventMessage msg)
        {
            if (DocumentManagerService.ActiveDocument?.Content is ISupportHotkeys activeViewModel)
            {
                ModifierKeys modifiers = msg.Args.KeyboardDevice.Modifiers;
                Key key = msg.Args.Key == Key.System
                    ? msg.Args.SystemKey
                    : msg.Args.Key;

                msg.Args.Handled = activeViewModel.HandleHotkey(new HotkeyMessage(key, modifiers));
            }
        }

        private void ShowModule(NavigationMenuItem navItem)
        {
            if(navItem.GetParameter() is TelewikiParameter)
            {
                TelewikiParameter telewikiParameter = (TelewikiParameter)navItem.GetParameter();

                telewikiParameter.SetUserPassword(UserPassword, false);

                ShowModuleInternal(navItem, telewikiParameter, false);

                return;
            }

            ShowModuleInternal(navItem, navItem.GetParameter(), false);
        }

        private void ShowModuleSimultaneously(NavigationMenuItem navItem)
        {
            ShowModuleInternal(navItem, navItem.GetParameter(), true);
        }

        private Task ShowViewAsync<TViewModel, TBeforeInitMessage>(
            IDocumentManagerService documentManagerService,
            int id,
            Func<Task<TViewModel>> getViewModelTaskFunc,
            string viewName)
            where TViewModel : class
            where TBeforeInitMessage : class, new()
        {
            Task result = Task.CompletedTask;

            string documentKey = $"{typeof(TViewModel).Name}_{id.ToString(CultureInfo.InvariantCulture)}";

            if (!openedDocuments.TryAdd(documentKey, documentKey))
            {
                return result;
            }

            IDocument document = documentManagerService.FindDocumentById(documentKey);

            if (document != null)
            {
                Messenger.Send(new TBeforeInitMessage());
                documentManagerService.ActiveDocument = document;
                openedDocuments.TryRemove(documentKey, out object dummy);
            }
            else
            {
                Task<TViewModel> initViewModelTask = getViewModelTaskFunc();

                result = initViewModelTask.ContinueWith(
                    task =>
                    {
                        Messenger.Send(new TBeforeInitMessage());

                        if (task.IsFaulted)
                        {
                            string errorMessage = Resources.ErrorDuringDataLoading;

                            if (task.Exception != null)
                            {
                                Logger.LogError(task.Exception.InnerException, "Failed to init view model");

                                task.Exception.Handle(
                                    ex =>
                                    {
                                        switch (ex)
                                        {
                                            case UnexpectedSatusException unexpectedSatusException:
                                                if (unexpectedSatusException.Args.HttpStatusCode == HttpStatusCode.Forbidden)
                                                {
                                                    errorMessage = "У вас нет прав на выполнение операции";
                                                }

                                                break;
                                            case UnexpectedErrorException _:
                                                errorMessage = $"{Resources.ServerConnectError}.{Resources.ServerUnavailable}";
                                                break;
                                        }

                                        return true;
                                    });
                            }

                            MessageFacadeService.ShowNotificationError(errorMessage);
                        }
                        else if (task.IsCompleted)
                        {
                            TViewModel viewModel = task.Result;
                            document = documentManagerService.CreateDocument(viewName, viewModel, null, this);
                            document.Id = documentKey;
                            document.DestroyOnClose = true;
                            document.Show();
                        }

                        openedDocuments.TryRemove(documentKey, out object dummy);
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
            }

            return result;
        }

        private void ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);
        }

        private void OnPasswordSend(PasswordSendMessage massege)
        {
            UserPassword = massege.Password;
        }

        private void OnSendWikiCookies(SendWikiCookiesMessage message)
        {
            Cookies = message.Cookies;
        }

        private void OnShowcaseHistoriesOppen(ShowcaseHistoriesMessage _)
        {
            NavigationMenuItem navItem = new NavigationMenuItem(
                "История витрин",
                "shop",
                typeof(ShowcasesView),
                Array.Empty<Role>(),
                Array.Empty<BusinessOperation>(),
                100);

            ShowModuleInternal(navItem, null, true);
        }

        private void OnSetUnavailableAsterisStatus(SetUnavailableAsterisStatusMessage message)
        {
            if (CallTrackOptions.Type != CallTrackType.Asterisk)
            {
                return;
            }

            Task.Factory.StartNew(async () => await SetUnavailableAsterisStatusAsync());

            async Task SetUnavailableAsterisStatusAsync()
            {
                try
                {
                    List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(AsteriskConstants.AsteriskAccount, AsteriskConstants.CallCenterDepartmentId)).GetPagedResultDataAsync();

                    if (employees.Any(x => x.Id == WebClient.AuthenticatedEmployee.Id && x.PositionId != AsteriskConstants.ManagerCallCenterId))
                    {
                        await WebClient.ExecuteCallApiRequestAsync(new SetAsteriskEmployeeStatus(AsteriskConstants.CallUnavailableStatusId, WebClient.AuthenticatedEmployee.Id));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to set Unavailable state");
                }
            }
        }

        private void OnEndAsteriskCall(EndAsteriskCallMessage message)
        {
            if (CallTrackOptions.Type != CallTrackType.Asterisk)
            {
                return;
            }

            MainWindowViewModel.ChangeUsersStatusAsterisk = false;

            Task.Factory.StartNew(async () => SetAsteriskBusyStatusAsync());

            async Task SetAsteriskBusyStatusAsync()
            {
                try
                {
                    Task<string> busySecondsAfterCallTask = WebClient.ExecuteApiRequestAsync(new QueryBusySecondsAfterCall());

                    string busySecondsAfterCall = await busySecondsAfterCallTask;

                    bool getbusySecondsAfterCall = int.TryParse(busySecondsAfterCall, out int seconds);

                    if (seconds == 0)
                    {
                        Logger.LogWarning("Asterisk status Busy is 0.");

                        return;
                    }

                    Task<List<EmployeeDto>> employeesTask = WebClient.ExecuteApiRequestAsync(new QueryEmployees(AsteriskConstants.AsteriskAccount, AsteriskConstants.CallCenterDepartmentId)).GetPagedResultDataAsync();

                    List<EmployeeDto> employees = await employeesTask;

                    if (employees.Any(x => x.Id == WebClient.AuthenticatedEmployee.Id))
                    {
                        await WebClient.ExecuteCallApiRequestAsync(new SetAsteriskEmployeeStatus(AsteriskConstants.CallBusyStatusId, WebClient.AuthenticatedEmployee.Id));

                        Logger.LogWarning("Set Asterisk status Busy");

                        await Task.Delay(TimeSpan.FromSeconds(getbusySecondsAfterCall ? seconds : 10));

                        if (!MainWindowViewModel.ChangeUsersStatusAsterisk)
                        {
                            await WebClient.ExecuteCallApiRequestAsync(new SetAsteriskEmployeeStatus(AsteriskConstants.CallActiveStatusId, WebClient.AuthenticatedEmployee.Id));
                            Logger.LogWarning("Set Asterisk status active");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to set busy state");
                }
            }
        }
    }
}