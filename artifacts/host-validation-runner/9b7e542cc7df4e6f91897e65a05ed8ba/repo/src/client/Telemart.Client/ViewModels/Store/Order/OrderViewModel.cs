using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AngleSharp.Common;
using AutoMapper;
using DevExpress.Data.Extensions;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.CodeView;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.DragDrop;
using DevExpress.XtraReports;
using Humanizer;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSwag.Collections;
using SmartFormat;
using Telemart.Client.Business;
using Telemart.Client.Business.Audit;
using Telemart.Client.Business.Audit.Entry;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Business.Order;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.ModuleAnalytics;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Core.ErrorHandling;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.AdditionalService;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct;
using Telemart.Client.Data.Requests.Features.Area;
using Telemart.Client.Data.Requests.Features.AssembledComputerRule;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.AssemblyService.Actions;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.Requests.Features.Call.Actions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.CheckCompatibility;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Complaint;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Contractor.Contact;
using Telemart.Client.Data.Requests.Features.Contractor.Template;
using Telemart.Client.Data.Requests.Features.Crm;
using Telemart.Client.Data.Requests.Features.Customer;
using Telemart.Client.Data.Requests.Features.CustomerBonus;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Event;
using Telemart.Client.Data.Requests.Features.ExternalPayment;
using Telemart.Client.Data.Requests.Features.FiscalDocument;
using Telemart.Client.Data.Requests.Features.GuestProduct.Actions;
using Telemart.Client.Data.Requests.Features.Hashtag.Actions;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.Requests.Features.ModuleAnalyticUrl;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.Requests.Features.OrderBill;
using Telemart.Client.Data.Requests.Features.OrderDocuments;
using Telemart.Client.Data.Requests.Features.OrderPayment;
using Telemart.Client.Data.Requests.Features.Organization;
using Telemart.Client.Data.Requests.Features.Payments;
using Telemart.Client.Data.Requests.Features.PrintReport;
using Telemart.Client.Data.Requests.Features.Promo;
using Telemart.Client.Data.Requests.Features.PromoCode;
using Telemart.Client.Data.Requests.Features.Purchase;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.Requests.Features.Task;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.Requests.Features.Warehouse.Delivery;
using Telemart.Client.Data.Requests.Features.Warehouse.Performance;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.Reports.AdditionalServiceProduct;
using Telemart.Client.Reports.Order.Assembly;
using Telemart.Client.Reports.Product;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Complaint;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Promo;
using Telemart.Client.TransferObjects.PromoCode;
using Telemart.Client.TransferObjects.Task;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.Warehouse.Delivery;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;
using Telemart.Client.ViewModels.AdditionalService;
using Telemart.Client.ViewModels.AdditionalServiceProduct;
using Telemart.Client.ViewModels.AssembledComputerRule;
using Telemart.Client.ViewModels.AssemblyService;
using Telemart.Client.ViewModels.Carry;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Common.PrintBarcodeParameters;
using Telemart.Client.ViewModels.Complaint;
using Telemart.Client.ViewModels.Customer;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Dialogs.AddDocuments;
using Telemart.Client.ViewModels.ModuleAnalytics;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.PromoCode;
using Telemart.Client.ViewModels.Service.ServiceRequests;
using Telemart.Client.ViewModels.Service.ServiceRequests.Create;
using Telemart.Client.ViewModels.Store.Call;
using Telemart.Client.ViewModels.Store.Order.Assembly;
using Telemart.Client.ViewModels.Store.Order.OrderDocuments;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.Store.Purchase;
using Telemart.Client.ViewModels.Tasks;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Helpers;
using Telemart.Common.TransferObjects;
using Telemart.Common.TreeStructure;
using Telemart.Fiscal.Client.Extensions;
using Telemart.Fiscal.Client.TransferObjects;
using CategoryDto = Telemart.Client.TransferObjects.CategoryDto;
using Currency = Telemart.Client.Dictionaries.Currency;
using CurrencyTypeIds = Telemart.Client.Dictionaries.CurrencyTypeIds;
using ProductPriceKind = Telemart.Client.Dictionaries.ProductPriceKind;
using ProductType = Telemart.Client.Dictionaries.ProductType;
using Role = Telemart.Client.Dictionaries.Role;

namespace Telemart.Client.ViewModels.Store.Order
{
    [MetadataType(typeof(OrderViewModelMetadata))]
    public sealed class OrderViewModel : ViewModelBase, IDocumentContent, IDataErrorInfo, IOrderPaymentInfo
    {
        private const decimal MinUahPrice = 0; // 1m;
        private const decimal MinUsdPrice = 0; // 0.01m;

        private const string SourceAlreadySetWarning = "Сначала удалите текущий источник";

        private readonly Dictionary<int, OrderHistoryFilterItem> idRecordToFilterItemDictionary = new Dictionary<int, OrderHistoryFilterItem>();
        private readonly TelegramBotOptions _telegramBotOptions;

        private OrderDto order;

        private IReadOnlyCollection<DeliveryDto> warehouseDeliveries;
        private IReadOnlyCollection<WarehousePerformanceDto> warehousePerformances;
        private IReadOnlyCollection<ContractorDto> contractors;
        private ObservableCollection<OrderDocumentSimpleDto> documents;

        private BarSubItem historyFilterItem;

        private IBarItem ordersBarItem;
        private IBarItem removedProductsBarItem;

        private OrderHistoryFilterItem orderFilterItem;
        private OrderHistoryFilterItem removedProductsFilterItem;

        private IReadOnlyDictionary<int, string> employeeNames;
        private IReadOnlyDictionary<int, AdditionalServiceDto> additionalServices;
        private short _isCheckReceiveAct;

        private enum PostProcessProductsMode
        {
            AddRange,
            ReplaceRange
        }

        public OrderViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IOrderRules orderRules,
            IMapper mapper,
            IMessenger messenger,
            IMessageFacadeService messageFacadeService,
            IMediator mediator,
            IPrintingSettingsStore printingSettingsStore,
            IOrderGiveHelper orderGiveHelper,
            IRroPrintHelper rroPrintHelper,
            DocumentCommands documentCommands,
            TelegramBotOptions telegramBotOptions,
            IFiscalRegistrarClientFactory fiscalRegistrarClientFactory,
            IErrorHandler errorHandler,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory,
            ILogger<OrderViewModel> logger,
            ProductInformationViewModel productInformationViewModel,
            NovaPayOptions novaPayOptions)
            : this()
        {
            _telegramBotOptions = telegramBotOptions;
            WebClient = webClient;
            Dictionaries = dictionaries;
            OrderRules = orderRules;
            Mapper = mapper;
            ErrorHandler = errorHandler;
            Messenger = messenger;
            MessageFacadeService = messageFacadeService;
            Mediator = mediator;
            PrintingSettingsStore = printingSettingsStore;
            FiscalRegistrarClientFactory = fiscalRegistrarClientFactory;
            OrderGiveHelper = orderGiveHelper;
            RroPrintHelper = rroPrintHelper;
            DocumentCommands = documentCommands;
            Logger = logger;
            NovaPayOptions = novaPayOptions;

            LockableOperationProcessor = lockableOperationProcessorFactory.Create<OrderDto>();
            OrderPaymentViewModel = new OrderPaymentInfoViewModel();
            ProductInformation = productInformationViewModel;

            Messenger.Register<CallMessage>(this, OnCallMessage);
            Messenger.Register<OrderPaymentMessage>(this, OnOrderPaymentMessage);
            Messenger.Register<PurchaseMessage>(this, OnPurchaseMessage);
            Messenger.Register<ComplaintMessage>(this, OnComplaintMessage);
            Messenger.Register<ServiceRequestMessage>(this, OnServiceRequestMessage);
            Messenger.Register<OrderBillMessage>(this, OnOrderBillMessage);
            Messenger.Register<OrderCreateDocumentMessage>(this, OnDocumentMessage);
            Messenger.Register<AdditionalServiceProductEntityMessage>(this, OnAdditionalServiceProductUpdateMessage);
            Messenger.Register<EntityMessage<AdditionalServiceProductDto>>(this, OnAdditionalServiceProductMessage);
            Messenger.Register<OrderMessage>(this, OnOrderMessage);
            AllowSetNotInStock = WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.Warehouse, Role.Packager, Role.Seller, Role.TechSupport);
            UserIsOutsourceSeller = WebClient.AuthenticatedEmployee.HasAnyRole(Role.OutsourceSeller);
            AllowOpenServiceRequest = WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestGetOne);
        }

        public OrderViewModel()
        {
            SelectedProducts = new ObservableCollection<OrderProductViewModel>();
            DraggedProductsErrors = new ObservableDictionary<string, string>();

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);

            OkCommand = new AsyncCommand(OkAsync);
            SaveCommand = new AsyncCommand(SaveAsync);
            CancelCommand = new DelegateCommand(Cancel);
            EditCommand = new AsyncCommand(EditAsync);
            EditInfoCommand = new AsyncCommand(EditInfoAsync);

            AddProductCommand = new DelegateCommand(AddProduct, CanAddProduct);
            BulkAddProductCommand = new DelegateCommand(BulkAddProduct, CanAddProduct);
            DeleteProductCommand = new AsyncCommand<IEnumerable<OrderProductViewModel>>(DeleteOrderProductsAsync, CanDeleteOrderProducts);
            SplitProductCommand = new AsyncCommand<OrderProductViewModel>(SplitOrderProductAsync, CanSplitOrderProduct);
            CreditCommand = new DelegateCommand(ShowCredit, CanShowCredit);
            CopyCreditDataCommand = new AsyncCommand(CopyCreditDataAsync, CanCopyCreditData);

            SetNewOrderProductStateCommand = new AsyncCommand<IEnumerable<OrderProductViewModel>>(SetNewOrderProductsStateAsync, CanSetState);
            SetClarifyOrderProductStateCommand = new AsyncCommand<IEnumerable<OrderProductViewModel>>(SetClarifyOrderProductsStateAsync, CanSetState);
            SetAgreedOrderProductStateCommand = new AsyncCommand<IEnumerable<OrderProductViewModel>>(SetAgreedOrderProductsStateAsync, CanSetState);
            SetAgreedToAllOrderProductStateCommand = new AsyncCommand<IEnumerable<OrderProductViewModel>>(SetAgreedToAllOrderProductsStateAsync, CanSetState);

            RemoveSourceCommand = new AsyncCommand<OrderProductViewModel>(RemoveSourceAsync, x => x != null && (x.Id > 0 || x.IsVirtualProduct) && (State.CanChangeSources || State == OrderStatus.Canceled) && !IsLockedFromAnyUser && SelectedProducts.Count() <= 1);
            SetWarehouseCommand = new AsyncCommand<OrderProductViewModel>(SetWarehouseSourceAsync, x => x is { Id: > 0 } && x.State.AllowSetSource && State.CanChangeSources && !IsLockedFromAnyUser && SelectedProducts.Count() <= 1);
            SetMovementCommand = new AsyncCommand<OrderProductViewModel>(SetMovementAsync, x => x is { Id: > 0 } && x.State.AllowSetSource && State.CanChangeSources && !IsLockedFromAnyUser && SelectedProducts.Count() <= 1);

            SetNoProductCommand = new AsyncCommand<OrderProductViewModel>(SetNoProductAsync, x => x is { Id: > 0 } && x.State.AllowSetSource && State == OrderStatus.Received && !IsLockedFromAnyUser && SelectedProducts.Count() <= 1);
            SetNoPurchaseCommand = new AsyncCommand(SetNoPurchaseAsync, () => Id > 0 && WebClient?.IsOperationAllowed(BusinessOperation.OrderSetNoPurchaseSource) == true && SelectedProducts.Count <= 1);
            SetNotInStockCommand = new AsyncCommand<OrderProductViewModel>(SetNotInStockAsync, CanSetNotInStock);
            FillSourcesCommand = new AsyncCommand(FillSourceAsync, CanFillSources);
            FillSourcesManualCommand = new AsyncCommand(FillSourcesManualAsync, CanFillSources);

            CreateServiceRequestCommand = new DelegateCommand<OrderProductViewModel>(CreateServiceRequest, x => x is { IsNew: false } && !IsLockedFromAnyUser && Rt == 1 && !UserIsOutsourceSeller);
            CreateManyServiceRequestCommand = new AsyncCommand(CreateManyServiceRequestAsync, () => !IsLockedFromAnyUser && Rt == 1 && !UserIsOutsourceSeller && State == OrderStatus.Done);

            CalculateLogisticsCommand = new DelegateCommand(CalculateLogistics);
            ShowStatusPayCommand = new AsyncCommand(QueryExternalPaymentStateAsync, () => (SelectedExternalPayment?.Payment.Id == Payment.MonobankId || SelectedExternalPayment?.Payment.Id == Payment.PumbId || SelectedExternalPayment?.Payment.Id == Payment.ABankId) && WebClient?.IsOperationAllowed(BusinessOperation.OrderShowStatusPay) == true);
            CancelExternalPaymentCommand = new AsyncCommand(CancelExternalPaymentAsync, () => SelectedExternalPayment is not null && WebClient?.IsOperationAllowed(BusinessOperation.CancelExternalPayment) == true);
            EditExternalPaymentCommand = new AsyncCommand(EditExternalPaymentAsync, () => SelectedExternalPayment is not null && WebClient?.IsOperationAllowed(BusinessOperation.EditExternalPayment) == true);
            SelectDeliveryAddressCommand = new DelegateCommand(SelectDeliveryAddress, () => SelectedCarryType != null && SelectedCarryType.Id != CarryType.PickupId);
            GetDeliveryDateCommand = new AsyncCommand(GetDeliveryDateAsync);
            OpenCustomerCommand = new DelegateCommand(OpenCustomer, () => order?.CustomerId != null);
            HandleSelectionChangedCommand = new DelegateCommand<ValueChangedEventArgs<FrameworkElement>>(HandleSelectionChanged);
            HandleSelectionProductInfoChangedCommand = new DelegateCommand<ValueChangedEventArgs<FrameworkElement>>(HandleSelectionProductInfoChanged);
            DropOrderProductCommand = new AsyncCommand<TreeListDropEventArgs>(DropOrderProductAsync);
            DragOrderProductOverCommand = new DelegateCommand<TreeListDragOverEventArgs>(DragOrderProductOver);

            SendSmsCommand = new DelegateCommand<string>(SendSms, CanSendSms);
            MailToCommand = new DelegateCommand<string>(MailTo, x => !string.IsNullOrWhiteSpace(x));

            ApplyContractorTemplateCommand = new DelegateCommand<ContractorTemplateDto>(ApplyContractorTemplate);

            RefreshAuditEntriesCommand = new AsyncCommand<OrderHistoryFilterItem>(RefreshAuditEntriesAsync);
            RefreshCrmCommand = new AsyncCommand(RefreshCrmAsync, () => Id != 0);
            ResendSmsCommand = new DelegateCommand(ResendSms, () => SelectedClientContact?.Type?.Id is ClientContactType.SmsId or ClientContactType.ViberId);

            GiveCommand = new AsyncCommand(GiveAsync, CanGive);
            PackCommand = new AsyncCommand(PackAsync, CanPack);
            CancelOrderCommand = new AsyncCommand(CancelOrderAsync, CanCancelOrder);
            ReceiveCommand = new AsyncCommand(ReceiveOrderAsync, () => order != null && order.StateId == OrderStatus.New.Id && (WebClient.IsOperationAllowed(BusinessOperation.OrderReceive) || order.CreatedBy == WebClient.AuthenticatedEmployee.Id));
            ConfirmOrderCommand = new AsyncCommand(ConfirmOrderAsync, CanConfirmOrder);
            ReconfirmOrderCommand = new AsyncCommand(ReconfirmOrderAsync, CanReconfirmOrder);
            UnpackOrderCommand = new AsyncCommand(UnpackAsync, CanUnpack);
            ShowDocumentsBotQrCommand = new DelegateCommand(ShowDocumentsBotQr, () => order?.Id > 0);

            ExpireOrderCommand = new AsyncCommand(ExpireOrderAsync, CanExpireOrder);

            SetExternalOrderCommand = new AsyncCommand(SetExternalOrderAsync, () => order?.Id > 0 && WebClient.IsOperationAllowed(BusinessOperation.OrderSetExternalOrder));

            EditReceivedDateCommand = new AsyncCommand(EditReceivedDateAsync, () => order != null && order.StateId == OrderStatus.Done.Id && WebClient.IsOperationAllowed(BusinessOperation.OrderEditReceiveDate));
            ClearFiscalRegistarCommand = new AsyncCommand(ClearFiscalRegistarAsync, () => order != null && order.StateId == OrderStatus.Done.Id && order.CompletedOnFiscalRegistrar == true && WebClient.IsOperationAllowed(BusinessOperation.OrderClearConfirmOnFiscalRegistar));
            EditContractorCommand = new AsyncCommand(EditContractorAsync, () => order != null && WebClient.IsOperationAllowed(BusinessOperation.OrderChangeContractor));
            EditCreatedByCommand = new AsyncCommand(EditCreatedByAsync, () => order != null && WebClient.IsOperationAllowed(BusinessOperation.OrderChangeCreatedBy));
            EditProductAddedByCommand = new AsyncCommand<OrderProductViewModel>(EditProductAddedByAsync, x => x != null && (x.Id > 0 || x.IsVirtualProduct) && order != null && SelectedProducts.Count() <= 1 && WebClient.IsOperationAllowed(BusinessOperation.OrderChangeProductAddedBy));

            AddPromoCodeCommand = new AsyncCommand(AddPromoCodeAsync, () => ExternalPayments != null && (!ExternalPayments.Any() || ExternalPayments.All(x => Payment.IsEditingAllowed(x.Payment.Id, x.PaymentStateId, x.Payment.Credit, x.Payment.PartialCredit))));
            RemovePromoCodeCommand = new AsyncCommand<OrderPromoCodeDto>(RemovePromoCodeAsync, x => x != null && (ExternalPayments != null && (!ExternalPayments.Any() || ExternalPayments.All(z => Payment.IsEditingAllowed(z.Payment.Id, z.PaymentStateId, z.Payment.Credit, z.Payment.PartialCredit)))) && x.PromoCodeTypeId != PromoCodeType.Bundle.Id);
            RefreshPromoCodesCommand = new AsyncCommand(RefreshPromoCodesAsync, () => PromoCodes != null && PromoCodes.Any());

            RefreshCallsCommand = new AsyncCommand(RefreshCallsAsync);
            RefreshServiceRequestsCommand = new AsyncCommand(RefreshServiceRequestsAsync);
            RefreshAssemblyServicesCommand = new AsyncCommand(RefreshAssemblyServicesAsync);
            RefreshAdditionalServiceProductsCommand = new AsyncCommand(RefreshAdditionalServiceProductsAsync);
            RefreshPromosCommand = new AsyncCommand(RefreshPromosAsync);

            AddCallCommand = new DelegateCommand(AddCall);
            EditCallCommand = new DelegateCommand<CallViewItem>(EditCall, x => x != null && x.CallState == CallState.New);
            CancelCallCommand = new AsyncCommand<CallViewItem>(CancelCallAsync, x => x != null);
            CallCommand = new DelegateCommand<CallViewItem>(Call, x => x != null);
            ReceiveGuestProductCommand = new AsyncCommand<OrderProductViewModel>(x => ShowGuestProductAsync(x, false), CanReceiveGuestProduct);
            ReceivedGuestProductCommand = new AsyncCommand<OrderProductViewModel>(x => ShowGuestProductAsync(x, false), CanReceivedGuestProduct);
            GuestProductInfoCommand = new AsyncCommand<OrderProductViewModel>(x => ShowGuestProductAsync(x, true), CanShowGuestProductInfo);

            PrintOrderCommand = new AsyncCommand<int>(PrintOrderAsync);
            PrintAssemblyCommand = new AsyncCommand(PrintAssemblyAsync, () => order != null && (order.StateId == OrderStatus.Confirmed.Id || order.StateId == OrderStatus.Packed.Id || order.StateId == OrderStatus.Done.Id));
            PrintWarrantyCardCommand = new AsyncCommand(PrintWarrantyCardAsync);
            PrintTrackNumberCommand = new AsyncCommand(PrintTrackNumberAsync, CanPrintTrackNumber);
            PrintChequeCommand = new AsyncCommand(PrintChequeAsync, () => !string.IsNullOrEmpty(order?.FiscalId));
            PrintActIncomeCommand = new AsyncCommand<bool>(PrintActIncomeAsync, _ => CanPrintAct());
            PrintActOutcomeCommand = new AsyncCommand(PrintActOutcomeAsync, CanPrintAct);
            ShowUklonOrderCommand = new DelegateCommand(ShowUklonOrder, CanShowUklonOrder);

            HandleHistoryFilterLoadedCommand = new DelegateCommand<RoutedEventArgs>(HandleHistoryFilterLoaded);
            SetHistoryFilterItemCommand = new DelegateCommand<OrderHistoryFilterItem>(SetHistoryItem);
            ShowProductHistoryCommand = new DelegateCommand<OrderProductViewModel>(ShowProductHistory, x => x is { IsNew: false } && SelectedProducts.Count() <= 1);

            RefreshOrderPaymentsCommand = new AsyncCommand(RefreshOrderPaymentsAsync);
            RefreshExternalPaymentsCommand = new AsyncCommand(RefreshExternalPaymentsAsync);
            AddOrderPaymentCommand = new AsyncCommand(AddOrderPaymentAsync, () => Id != 0);
            AddOrderPaymentViaCashRegistrarCommand = new AsyncCommand(AddOrderPaymentViaCashRegistrarAsync, () => Id != 0 && order.PaymentId != Payment.CashlessTaxId);
            AddOrderWithdrawCommand = new AsyncCommand(AddOrderWithdrawAsync, () => Id != 0);
            EditOrderPaymentCommand = new DelegateCommand<OrderPaymentRecordViewItem>(EditOrderPayment, x => x != null);
            SendChequeCommand = new AsyncCommand<OrderPaymentRecordViewItem>(x => SendChequeAsync(x.FiscalId), x => !string.IsNullOrEmpty(x?.FiscalId));

            RefreshComplaintsCommand = new AsyncCommand(RefreshComplaintsAsync);
            AddComplaintCommand = new DelegateCommand(AddComplaint);

            AddAssemblyCommand = new AsyncCommand(AddAssemblyAsync, CanAddProduct);
            AddAssembledComputerRuleCommand = new AsyncCommand(AddAssembledComputerRuleAsync, CanAddProduct);
            AddFolderCommand = new DelegateCommand(() => AddFolder("Сборка", OrderFolderType.AssemblyService), CanAddProduct);
            EditAssemblyCommand = new AsyncCommand<OrderProductViewModel>(EditAssemblyAsync, x => x?.IsAssembly() == true && CanAddProduct());
            CreateAssemblyWithProductsCommand = new AsyncCommand(CreateAssemblyWithProductsAsync, CanCreateAssemblyWithProducts);
            GenerateAssembledComputerRuleCommand = new AsyncCommand(GenerateAssembledComputerRuleAsync, CanGenerateAssembledComputerRule);
            CheckCompatibilityCommand = new AsyncCommand<OrderProductViewModel>(CheckCompatibilityAsync, x => x?.IsAssemblyOrAssembledComputerRule() == true);
            PrintAssemblyReportCommand = new AsyncCommand(PrintAssemblyReportAsync, () => OrderProducts.Any(x => x.IsAssemblyOrAssembledComputerRule() && x.Product.Id == Constants.AssemblyServiceProductId));

            AddBundleCommand = new AsyncCommand<IEnumerable<OrderProductViewModel>>(AddBundleAsync, (x) => x?.Count() > 1 && x.All(y => y.Quantity == 1 && y.OrderPromoCodeId == null && y.OrderFolderId is null) && CanAddProduct());
            DeleteBundleCommand = new AsyncCommand<OrderProductViewModel>(DeleteBundleAsync, (x) => x != null && x.IsVirtualProduct && x.OrderFolderId != null && x.OrderFolder.TypeId == OrderFolderType.BundleId && CanAddProduct());

            PaymentControlCommand = new AsyncCommand(PaymentControlAsync, CanPaymentControlOrder);
            PrintOrderPaymentFiscalChequeCommand = new AsyncCommand<OrderPaymentRecordViewItem>(x => PrintOrderPaymentFiscalChequeAsync(x.Id, x.Amount, x.FiscalCashboxId ?? x.CashboxId, x.PaymentId!.Value, x.FiscalId), CanPrintOrderPaymentOnFiscalRegistrar);

            AddBonusCommand = new AsyncCommand(AddBonusAsync, CanEditBonuses);
            EditBonusCommand = new AsyncCommand<OrderBonusSummaryViewItem>(EditBonusAsync, x => x != null && CanEditBonuses());
            RemoveBonusCommand = new AsyncCommand<OrderBonusSummaryViewItem>(RemoveBonusAsync, x => x != null && CanEditBonuses());
            EditServiceRequestCommand = new DelegateCommand<ServiceRequestViewItem>(EditServiceRequest, x => x != null && AllowOpenServiceRequest);

            HandleRowDoubleClickCommand = new AsyncCommand<RowDoubleClickInfo>(HandleRowDoubleClickAsync, x => x != null);
            HandlePromoRowDoubleClickCommand = new DelegateCommand<RowDoubleClickInfo>(HandlePromoRowDoubleClick, x => x != null);

            CreateOrderBillCommand = new DelegateCommand(CreateOrderBill, () => SelectedContractor?.Buh1CId.HasValue == true);
            RefreshOrderBillsCommand = new AsyncCommand(RefreshOrderBillsAsync, () => order != null);
            RecalculateOrderBillCommand = new AsyncCommand(RecalculateOrderBillAsync, () => SelectedOrderBill != null && (SelectedOrderBill.StateId == OrderBillState.New.Id || SelectedOrderBill.StateId == OrderBillState.Paid.Id));
            CancelOrderBillCommand = new AsyncCommand(CancelOrderBillAsync, () => SelectedOrderBill != null && (SelectedOrderBill.StateId == OrderBillState.New.Id || SelectedOrderBill.StateId == OrderBillState.Paid.Id));
            PrintBillCommand = new AsyncCommand(PrintBillAsync, () => SelectedOrderBill != null);
            PrintBillInvoiceCommand = new AsyncCommand(PrintBillInvoiceAsync, CanPrintBillInvoice);

            OpenAssemblyServiceCommand = new DelegateCommand<int>(OpenAssemblyService, x => x != 0);
            OpenAdditionalServiceProductCommand = new DelegateCommand<int>(OpenAdditionalServiceProduct, x => x != 0);
            FindCustomerCommand = new AsyncCommand(FindCustomerAsync, () => order != null && !string.IsNullOrWhiteSpace(Phone) && ((IsLockedByCurrentUserAndEditingAllowed && order.CustomerId is null) || order.Id == 0));

            ResetProductPricesCommand = new DelegateCommand(ResetProductPrices, () => CanAddProduct() && WebClient.IsOperationAllowed(BusinessOperation.OrderResetProductPriceType));
            SetProductPricesCommand = new DelegateCommand(SetProductPrices, () => CanAddProduct() && WebClient.IsOperationAllowed(BusinessOperation.OrderSetProductPriceType));
            SendOrderChequeToEmailCommand = new AsyncCommand(() => SendChequeToEmailAsync(order.FiscalId), () => !string.IsNullOrEmpty(order?.FiscalId));
            AddCustomAssemblyCommand = new AsyncCommand(AddCustomAssemblyAsync, CanAddProduct);
            CalculateAssemblyServiceProductCommand = new AsyncCommand<OrderProductViewModel>(x => CalculateAssemblyServiceProductPriceAsync(x.OrderFolderId), CanCalculateAssemblyServiceProductPrice);
            RefreshDocumentsCommand = new AsyncCommand(RefreshDocumentsAsync);
            PrintDocumentCommand = new AsyncCommand<OrderDocumentViewItem>(PrintDocumentAsync, x => x != null);
            AddDocumentCommand = new DelegateCommand(AddDocument, () => Model != null && WebClient.IsOperationAllowed(BusinessOperation.OrderCreateDocumment));
            RemoveDocumentCommand = new AsyncCommand<OrderDocumentViewItem>(RemoveDocumentAsync, x => x != null && WebClient.IsOperationAllowed(BusinessOperation.OrderDeleteDocumment));
        }

        public IDocumentOwner DocumentOwner { get; set; }

        string IDataErrorInfo.Error => string.Empty;

        public OrderPaymentInfoViewModel OrderPaymentViewModel { get; private set; }

        public object Title
        {
            get { return GetProperty(() => Title); }
            private set { SetProperty(() => Title, value); }
        }

        #region Collections

        public ObservableCollection<OrderCarryTypeViewItem> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            set { SetProperty(() => CarryTypes, value); }
        }

        public ReadOnlyObservableCollection<OrderCityViewItem> Cities
        {
            get { return GetProperty(() => Cities); }
            set { SetProperty(() => Cities, value); }
        }

        public ObservableCollection<OrderContractorViewItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            set { SetProperty(() => Contractors, value); }
        }

        public ObservableCollection<OrderProductViewModel> SelectedProducts
        {
            get { return GetProperty(() => SelectedProducts); }
            set { SetProperty(() => SelectedProducts, value); }
        }

        public ObservableDictionary<string, string> DraggedProductsErrors
        {
            get { return GetProperty(() => DraggedProductsErrors); }
            set { SetProperty(() => DraggedProductsErrors, value); }
        }

        public ReadOnlyObservableCollection<OrderPaymentViewItem> Payments
        {
            get { return GetProperty(() => Payments); }
            set { SetProperty(() => Payments, value); }
        }

        public ReadOnlyObservableCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            set { SetProperty(() => Subdivisions, value); }
        }

        public ReadOnlyObservableCollection<OrderWarehouseViewItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> AssemblyWarehouses
        {
            get { return GetProperty(() => AssemblyWarehouses); }
            set { SetProperty(() => AssemblyWarehouses, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> BufferWarehouses
        {
            get { return GetProperty(() => BufferWarehouses); }
            set { SetProperty(() => BufferWarehouses, value); }
        }

        public ReadOnlyObservableCollection<OrderWarehouseViewItem> AdditionalServiceWarehouses
        {
            get { return GetProperty(() => AdditionalServiceWarehouses); }
            set { SetProperty(() => AdditionalServiceWarehouses, value); }
        }

        public ReadOnlyObservableCollection<OrganizationDto> Organizations
        {
            get { return GetProperty(() => Organizations); }
            set { SetProperty(() => Organizations, value); }
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ContractorTemplateDto> ContractorTemplates
        {
            get { return GetProperty(() => ContractorTemplates); }
            set { SetProperty(() => ContractorTemplates, value); }
        }

        public ReadOnlyObservableCollection<ClientContactViewItem> ClientContactsHistoryItems
        {
            get { return GetProperty(() => ClientContactsHistoryItems); }
            private set { SetProperty(() => ClientContactsHistoryItems, value); }
        }

        public ClientContactViewItem SelectedClientContact
        {
            get { return GetProperty(() => SelectedClientContact); }
            set { SetProperty(() => SelectedClientContact, value); }
        }

        public IReadOnlyCollection<ContractorContactDto> ContractorContacts { get; private set; }

        public ReadOnlyObservableCollection<ComboBoxItem> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            private set { SetProperty(() => Cashboxes, value); }
        }

        public ReadOnlyObservableCollection<Warranty> Warranties
        {
            get { return GetProperty(() => Warranties); }
            private set { SetProperty(() => Warranties, value); }
        }

        public ReadOnlyObservableCollection<AssemblyServiceDto> AssemblyServices
        {
            get { return GetProperty(() => AssemblyServices); }
            private set { SetProperty(() => AssemblyServices, value); }
        }

        public ReadOnlyObservableCollection<AssemblyServiceDto> ProductAssemblyServices
        {
            get { return GetProperty(() => ProductAssemblyServices); }
            private set { SetProperty(() => ProductAssemblyServices, value); }
        }

        public IReadOnlyDictionary<int, PromoCodeDto[]> ProductsPromoCodes
        {
            get { return GetProperty(() => ProductsPromoCodes); }
            private set { SetProperty(() => ProductsPromoCodes, value); }
        }

        public ReadOnlyObservableCollection<PromoCodeDto> ProductPromos
        {
            get { return GetProperty(() => ProductPromos); }
            private set { SetProperty(() => ProductPromos, value); }
        }

        public ObservableCollection<AdditionalServiceProductDto> AdditionalServiceProducts
        {
            get { return GetProperty(() => AdditionalServiceProducts); }
            private set { SetProperty(() => AdditionalServiceProducts, value); }
        }

        public ReadOnlyObservableCollection<AdditionalServiceProductDto> ProductAdditionalServiceProducts
        {
            get { return GetProperty(() => ProductAdditionalServiceProducts); }
            private set { SetProperty(() => ProductAdditionalServiceProducts, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> LegalEntities
        {
            get { return GetProperty(() => LegalEntities); }
            private set { SetProperty(() => LegalEntities, value); }
        }

        public ReadOnlyObservableCollection<OrderSourceTypeViewItem> OrderSources
        {
            get { return GetProperty(() => OrderSources); }
            private set { SetProperty(() => OrderSources, value); }
        }

        public ObservableCollection<OrderDocumentViewItem> OrderDocumentViewItems
        {
            get { return GetProperty(() => OrderDocumentViewItems); }
            private set { SetProperty(() => OrderDocumentViewItems, value); }
        }

        public ReadOnlyObservableCollection<OrderDocumentTypeDto> OrderDocumentTypes
        {
            get { return GetProperty(() => OrderDocumentTypes); }
            private set { SetProperty(() => OrderDocumentTypes, value); }
        }

        #endregion

        #region Properties

        public double Top
        {
            get { return GetProperty(() => Top); }
            set { SetProperty(() => Top, value); }
        }

        public double Left
        {
            get { return GetProperty(() => Left); }
            set { SetProperty(() => Left, value); }
        }

        public OrderProductViewModel CurrentOrderProduct
        {
            get { return GetProperty(() => CurrentOrderProduct); }
            set { SetProperty(() => CurrentOrderProduct, value, CurrentProductChanged); }
        }

        public ObservableCollection<AuditEntry> AuditEntries
        {
            get { return GetProperty(() => AuditEntries); }
            private set { SetProperty(() => AuditEntries, value); }
        }

        public ObservableCollection<OrderPromoCodeDto> PromoCodes
        {
            get { return GetProperty(() => PromoCodes); }
            private set { SetProperty(() => PromoCodes, value); }
        }

        public ReadOnlyObservableCollection<PromoCodeType> PromoCodeTypes
        {
            get { return GetProperty(() => PromoCodeTypes); }
            private set { SetProperty(() => PromoCodeTypes, value); }
        }

        public ObservableCollection<OrderProductBonusDto> Bonuses
        {
            get { return GetProperty(() => Bonuses); }
            private set { SetProperty(() => Bonuses, value); }
        }

        public ObservableCollection<ExternalPaymentViewItem> ExternalPayments
        {
            get { return GetProperty(() => ExternalPayments); }
            private set { SetProperty(() => ExternalPayments, value); }
        }

        public ExternalPaymentViewItem SelectedExternalPayment
        {
            get { return GetProperty(() => SelectedExternalPayment); }
            set { SetProperty(() => SelectedExternalPayment, value); }
        }

        public IEnumerable<OrderBonusSummaryViewItem> BonusSummary
        {
            get { return GetProperty(() => BonusSummary); }
            private set { SetProperty(() => BonusSummary, value); }
        }

        public ReadOnlyObservableCollection<BonusType> BonusTypes
        {
            get { return GetProperty(() => BonusTypes); }
            private set { SetProperty(() => BonusTypes, value); }
        }

        public ReadOnlyObservableCollection<ProductPriceKind> ProductPriceKinds
        {
            get { return GetProperty(() => ProductPriceKinds); }
            private set { SetProperty(() => ProductPriceKinds, value); }
        }

        public ObservableCollection<CallViewItem> Calls
        {
            get { return GetProperty(() => Calls); }
            private set { SetProperty(() => Calls, value); }
        }

        public ObservableCollection<ServiceRequestViewItem> ServiceRequests
        {
            get { return GetProperty(() => ServiceRequests); }
            private set { SetProperty(() => ServiceRequests, value); }
        }

        public ObservableCollection<OrderBillViewItem> OrderBills
        {
            get { return GetProperty(() => OrderBills); }
            private set { SetProperty(() => OrderBills, value); }
        }

        public OrderBillViewItem SelectedOrderBill
        {
            get { return GetProperty(() => SelectedOrderBill); }
            set { SetProperty(() => SelectedOrderBill, value); }
        }

        public ObservableRangeCollection<OrderProductViewModel> OrderProducts
        {
            get { return GetProperty(() => OrderProducts); }
            set { SetProperty(() => OrderProducts, value, () => RaisePropertiesChanged(nameof(GuestProductColumnIsVisible), nameof(ScanVisible))); }
        }

        public IEnumerable<SummaryViewItem> OrderSummaryItems
        {
            get { return GetProperty(() => OrderSummaryItems); }
            private set { SetProperty(() => OrderSummaryItems, value); }
        }

        public string Address
        {
            get { return GetProperty(() => Address); }
            set { SetProperty(() => Address, value); }
        }

        public DeliveryDataDto DeliveryData
        {
            get { return GetProperty(() => DeliveryData); }
            set { SetProperty(() => DeliveryData, value); }
        }

        public string PhoneHexBackground
        {
            get { return GetProperty(() => PhoneHexBackground); }
            set { SetProperty(() => PhoneHexBackground, value); }
        }

        public string JoinedComment
        {
            get { return GetProperty(() => JoinedComment); }
            set { SetProperty(() => JoinedComment, value); }
        }

        public string EmployeeComment
        {
            get { return GetProperty(() => EmployeeComment); }
            set { SetProperty(() => EmployeeComment, value, RecalculateJoinedComment); }
        }

        public string SystemComment
        {
            get { return GetProperty(() => SystemComment); }
            set { SetProperty(() => SystemComment, value, RecalculateJoinedComment); }
        }

        public string CustomerComment
        {
            get { return GetProperty(() => CustomerComment); }
            set { SetProperty(() => CustomerComment, value, RecalculateJoinedComment); }
        }

        public DateTime? CompletedOn
        {
            get { return GetProperty(() => CompletedOn); }
            set { SetProperty(() => CompletedOn, value); }
        }

        public int? CompletedBy
        {
            get { return GetProperty(() => CompletedBy); }
            set { SetProperty(() => CompletedBy, value); }
        }

        public DateTime? ReceiveTime
        {
            get { return GetProperty(() => ReceiveTime); }
            set { SetProperty(() => ReceiveTime, value); }
        }

        public DateTime? DeliveryTime
        {
            get { return GetProperty(() => DeliveryTime); }
            set { SetProperty(() => DeliveryTime, value); }
        }

        public DateTime? DeliveryTimeTo
        {
            get { return GetProperty(() => DeliveryTimeTo); }
            set { SetProperty(() => DeliveryTimeTo, value); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public string LastName
        {
            get { return GetProperty(() => LastName); }
            set { SetProperty(() => LastName, value, () => { RaisePropertyChanged(nameof(FioStr)); }); }
        }

        public string FirstName
        {
            get { return GetProperty(() => FirstName); }
            set { SetProperty(() => FirstName, value, () => { RaisePropertyChanged(nameof(FioStr)); }); }
        }

        public string MiddleName
        {
            get { return GetProperty(() => MiddleName); }
            set { SetProperty(() => MiddleName, value, () => { RaisePropertyChanged(nameof(FioStr)); }); }
        }

        public string FioStr
        {
            get
            {
                return string.Join(" ", FioParts().Where(x => !string.IsNullOrWhiteSpace(x)));

                IEnumerable<string> FioParts()
                {
                    yield return LastName;
                    yield return FirstName;
                    yield return MiddleName;
                }
            }
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value, () => Model = new { Id }); }
        }

        // Model property created for reflection in Discussions
        public object Model
        {
            get { return GetProperty(() => Model); }
            private set { SetProperty(() => Model, value); }
        }

        public bool IsEditingCompleted
        {
            get { return GetProperty(() => IsEditingCompleted); }
            set { SetProperty(() => IsEditingCompleted, value, () => RaisePropertyChanged(nameof(IsEditButtonVisible))); }
        }

        public bool IsLocked => LockerId.HasValue && LockerId != WebClient.AuthenticatedEmployee.Id;

        public bool IsLockedOrEdidingCompleted
        {
            get { return GetProperty(() => IsLockedOrEdidingCompleted); }
            set { SetProperty(() => IsLockedOrEdidingCompleted, value); }
        }

        public bool CanChangePriceIdForCurrentProduct
        {
            get { return GetProperty(() => CanChangePriceIdForCurrentProduct); }
            private set { SetProperty(() => CanChangePriceIdForCurrentProduct, value); }
        }

        public bool IsLockedByCurrentUserAndEditingAllowed
        {
            get => GetProperty(() => IsLockedByCurrentUserAndEditingAllowed);
            set => SetProperty(() => IsLockedByCurrentUserAndEditingAllowed, value, () =>
            {
                OrderProducts.ForEach(x => x.RefreshAllowEdit());
                RaisePropertiesChanged(nameof(AllowEditCarryType));
            });
        }

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        public int? LockerId
        {
            get { return GetProperty(() => LockerId); }
            set { SetProperty(() => LockerId, value, () => RaisePropertiesChanged(nameof(IsLocked), nameof(IsLockedFromAnyUser), nameof(IsEditButtonVisible), nameof(EditInfoVisible))); }
        }

        public string LockPerson
        {
            get { return GetProperty(() => LockPerson); }
            set { SetProperty(() => LockPerson, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int? PackageDeliveryCost
        {
            get { return GetProperty(() => PackageDeliveryCost); }
            set { SetProperty(() => PackageDeliveryCost, value); }
        }

        public bool Loaded
        {
            get { return GetProperty(() => Loaded); }
            set { SetProperty(() => Loaded, value); }
        }

        public bool PackageDeliveryPaid
        {
            get { return GetProperty(() => PackageDeliveryPaid); }
            set { SetProperty(() => PackageDeliveryPaid, value); }
        }

        public string PackageTtn
        {
            get { return GetProperty(() => PackageTtn); }
            set { SetProperty(() => PackageTtn, value); }
        }

        public double PackageWeight
        {
            get { return GetProperty(() => PackageWeight); }
            set { SetProperty(() => PackageWeight, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value, OnPhone1Changed); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value, OnPhone2Changed); }
        }

        public int Pko
        {
            get { return GetProperty(() => Pko); }
            set { SetProperty(() => Pko, value, RefreshSummaryItems); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            set { SetProperty(() => ProductInformation, value); }
        }

        public int Rt
        {
            get { return GetProperty(() => Rt); }
            set { SetProperty(() => Rt, value, RefreshSummaryItems); }
        }

        public bool DontCall
        {
            get { return GetProperty(() => DontCall); }
            set { SetProperty(() => DontCall, value); }
        }

        public bool OrganizationRecipient
        {
            get { return GetProperty(() => OrganizationRecipient); }
            set { SetProperty(() => OrganizationRecipient, value); }
        }

        public OrderCarryTypeViewItem SelectedCarryType
        {
            get { return GetProperty(() => SelectedCarryType); }
            set { SetProperty(() => SelectedCarryType, value, SelectedCarryTypeChangedCallback); }
        }

        public OrderCityViewItem SelectedCity
        {
            get { return GetProperty(() => SelectedCity); }
            set { SetProperty(() => SelectedCity, value, SelectedCityChangedCallback); }
        }

        public OrderContractorViewItem SelectedContractor
        {
            get { return GetProperty(() => SelectedContractor); }
            set { SetProperty(() => SelectedContractor, value, SelectedContractorChangedCallback); }
        }

        public int? PriceColumn
        {
            get { return GetProperty(() => PriceColumn); }
            set { SetProperty(() => PriceColumn, value); }
        }

        public OrderPaymentViewItem SelectedPayment
        {
            get { return GetProperty(() => SelectedPayment); }
            set { SetProperty(() => SelectedPayment, value, SelectedPaymentChangedCallback); }
        }

        public Subdivision SelectedSubdivision
        {
            get { return GetProperty(() => SelectedSubdivision); }
            set { SetProperty(() => SelectedSubdivision, value, SelectedSubdivisionChangedCallback); }
        }

        public OrderWarehouseViewItem SelectedWarehouse
        {
            get { return GetProperty(() => SelectedWarehouse); }
            set { SetProperty(() => SelectedWarehouse, value, SelectedWarehouseChangedCallback); }
        }

        public int? BufferWarehouseId
        {
            get { return GetProperty(() => BufferWarehouseId); }
            set { SetProperty(() => BufferWarehouseId, value); }
        }

        public OrderWarehouseViewItem SelectedAdditionalServiceWarehouse
        {
            get { return GetProperty(() => SelectedAdditionalServiceWarehouse); }
            set { SetProperty(() => SelectedAdditionalServiceWarehouse, value); }
        }

        public WarehouseDto AssemblyWarehouse
        {
            get { return GetProperty(() => AssemblyWarehouse); }
            set { SetProperty(() => AssemblyWarehouse, value, AssemblyWarehouseChangedCallBack); }
        }

        public bool ShowLoadingIndicator
        {
            get { return GetProperty(() => ShowLoadingIndicator); }
            set { SetProperty(() => ShowLoadingIndicator, value); }
        }

        public OrderStatus State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value, OrderStateChangedCallback); }
        }

        public string WindowStatusInfo
        {
            get { return GetProperty(() => WindowStatusInfo); }
            set { SetProperty(() => WindowStatusInfo, value); }
        }

        public bool IsOrderProductsValid
        {
            get { return GetProperty(() => IsOrderProductsValid); }
            set { SetProperty(() => IsOrderProductsValid, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ChangeReasons
        {
            get { return GetProperty(() => ChangeReasons); }
            private set { SetProperty(() => ChangeReasons, value); }
        }

        public bool IsHelpVisible
        {
            get { return GetProperty(() => IsHelpVisible); }
            set { SetProperty(() => IsHelpVisible, value); }
        }

        public int? CallsCount
        {
            get { return GetProperty(() => CallsCount); }
            set { SetProperty(() => CallsCount, value, () => RaisePropertyChanged(nameof(CallsCountString))); }
        }

        public int? NewCallsCount
        {
            get { return GetProperty(() => NewCallsCount); }
            set { SetProperty(() => NewCallsCount, value, () => RaisePropertyChanged(nameof(CallsCountString))); }
        }

        public int? ServiceRequestsCount
        {
            get { return GetProperty(() => ServiceRequestsCount); }
            set { SetProperty(() => ServiceRequestsCount, value, () => RaisePropertiesChanged(nameof(ServiceRequestsCountString))); }
        }

        public int? ExternalPaymentsCount
        {
            get { return GetProperty(() => ExternalPaymentsCount); }
            set { SetProperty(() => ExternalPaymentsCount, value, () => RaisePropertiesChanged(nameof(ExternalPaymentsCountString))); }
        }

        public int? SelectLegalEntityId
        {
            get { return GetProperty(() => SelectLegalEntityId); }
            set { SetProperty(() => SelectLegalEntityId, value); }
        }

        public OrderSourceTypeViewItem SelectedOrderSource
        {
            get { return GetProperty(() => SelectedOrderSource); }
            set { SetProperty(() => SelectedOrderSource, value); }
        }

        public ReadOnlyObservableCollection<ServiceRequestState> ServiceRequestsStates
        {
            get { return GetProperty(() => ServiceRequestsStates); }
            private set { SetProperty(() => ServiceRequestsStates, value); }
        }

        public ReadOnlyObservableCollection<PaymentState> PaymentStates
        {
            get { return GetProperty(() => PaymentStates); }
            private set { SetProperty(() => PaymentStates, value); }
        }

        public ReadOnlyObservableCollection<AssemblyServiceState> AssemblyServiceStates
        {
            get { return GetProperty(() => AssemblyServiceStates); }
            private set { SetProperty(() => AssemblyServiceStates, value); }
        }

        public ReadOnlyObservableCollection<AdditionalServiceProductState> AdditionalServiceProductStates
        {
            get { return GetProperty(() => AdditionalServiceProductStates); }
            private set { SetProperty(() => AdditionalServiceProductStates, value); }
        }

        public ReadOnlyObservableCollection<OrderBillState> OrderBillStates
        {
            get { return GetProperty(() => OrderBillStates); }
            private set { SetProperty(() => OrderBillStates, value); }
        }

        public int? ComplaintsCount
        {
            get { return GetProperty(() => ComplaintsCount); }
            set { SetProperty(() => ComplaintsCount, value, () => RaisePropertyChanged(nameof(ComplaintsCountString))); }
        }

        public int? NewComplaintsCount
        {
            get { return GetProperty(() => NewComplaintsCount); }
            set { SetProperty(() => NewComplaintsCount, value, () => RaisePropertyChanged(nameof(ComplaintsCountString))); }
        }

        public bool SeparateWarrantyCards
        {
            get { return GetProperty(() => SeparateWarrantyCards); }
            set { SetProperty(() => SeparateWarrantyCards, value); }
        }

        public bool FreeDelivery
        {
            get { return GetProperty(() => FreeDelivery); }
            set { SetProperty(() => FreeDelivery, value); }
        }

        public int? BasedOnServiceRequestId
        {
            get { return GetProperty(() => BasedOnServiceRequestId); }
            set { SetProperty(() => BasedOnServiceRequestId, value); }
        }

        public ObservableCollection<OrderPaymentRecordViewItem> OrderPayments
        {
            get { return GetProperty(() => OrderPayments); }
            private set { SetProperty(() => OrderPayments, value); }
        }

        public OrderHistoryFilterItem SelectedHistoryFilterItem
        {
            get { return GetProperty(() => SelectedHistoryFilterItem); }
            set { SetProperty(() => SelectedHistoryFilterItem, value); }
        }

        public ObservableCollection<ComplaintViewItem> Complaints
        {
            get { return GetProperty(() => Complaints); }
            private set { SetProperty(() => Complaints, value); }
        }

        public bool IsAddMode
        {
            get { return GetProperty(() => IsAddMode); }
            private set { SetProperty(() => IsAddMode, value, () => RaisePropertiesChanged(nameof(EditInfoVisible), nameof(IsContractorEditable))); }
        }

        public bool AutoFillSources
        {
            get { return GetProperty(() => AutoFillSources); }
            set { SetProperty(() => AutoFillSources, value); }
        }

        public string SelectedTabName
        {
            get { return GetProperty(() => SelectedTabName); }
            set { SetProperty(() => SelectedTabName, value); }
        }

        public bool BufferWarehouseRequired
        {
            get { return GetProperty(() => BufferWarehouseRequired); }
            set { SetProperty(() => BufferWarehouseRequired, value); }
        }

        public bool GuestProductColumnIsVisible => OrderProducts?.Any(x => x.IsGuestProduct) == true && order?.Id > 0;

        public bool IsContractorEditable => IsAddMode && OrderProducts != null && !OrderProducts.Any();

        public bool AnyAssemblies => GetOrderProducts().Any(x => x.Product.Id == Constants.AssemblyServiceProductId);

        public bool OrderBillsVisible => IsInDesignMode || SelectedPayment?.Id == Payment.CashlessNoTaxId || SelectedPayment?.Id == Payment.CashlessTaxId;

        public string Tab3Header => OrderBillsVisible ? "Счета и оплаты" : "Оплаты";

        public bool IsNotReadonlyAssemblyWarehouse => AnyAssemblies &&
            (AssemblyServices.Count == 0 || AssemblyServices.Any(x => x.StateId == AssemblyServiceState.Waiting.Id || x.StateId == AssemblyServiceState.Warehouse.Id));

        public bool IsNotReadonlyAdditionalServiceWarehouse => GetOrderProducts().Any(x => x.IsAdditionalService && (x.TypeId == ProductType.ServiceId || x.TypeId == ProductType.ServiceCertificateId));

        public int? MinPriceFreeDelivery { get; } = null;

        public decimal? MoneyBackAmount => order?.MoneyBackAmount;

        public string CallsCountString => $"{NewCallsCount ?? 0}/{CallsCount ?? 0}";

        public string ServiceRequestsCountString => $"{ServiceRequestsCount ?? 0}";

        public string ExternalPaymentsCountString => $"{ExternalPaymentsCount ?? 0}";

        public string ComplaintsCountString => $"{NewComplaintsCount ?? 0}/{ComplaintsCount ?? 0}";

        public bool IsLockedFromAnyUser => LockerId.HasValue;

        public bool NeedCalculateLogistics => State == OrderStatus.Received || State == OrderStatus.Confirmed;

        public bool IsEditButtonVisible => !IsAddMode && LockerId == null && !IsEditingCompleted;

        public bool FindCustomerVisible => order != null && order.CustomerId is null;

        public bool OpenCustomerVisible => order != null && order.CustomerId != null;

        public bool IsEnabledLegalEntity => order != null && order.LegalEntity is null && order.PaymentId == Payment.CashId && WebClient?.IsOperationAllowed(BusinessOperation.OrderSetLegalEntity) == true;

        public bool IsEnabledOrderSource => order != null && (order.OrderSourceId is null || order.OrderSourceId.Value != OrderSourceType.Site);

        public bool AllowSetNotInStock { get; }

        public bool UserIsOutsourceSeller { get; }

        public bool EditInfoVisible => !IsAddMode && LockerId == null;

        public bool BonusVisible => order?.CustomerId != null || IsInDesignMode;

        public bool AllowEditCarryType => IsLockedByCurrentUserAndEditingAllowed
                                          && (order.BasedOnServiceRequestId.HasValue
                                              || !ExternalPayments.Any()
                                              || ExternalPayments.All(x => Payment.IsEditingAllowed(x.Payment.Id, x.PaymentStateId, x.Payment.Credit, x.Payment.PartialCredit)))
                                          && AllowEditCarryData;

        public bool AllowEditCarryData => State != OrderStatus.Packed;

        public bool ScanVisible => OrderProducts?.Any(x => x.Product?.TypeId == ProductType.GuestProductId) == true;

        public bool AllowOpenServiceRequest { get; }

        public bool OrganizationRecipientVisible => SelectedPayment?.Id == Payment.CashlessNoTaxId || SelectedPayment?.Id == Payment.CashlessTaxId;

        public OrderHistoryFilterItem OrderFilterItem => orderFilterItem ??= new OrderHistoryFilterItem(Id, "Заказ", OrderHistoryType.Order);

        public OrderHistoryFilterItem RemovedProductsFilterItem => removedProductsFilterItem ??= new OrderHistoryFilterItem(Id, "Удаленные товары", OrderHistoryType.DeletedProducts);

        public IBarItem OrdersCheckItem => ordersBarItem ??= GetBarItemForHistoryFilter(OrderFilterItem);

        public IBarItem RemovedProductsCheckItem => removedProductsBarItem ??= GetBarItemForHistoryFilter(RemovedProductsFilterItem);

        #region Order Information

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value, RefreshSummaryItems); }
        }

        public int? ConfirmedBy
        {
            get { return GetProperty(() => ConfirmedBy); }
            set { SetProperty(() => ConfirmedBy, value, RefreshSummaryItems); }
        }

        public int? EmployeePackId
        {
            get { return GetProperty(() => EmployeePackId); }
            set { SetProperty(() => EmployeePackId, value); }
        }

        public int? EmployeeManagerId
        {
            get { return GetProperty(() => EmployeeManagerId); }
            private set { SetProperty(() => EmployeeManagerId, value); }
        }

        #endregion

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        public IAsyncCommand ReceiveGuestProductCommand { get; }

        public IAsyncCommand ReceivedGuestProductCommand { get; }

        public IAsyncCommand GuestProductInfoCommand { get; }

        public IDelegateCommand EditServiceRequestCommand { get; }

        public IDelegateCommand CallCommand { get; }

        public IDelegateCommand AddProductCommand { get; }

        public IDelegateCommand BulkAddProductCommand { get; }

        public IAsyncCommand DropOrderProductCommand { get; }

        public IDelegateCommand DragOrderProductOverCommand { get; }

        public IDelegateCommand CancelCommand { get; }

        public IAsyncCommand DeleteProductCommand { get; }

        public IAsyncCommand SplitProductCommand { get; }

        public IAsyncCommand HandleRowDoubleClickCommand { get; }

        public IDelegateCommand HandlePromoRowDoubleClickCommand { get; }

        public IAsyncCommand OkCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        public IAsyncCommand RemoveSourceCommand { get; }

        public IDelegateCommand SelectDeliveryAddressCommand { get; }

        public IDelegateCommand GetDeliveryDateCommand { get; }

        public IDelegateCommand OpenCustomerCommand { get; }

        public IDelegateCommand SendSmsCommand { get; }

        public IAsyncCommand FindCustomerCommand { get; }

        public IAsyncCommand SetNoProductCommand { get; }

        public IDelegateCommand ShowUklonOrderCommand { get; }

        public IAsyncCommand SetNoPurchaseCommand { get; }

        public IAsyncCommand SetWarehouseCommand { get; }

        public IAsyncCommand SetMovementCommand { get; }

        public IAsyncCommand EditCommand { get; }

        public IDelegateCommand ApplyContractorTemplateCommand { get; }

        public IAsyncCommand RefreshAuditEntriesCommand { get; }

        public IDelegateCommand MailToCommand { get; }

        public IDelegateCommand HandleSelectionChangedCommand { get; }

        public IDelegateCommand HandleSelectionProductInfoChangedCommand { get; }

        public IAsyncCommand GiveCommand { get; }

        public IAsyncCommand PackCommand { get; }

        public IAsyncCommand CancelOrderCommand { get; }

        public IAsyncCommand ConfirmOrderCommand { get; }

        public IAsyncCommand ReceiveCommand { get; }

        public IAsyncCommand ReconfirmOrderCommand { get; }

        public IAsyncCommand UnpackOrderCommand { get; }

        public IAsyncCommand AddPromoCodeCommand { get; }

        public IAsyncCommand RemovePromoCodeCommand { get; }

        public IAsyncCommand RefreshPromoCodesCommand { get; }

        public IAsyncCommand RefreshCallsCommand { get; }

        public IAsyncCommand RefreshServiceRequestsCommand { get; }

        public IAsyncCommand RefreshAssemblyServicesCommand { get; }

        public IAsyncCommand RefreshAdditionalServiceProductsCommand { get; }

        public IAsyncCommand RefreshPromosCommand { get; }

        public IDelegateCommand AddCallCommand { get; }

        public IDelegateCommand EditCallCommand { get; }

        public IAsyncCommand CancelCallCommand { get; }

        public IAsyncCommand ExpireOrderCommand { get; }

        public IAsyncCommand SetExternalOrderCommand { get; }

        public IAsyncCommand EditReceivedDateCommand { get; }

        public IAsyncCommand ClearFiscalRegistarCommand { get; }

        public IAsyncCommand EditContractorCommand { get; }

        public IAsyncCommand EditCreatedByCommand { get; }

        public IAsyncCommand EditProductAddedByCommand { get; }

        public IAsyncCommand PrintOrderCommand { get; }

        public IAsyncCommand PrintAssemblyCommand { get; }

        public IAsyncCommand PrintWarrantyCardCommand { get; }

        public IAsyncCommand PrintTrackNumberCommand { get; }

        public IAsyncCommand PrintChequeCommand { get; }

        public IAsyncCommand PrintActIncomeCommand { get; }

        public IAsyncCommand PrintActOutcomeCommand { get; }

        public IDelegateCommand HandleHistoryFilterLoadedCommand { get; }

        public IDelegateCommand SetHistoryFilterItemCommand { get; }

        public IAsyncCommand RefreshOrderBillsCommand { get; }

        public IAsyncCommand CancelOrderBillCommand { get; }

        public IDelegateCommand CreateOrderBillCommand { get; }

        public IAsyncCommand RecalculateOrderBillCommand { get; }

        public IDelegateCommand ShowProductHistoryCommand { get; }

        public IAsyncCommand CreateManyServiceRequestCommand { get; }

        public IAsyncCommand SetNotInStockCommand { get; }

        public IDelegateCommand CalculateLogisticsCommand { get; }

        public IAsyncCommand ShowStatusPayCommand { get; }

        public IAsyncCommand CancelExternalPaymentCommand { get; }

        public IAsyncCommand EditExternalPaymentCommand { get; }

        public IAsyncCommand RefreshOrderPaymentsCommand { get; }

        public IAsyncCommand RefreshExternalPaymentsCommand { get; }

        public IDelegateCommand AddOrderPaymentCommand { get; }

        public IDelegateCommand AddOrderPaymentViaCashRegistrarCommand { get; }

        public IDelegateCommand AddOrderWithdrawCommand { get; }

        public IAsyncCommand RefreshCrmCommand { get; }

        public IDelegateCommand ResendSmsCommand { get; }

        public IAsyncCommand FillSourcesCommand { get; }

        public IDelegateCommand EditOrderPaymentCommand { get; }

        public IAsyncCommand FillSourcesManualCommand { get; }

        public IDelegateCommand CreateServiceRequestCommand { get; }

        public IAsyncCommand GenerateAssembledComputerRuleCommand { get; }

        public IAsyncCommand EditInfoCommand { get; }

        public IDelegateCommand OpenAssemblyServiceCommand { get; }

        public IDelegateCommand OpenAdditionalServiceProductCommand { get; }

        public IAsyncCommand SetNewOrderProductStateCommand { get; }

        public IAsyncCommand SetClarifyOrderProductStateCommand { get; }

        public IAsyncCommand SetAgreedOrderProductStateCommand { get; }

        public IAsyncCommand SetAgreedToAllOrderProductStateCommand { get; }

        public IDelegateCommand ResetProductPricesCommand { get; }

        public IDelegateCommand SetProductPricesCommand { get; }

        public IAsyncCommand RefreshComplaintsCommand { get; }

        public IDelegateCommand AddComplaintCommand { get; }

        public IAsyncCommand EditAssemblyCommand { get; }

        public IAsyncCommand CreateAssemblyWithProductsCommand { get; }

        public IAsyncCommand AddAssemblyCommand { get; }

        public IAsyncCommand AddAssembledComputerRuleCommand { get; }

        public IAsyncCommand AddCustomAssemblyCommand { get; }

        public IDelegateCommand AddFolderCommand { get; }

        public IAsyncCommand PrintAssemblyReportCommand { get; }

        public IAsyncCommand AddBundleCommand { get; }

        public IAsyncCommand DeleteBundleCommand { get; }

        public IAsyncCommand CheckCompatibilityCommand { get; }

        public IAsyncCommand PaymentControlCommand { get; }

        public IAsyncCommand PrintOrderPaymentFiscalChequeCommand { get; }

        public IAsyncCommand AddBonusCommand { get; }

        public IAsyncCommand EditBonusCommand { get; }

        public IAsyncCommand RemoveBonusCommand { get; }

        public IDelegateCommand CreditCommand { get; }

        public IAsyncCommand CopyCreditDataCommand { get; }

        public IAsyncCommand PrintBillCommand { get; }

        public IAsyncCommand PrintBillInvoiceCommand { get; }

        public IAsyncCommand SendChequeCommand { get; }

        public IAsyncCommand SendOrderChequeToEmailCommand { get; }

        public IAsyncCommand CalculateAssemblyServiceProductCommand { get; }

        public DocumentCommands DocumentCommands { get; }

        public IAsyncCommand RefreshDocumentsCommand { get; }

        public IDelegateCommand ShowDocumentsBotQrCommand { get; }

        public IAsyncCommand PrintDocumentCommand { get; }

        public IDelegateCommand AddDocumentCommand { get; }

        public IAsyncCommand RemoveDocumentCommand { get; }

        public IDictionaries Dictionaries { get; }

        #endregion

        #endregion

        public bool NotifyBySms
        {
            get { return GetProperty(() => NotifyBySms); }
            set { SetProperty(() => NotifyBySms, value); }
        }

        #region Services

        private IDocumentManagerService NonModalSizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("NotModalSizeableDocumentManagerService");

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService NonModalDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalDialogDocumentManagerService");

        private IDialogService CreateManyServiceRequestWizardDialogService => GetService<IDialogService>("CreateManyServiceRequestWizardDialogService");

        private SaveFileDialogService SaveFileDialogService => (SaveFileDialogService)GetService<ISaveFileDialogService>("XlsSaveFileDialogService", ServiceSearchMode.PreferParents);

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>();

        #endregion

        private IMessenger Messenger { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IMediator Mediator { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IOrderRules OrderRules { get; }

        private IOrderGiveHelper OrderGiveHelper { get; }

        private IRroPrintHelper RroPrintHelper { get; }

        private IWebClient WebClient { get; }

        private IMapper Mapper { get; }

        private IErrorHandler ErrorHandler { get; }

        private IFiscalRegistrarClientFactory FiscalRegistrarClientFactory { get; }

        private LockableOperationProcessor<OrderDto> LockableOperationProcessor { get; }

        private ILogger<OrderViewModel> Logger { get; }

        private NovaPayOptions NovaPayOptions { get; }

        private bool IsInitMode { get; set; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        private async Task HandleLoadedAsync()
        {
            await ShowModuleAnalyticsViewAsync();
        }

        public bool IsAllProductsWithNoneOrNoProductSource()
        {
            return GetOrderProducts().All(x => x.Source is NoneOrderProductSource || x.Source is NoProductOrderProductSource);
        }

        public IEnumerable<OrderProductViewModel> GetOrderProducts()
        {
            return OrderProducts?.Where(x => !x.IsVirtualProduct);
        }

        IEnumerable<IOrderPaymentInfoProduct> IOrderPaymentInfo.GetOrderProducts()
        {
            return GetOrderProducts();
        }

        public IEnumerable<IOrderPayment> GetOrderPayments()
        {
            return OrderPayments;
        }

        public int GetBonusesQuantity()
        {
            return Bonuses?.Sum(x => x.Quantity) ?? 0;
        }

        public int GetBonusesToChargeQuantity()
        {
            return OrderProducts.Sum(x => (x.BonusesToCharge ?? 0) * x.Quantity);
        }

        public async Task InitializeAddAsync(OrderDto orderToAdd)
        {
            order = orderToAdd;
            Id = orderToAdd.Id;

            await InitializeAsync();

            Title = "Создание нового заказа";
            IsEditingCompleted = false;

            RefreshPaymentInfo();
            SetDisabledFields();

            IsLockedByCurrentUserAndEditingAllowed = true;
            IsAddMode = true;

            OnInitializeCompleted();
        }

        public async Task InitializeEditAsync(int orderId, bool editMode = false)
        {
            Id = orderId;

            await InitializeAsync();

            Title = $"Заказ №{Id.ToString(CultureInfo.InvariantCulture)} от {CreatedOn:g}";
            IsEditingCompleted = !OrderRules.IsOrderCanBeChanged(this);

            SetDisabledFields();
            RefreshPaymentInfo();

            OnInitializeCompleted();

            if (IsEditButtonVisible && editMode)
            {
                await EditAsync();
            }
        }

        public void OnClose(CancelEventArgs e)
        {
            Messenger.Send(new ModuleAnalyticsCloseMessage(Id));
        }

        public void OnDestroy()
        {
            if (IsAddMode)
            {
                Messenger.Send(new OnOrderCreationFinishedMessage());
            }
            else if (IsLockedByCurrentUserAndEditingAllowed || LockerId == WebClient.AuthenticatedEmployee.Id)
            {
                try
                {
                    LockResponse<OrderDto> response = WebClient.ExecuteApiRequest(new UnlockOrder(Id));

                    Messenger.Send(new OrderMessage(response.Dto, MessageType.Changed));

                    if (!response.Success)
                    {
                        MessageFacadeService.ShowNotificationError("Не удалось разблокировать заказ");
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to unlock entity");
                    MessageFacadeService.ShowNotificationError("Ошибка при разблокировании заказа");
                }
            }

            Messenger.Unregister<CallMessage>(this, OnCallMessage);
            Messenger.Unregister<OrderPaymentMessage>(this, OnOrderPaymentMessage);
        }

        public void RefreshPaymentInfo()
        {
            OrderPaymentViewModel.CalcPaymentInfo(this);
        }

        public void RefreshSummaryItems()
        {
            OrderSummaryItems = GetSummaryItems();
        }

        public void OrderProductsChanged()
        {
            if (AssemblyWarehouse == null && AnyAssemblies && IsLockedByCurrentUserAndEditingAllowed)
            {
                AssemblyWarehouse = AssemblyWarehouses.FirstOrDefault(x => x.Id == SelectedWarehouse?.AssemblyWarehouseId);
            }

            RaisePropertiesChanged(nameof(OrderProducts), nameof(DeliveryTime), nameof(DeliveryTimeTo), nameof(IsContractorEditable));
            RefreshPaymentInfo();
            FillHistoryFilter();
            ResetCarryTypeValidation();
        }

        public IEnumerable<OrderProductViewModel> GetGiftOrderProducts(int parentId)
        {
            return GetOrderProducts().Where(x => x.ParentRecordId.HasValue && x.ParentRecordId.Value == parentId && x.IsGift);
        }

        public IEnumerable<OrderProductViewModel> GetChildOrderProducts(int parentId)
        {
            return GetOrderProducts().Where(x => x.ParentRecordId.HasValue && x.ParentRecordId.Value == parentId);
        }

        public IEnumerable<OrderProductViewModel> GetAdditionalServiceOrderProductsAndAdditionalServiceConsumableProducts(int parentId)
        {
            var additionalServiceProducts = GetOrderProducts()
                .Where(x => x.ParentRecordId.HasValue
                            && x.ParentRecordId.Value == parentId
                            && x.IsAdditionalService);

            foreach (OrderProductViewModel additionalServiceProduct in additionalServiceProducts)
            {
                yield return additionalServiceProduct;

                foreach (OrderProductViewModel orderProductViewModel in GetOrderProducts().Where(x => x.ParentRecordId == additionalServiceProduct.Id))
                {
                    yield return orderProductViewModel;
                }
            }
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            Id = 1;

            ShowLoadingIndicator = false;

            order = new OrderDto();

            FirstName = "Jhon";
            LastName = "Doe";
            Phone = "0954563290";
            Phone2 = "0934563211";
            Email = "test@test.com";
            Rt = 1;
            BasedOnServiceRequestId = 13967;

            DeliveryTime = DateTime.Now;
            DeliveryTimeTo = DateTime.Now.AddDays(1).AddHours(1);

            ReceiveTime = DateTime.Now;

            IsLockedOrEdidingCompleted = true;

            WindowStatusInfo = "Редактирует: ";
            LockPerson = "Pravdivy Alexander";

            State = new ReceivedOrderStatus();
            OrderPaymentViewModel = new OrderPaymentInfoViewModel();

            employeeNames = new Dictionary<int, string>();
            additionalServices = new Dictionary<int, AdditionalServiceDto>();
        }

        private static bool PaymentOnNonVirtualCashbox(IReadOnlyCollection<OrderPaymentDto> orderPaymentDtos, IReadOnlyCollection<CashboxDto> cashboxes)
        {
            int[] nonVirtualCashboxIds = cashboxes
                .Where(x => x.TypeId != CashboxType.Virtual.Id)
                .Select(x => x.Id)
                .ToArray();

            return orderPaymentDtos.Any(x => nonVirtualCashboxIds.Contains(x.CashboxId));
        }

        private static bool PaymentOnVirtualCashbox(IReadOnlyCollection<OrderPaymentDto> orderPaymentDtos, IReadOnlyCollection<CashboxDto> cashboxes)
        {
            int[] virtualCashboxIds = cashboxes
                .Where(x => x.TypeId == CashboxType.Virtual.Id)
                .Select(x => x.Id)
                .ToArray();

            return orderPaymentDtos.Any(x => virtualCashboxIds.Contains(x.CashboxId));
        }

        private static bool IsOrderReadyToPack(OrderDto orderObj)
        {
            return orderObj.StateId == OrderStatus.Confirmed.Id
                && orderObj.Products.Any()
                && orderObj.Products.All(op => IsOrderProductReadyToPack(orderObj, op));
        }

        private static bool IsAssemblyOrAssembledComputerRuleFolder(OrderFolderDto orderFolder)
        {
            return orderFolder != null && (orderFolder.TypeId == OrderFolderType.AssemblyServiceId || orderFolder.TypeId == OrderFolderType.AssembledComputerRuleId);
        }

        private static bool IsOrderReadyToGive(OrderDto orderObj)
        {
            return orderObj.StateId == OrderStatus.Packed.Id
                && orderObj.CarryId == CarryType.PickupId
                && orderObj.Products.Any()
                && orderObj.Products.All(op => IsOrderProductReadyToPack(orderObj, op));
        }

        private static IEnumerable<ValidationResultItem> CanGetDeliveryDate(OrderViewModel x, ITrackNumberProvider provider)
        {
            if (x.SelectedWarehouse == null)
            {
                yield return new ValidationResultItem("Поле Склад должно быть заполнено", true);
            }

            if (x.SelectedCity == null)
            {
                yield return new ValidationResultItem("Поле Город должно быть заполнено", true);
            }

            if (x.SelectedCarryType == null)
            {
                yield return new ValidationResultItem("Поле Доставка должно быть заполнено", true);
            }

            if (string.IsNullOrEmpty(x.Address))
            {
                yield return new ValidationResultItem("Поле Адрес должно быть заполнено", true);
            }

            if (!provider.SupportGetDeliveryDate)
            {
                yield return new ValidationResultItem("Способ доставки не поддерживает функцию расчета даты доставки", true);
            }
        }

        private static BonusConfirmationViewItem MapBonuses(OrderProductDto orderProduct, IReadOnlyDictionary<int, Stack<OrderProductBonusDto>> bonusesDictionary)
        {
            BonusConfirmationViewItem bonusConfirmationViewItem = new BonusConfirmationViewItem
            {
                MaxQuantity = orderProduct.MaxBonusesToUse ?? 0,
                OrderProductId = orderProduct.Id,
                ProductId = orderProduct.Product.Id,
                Price = orderProduct.Price,
                PriceCurrent = orderProduct.PriceOut,
                ProductName = orderProduct.Product.GetLocalName(LocalizableNameType.Ukr)
            };

            if (bonusesDictionary.ContainsKey(orderProduct.Id) && bonusesDictionary[orderProduct.Id].Any())
            {
                OrderProductBonusDto bonusDto = bonusesDictionary[orderProduct.Id].Pop();

                bonusConfirmationViewItem.Id = bonusDto.Id;
                bonusConfirmationViewItem.Quantity = bonusDto.Quantity;
                bonusConfirmationViewItem.PriceCurrent += bonusDto.Quantity;
            }

            return bonusConfirmationViewItem;
        }

        private static IEnumerable<CallViewItem> GetSortedCalls(IEnumerable<CallViewItem> calls)
        {
            return calls.OrderByDescending(x => x.CompletedOn == null)
                .ThenByDescending(x => x.CompletedOn)
                .ThenByDescending(x => x.Id);
        }

        private static bool CanSendSms(string phone)
        {
            return !string.IsNullOrWhiteSpace(phone);
        }

        private static bool IsOrderProductReadyToPack(OrderDto orderObj, OrderProductDto orderProdObj)
        {
            return orderProdObj.StateId == OrderProductStatus.Agreed.Id &&
                   orderProdObj.SourceId == OrderProductSourceType.WarehouseSource.Id &&
                   orderProdObj.WarehouseId == orderObj.WarehouseId;
        }

        private static OrderSaveDto MapToSaveDto(OrderViewModel source, OrderSaveDto target)
        {
            target.Id = source.Id;
            target.LastName = source.LastName;
            target.FirstName = source.FirstName;
            target.MiddleName = source.MiddleName;
            target.Address = source.Address;
            target.DeliveryData = source.DeliveryData;
            target.CarryId = source.SelectedCarryType.Id;
            target.Email = source.Email;
            target.EmployeeComment = source.EmployeeComment;
            target.Phone = source.Phone;
            target.Phone2 = source.Phone2;
            target.WarehouseId = source.SelectedWarehouse?.Id;
            target.BufferWarehouseId = source.BufferWarehouseId;
            target.AssemblyWarehouseId = source.AssemblyWarehouse?.Id;
            target.AdditionalServiceWarehouseId = source.SelectedAdditionalServiceWarehouse?.Id;
            target.CityId = source.SelectedCity?.Id;
            target.OrderProducts = source.GetOrderProducts().Select(x => MapToOrderProductSave(x, new OrderProductSaveDto())).ToList();
            target.Folders = source
                .GetOrderProducts()
                .Where(x => x.OrderFolder != null)
                .GroupBy(x => x.OrderFolderId)
                .Select(x => MapToOrderFolderSaveDto(x.First().OrderFolder))
                .ToList();
            target.PromoCodes = source.PromoCodes.Select(x => new OrderProductPromoCodeSaveDto { Id = x.Id, PromoCodeId = x.PromoCodeId }).ToList();
            target.Options = new OrderOptionsDto
            {
                SeparateWarrantyCards = source.SeparateWarrantyCards,
                FreeDelivery = source.FreeDelivery,
                DontCall = source.DontCall,
                OrganizationRecipient = source.OrganizationRecipient
            };

            target.LegalEntityId = source.SelectLegalEntityId;
            target.OrderSourceId = source.SelectedOrderSource?.Id;

            return target;
        }

        private static OrderFolderSaveDto MapToOrderFolderSaveDto(OrderFolderDto orderFolder)
        {
            return new OrderFolderSaveDto(
                orderFolder.Id,
                orderFolder.ProductId,
                orderFolder.Quantity,
                orderFolder.Name,
                orderFolder.TypeId,
                orderFolder.Price,
                orderFolder.BonusesToCharge,
                orderFolder.PriceId,
                orderFolder.FreeDelivery);
        }

        private static OrderProductSaveDto MapToOrderProductSave(OrderProductViewModel source, OrderProductSaveDto target)
        {
            target.Id = source.Id;
            target.CurrencyOutId = source.CurrencyOutId;
            target.CurrencyId = source.CurrencyId;
            target.PriceOut = source.PriceOut;
            target.Price = source.IsNew ? source.Price : null;
            target.PriceId = source.PriceId;
            target.Price1C = source.Price1C;
            target.ProductId = source.Product.Id;
            target.Quantity = source.Quantity;
            target.ParentRecordId = source.ParentRecordId;
            target.IsAdditionalService = source.IsAdditionalService;
            target.IsAdditionalServiceConsumable = source.IsAdditionalServiceConsumable;
            target.IsGift = source.IsGift;
            target.OrderPromoCodeId = source.OrderPromoCodeId;
            target.PromoDiscount = source.PromoDiscount;
            target.AssemblyId = source.AssemblyId;
            target.AssemblyQuantity = source.AssemblyQuantity;
            target.OrderFolderId = source.OrderFolderId;
            target.AssemblyIncluded = source.AssemblyIncluded;
            target.InitiatedBy = source.InitiatedBy;
            target.AdditionalWarranty = source.AdditionalWarranty;
            target.MaxBonusesToUse = source.MaxBonusesToUse;
            target.BonusesToCharge = source.BonusesToCharge;

            return target;
        }

        private OrderCreateDto MapToCreateDto(OrderViewModel source, OrderCreateDto target)
        {
            target = (OrderCreateDto)MapToSaveDto(source, target);

            target.ClientId = source.SelectedContractor.Id;
            target.PaymentId = source.SelectedPayment.Id;
            target.FillSources = source.AutoFillSources;
            target.NotifyBySms = source.NotifyBySms;
            target.WorkPlaceId = WebClient.WorkPlaceId;

            return target;
        }

        private void Call(CallViewItem viewItem)
        {
            Messenger.Send(new OutcomingCallViewMessage(viewItem));
        }

        private async Task InitializeAsync()
        {
            IsInitMode = true;
            IsOrderProductsValid = true;
            AutoFillSources = true;
            NotifyBySms = true;

            await FetchDataAsync();

            MapOrder();

            SelectedSubdivision = Subdivisions.FirstOrDefault(x => x.Id == order.SubdivisionId);
            SelectedPayment = Payments.FirstOrDefault(x => x.Id == order.PaymentId);
            SelectedContractor = Contractors.FirstOrDefault(x => x.Id == order.ClientId);

            if (SelectedContractor == null)
            {
                ContractorDto contractor = contractors.FirstOrDefault(x => x.Id == order.ClientId);

                if (contractor != null)
                {
                    OrderContractorViewItem contractorViewItem = Mapper.Map<OrderContractorViewItem>(contractor);
                    contractorViewItem.Valid = true;
                    Contractors.Add(contractorViewItem);
                    SelectedContractor = contractorViewItem;
                }
            }

            EmployeeManagerId = order.ManagerEmployeeId;

            SelectedCity = Cities.FirstOrDefault(x => x.Id == order.CityId);
            SelectedCarryType = CarryTypes.FirstOrDefault(x => x.Id == order.CarryId);

            if (order.WarehouseId.HasValue)
            {
                SelectedWarehouse = Warehouses.FirstOrDefault(x => x.Id == order.WarehouseId);
            }

            if (order.AssemblyWarehouseId.HasValue)
            {
                AssemblyWarehouse = AssemblyWarehouses.FirstOrDefault(x => x.Id == order.AssemblyWarehouseId);
            }

            if (order.AdditionalServiceWarehouseId.HasValue)
            {
                SelectedAdditionalServiceWarehouse = AdditionalServiceWarehouses.FirstOrDefault(x => x.Id == order.AdditionalServiceWarehouseId);
            }

            if (order.OrderSourceId.HasValue)
            {
                SelectedOrderSource = Mapper.Map<OrderSourceTypeViewItem>(OrderSources.FirstOrDefault(x => x.Id == order.OrderSourceId));
            }
        }

        private void OnInitializeCompleted()
        {
            RefreshSummaryItems();
            ResetValidation();
            IsInitMode = false;
        }

        private void AddProduct()
        {
            if (!IsLockedByCurrentUserAndEditingAllowed)
            {
                return;
            }

            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                SelectedContractor.Id,
                NomenclatureViewSelectionMode.ByQuantity,
                queryGifts: OrderRules.NeedCheckGifts(SelectedSubdivision),
                queryAdditionalServices: true,
                includePriceJson: true,
                cartProductIds: GetOrderProducts().Select(x => x.ProductId).ToArray());

            NomenclatureViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (viewModel.IsOk)
            {
                ProcessSelectedForAddingProducts(viewModel.GetSelectedItems().ToArray());
            }
        }

        private void BulkAddProduct()
        {
            if (!IsLockedByCurrentUserAndEditingAllowed)
            {
                return;
            }

            OrderProductBulkAddParameter parameter = new OrderProductBulkAddParameter(
                NomenclatureViewPriceContext.Client,
                SelectedContractor.Id,
                OrderRules.NeedCheckGifts(SelectedSubdivision),
                true,
                GetOrderProducts().Select(x => x.ProductId).ToArray());

            OrderProductBulkAddViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<OrderProductBulkAddViewModel>(parameter, this);

            if (viewModel.IsOk)
            {
                ProcessSelectedForAddingProducts(viewModel.NomenclatureItemsToAdd);
            }
        }

        private bool CanPrintBillInvoice()
        {
            bool isNovaPoshta = CarryType.IsNovaposhta(SelectedCarryType?.Id ?? 0);
            bool warehouseNotUseCell = SelectedWarehouse != null && !SelectedWarehouse.UseCells;

            return OrderBills?.All(x => x.StateId == OrderBillState.Paid.Id) == true
                   && (order?.StateId == OrderStatus.Done.Id
                       || (order?.StateId == OrderStatus.Packed.Id && (warehouseNotUseCell || isNovaPoshta)));
        }

        private bool CanAddProduct()
        {
            return IsLockedByCurrentUserAndEditingAllowed
                   && SelectedSubdivision != null
                   && SelectedContractor != null
                   && State.CanAddProduct
                   && (!ExternalPayments.Any() || ExternalPayments.Any(x => Payment.IsEditingAllowed(x.Payment.Id, x.PaymentStateId, x.Payment.Credit, x.Payment.PartialCredit)));
        }

        private bool CanCreateAssemblyWithProducts()
        {
            if (CanAddProduct()
                && SelectedProducts.Any()
                && SelectedProducts.All(x => !x.IsAssembly()))
            {
                int[] parentIds = SelectedProducts
                    .Where(x => x.IsAdditionalService || x.IsGift)
                    .Select(x => x.ParentRecordId)
                    .OfType<int>()
                    .ToArray();

                var selectedParentsCount = SelectedProducts
                    .Select(x => x.Id)
                    .Intersect(parentIds)
                    .Count();

                if (selectedParentsCount == parentIds.Count())
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanCalculateAssemblyServiceProductPrice(OrderProductViewModel viewModel)
        {
            if (viewModel == null)
            {
                return false;
            }

            int? orderFolderType = order.Folders?.FirstOrDefault(x => x.Id == viewModel.OrderFolderId)?.TypeId;
            var assemblyServiceProduct = GetOrderProducts().FirstOrDefault(x => x.OrderFolderId == viewModel.OrderFolderId && x.ProductId == Constants.AssemblyServiceProductId);

            return orderFolderType.HasValue
                   && orderFolderType != OrderFolderType.AssembledComputerRuleId
                   && IsLockedByCurrentUserAndEditingAllowed
                   && viewModel is { OrderFolderId: not null }
                   && assemblyServiceProduct?.PriceCanBeChanged == true;
        }

        private void Cancel()
        {
            Close();
        }

        private void RefreshBonusSummary()
        {
            BonusSummary = Bonuses?
                .Union(OrderPayments
                    .Where(z => z.BonusTypeId == BonusType.TradeInPointsId)
                    .Select(x => new OrderProductBonusDto()
                    {
                        Id = 0,
                        OrderProductId = 0,
                        BonusTypeId = x.BonusTypeId.Value,
                        OrderId = order.Id,
                        Quantity = (int)x.Amount * x.Sign,
                        CreatedBy = x.CreatedBy,
                        CreatedOn = x.CreatedOn,
                        ModifiedBy = x.CreatedBy,
                        ModifiedOn = x.CreatedOn
                    }))
                .GroupBy(x => x.BonusTypeId)
                .Select(x => new OrderBonusSummaryViewItem
                {
                    BonusTypeId = x.Key,
                    Quantity = x.Sum(y => y.Quantity),
                    ModifiedBy = x.First().ModifiedBy,
                    ModifiedOn = x.First().ModifiedOn
                }).Where(x => x.Quantity != 0);
        }

        private bool CanDeleteOrderProducts(IEnumerable<OrderProductViewModel> orderProducts)
        {
            if (orderProducts is null || orderProducts.Count() < 1)
            {
                return false;
            }

            if (orderProducts.Any(x => x.OrderFolderId != null && x.OrderFolder.TypeId == OrderFolderType.BundleId))
            {
                return false;
            }

            if (orderProducts.All(x =>
                {
                    AssemblyServiceDto assemblyService = AssemblyServices.FirstOrDefault(y => x != null && y.Products.Any(z => z.OrderProductId == x.Id));

                    return IsLockedByCurrentUserAndEditingAllowed
                           && x != null
                           && x.State.CanBeRemoved
                           && (x.ParentId == null
                               || x.IsAdditionalService
                               || (x.IsAssembly()
                                   && (assemblyService is null || ((assemblyService.StateId == AssemblyServiceState.Waiting.Id
                                                                    || assemblyService.StateId == AssemblyServiceState.Warehouse.Id
                                                                    || !x.AssemblyIncluded)
                                                                    && x.Product.TypeId != ProductType.AssemblyServiceId))))
                           && (!ExternalPayments.Any() || ExternalPayments.Any(y => Payment.IsEditingAllowed(y.Payment.Id, y.PaymentStateId, y.Payment.Credit, y.Payment.PartialCredit)))
                           && (x.ParentRecordId != null
                               || GetGiftOrderProducts(x.Id).All(x => x.State.CanBeRemoved)
                               || GetAdditionalServiceOrderProductsAndAdditionalServiceConsumableProducts(x.Id).All(y => y.State.CanBeRemoved));
                }))
            {
                return true;
            }

            return false;
        }

        private bool CanShowCredit()
        {
            return Id > 0
                   && LockerId == null
                   && order != null
                   && order.BasedOnServiceRequestId is null
                   && SelectedSubdivision?.Id == Subdivision.Telemart.Id
                   && ((order.StateId != OrderStatus.DidNotOrder.Id
                        && order.StateId != OrderStatus.Returned.Id
                        && order.StateId != OrderStatus.Done.Id
                        && order.StateId != OrderStatus.Canceled.Id
                        && order.StateId != OrderStatus.DidNotTake.Id)
                       || order.ExternalPayments?
                           .Where(x => Dictionaries.GetItemById<Payment>(x.PaymentId).Credit)
                           .Any() == true)
                   && order?.Products?.Any() == true;
        }

        private bool CanCopyCreditData()
        {
            return Id > 0
                   && SelectedExternalPayment?.Params?.CreditOfferId > 0
                   && order.Products?.Any() == true;
        }

        private bool CanGenerateAssembledComputerRule()
        {
            return CurrentOrderProduct != null
                   && CurrentOrderProduct?.TypeId == ProductType.AssembledComputerRuleId
                   && IsLockedByCurrentUserAndEditingAllowed
                   && SelectedSubdivision != null
                   && SelectedContractor != null
                   && State.CanAddProduct;
        }

        private void Close()
        {
            DocumentOwner.Close(this, false);
        }

        private async Task DeleteOrderProductsAsync(IEnumerable<OrderProductViewModel> orderProducts)
        {
            if (!IsLockedByCurrentUserAndEditingAllowed)
            {
                return;
            }

            foreach (var orderProduct in orderProducts.ToArray())
            {
                if (orderProduct.OrderFolder != null && orderProduct.Quantity % orderProduct.OrderFolder.Quantity != 0)
                {
                    MessageFacadeService.ShowNotificationWarning("Количество товара должно быть кратно количеству сборок");
                    continue;
                }

                if (orderProduct.IsAssembly()
                    && (orderProduct.AssemblyIncluded || orderProduct.IsVirtualProduct)
                    && (orderProduct.Id > 0
                        || (orderProduct.IsVirtualProduct && OrderProducts.Any(x => x.ParentId == orderProduct.Id && x.Id > 0))))
                {
                    OrderCanEditAssemblyResponse orderCanEditAssemblyResponse = await CanEditAssemblyAsync(orderProduct.OrderFolderId!.Value);

                    if (orderCanEditAssemblyResponse == null)
                    {
                        continue;
                    }
                }

                List<OrderProductViewModel> additionalServiceOrderProducts = OrderProducts
                    .Where(x => x.ParentRecordId == orderProduct.Id && x.IsAdditionalService)
                    .ToList();

                bool isChildAdditionalServiceProductCompleted = AdditionalServiceProducts
                    .Where(x => x.OrderProductId.HasValue)
                    .Any(x => additionalServiceOrderProducts
                    .Select(y => y.Id)
                    .Contains(x.OrderProductId!.Value) && x.StateId == AdditionalServiceProductState.CompletedId);

                if (isChildAdditionalServiceProductCompleted)
                {
                    if (!MessageFacadeService.Confirm("К товару относится услуга в статусе \"Завершена\". Из заказа будет удален только товар. Продолжить?"))
                    {
                        continue;
                    }
                }

                bool isChildAdditionalServiceProductDoing = AdditionalServiceProducts
                    .Where(x => x.OrderProductId.HasValue)
                    .Any(x => additionalServiceOrderProducts
                        .Select(y => y.Id)
                        .Contains(x.OrderProductId!.Value)
                        && x.StateId == AdditionalServiceProductState.DoingId);

                if (isChildAdditionalServiceProductDoing)
                {
                    MessageFacadeService.ShowNotificationError("Невозможно удалить. К товару относится услуга в статусе \"Выполняется\"");
                    continue;
                }

                var state = GetAdditionalServiceProduct(orderProduct.Id)?.StateId;

                if (state == AdditionalServiceProductState.CompletedId || state == AdditionalServiceProductState.DoingId)
                {
                    MessageFacadeService.ShowNotificationError($"Услуга находится в статусе {(state == AdditionalServiceProductState.CompletedId ? "\"Завершена\"" : "\"Выполняется\"")}");
                    continue;
                }

                if (orderProduct.ParentRecordId.HasValue)
                {
                    OrderProductViewModel parentOrderProduct = OrderProducts.FirstOrDefault(x => x.Id == orderProduct.ParentRecordId);

                    if (parentOrderProduct != null)
                    {
                        var parentState = GetAdditionalServiceProduct(parentOrderProduct.Id)?.StateId;

                        if (parentState == AdditionalServiceProductState.CompletedId || parentState == AdditionalServiceProductState.DoingId)
                        {
                            MessageFacadeService.ShowNotificationError($"Товар был оказан на услугу, которая находится в статусе {(state == AdditionalServiceProductState.CompletedId ? "\"Завершена\"" : "\"Выполняется\"")}");
                            continue;
                        }
                    }
                }

                if (additionalServiceOrderProducts.Any()
                    || GetAdditionalServiceProduct(orderProduct.Id) is not null
                    || (orderProduct.ParentRecordId.HasValue && GetAdditionalServiceProduct(orderProduct.ParentRecordId.Value) is not null))
                {
                    OrderCanEditAdditionalServiceResponse orderCanEditAdditionalServiceResponse = await CanEditAdditionalServiceAsync(orderProduct.Id);

                    if (orderCanEditAdditionalServiceResponse == null)
                    {
                        continue;
                    }
                }

                List<OrderProductViewModel> orderProductsToRemove;

                if (orderProduct.IsVirtualProduct)
                {
                    orderProductsToRemove = OrderProducts.Where(x => x.OrderFolderId == orderProduct.OrderFolderId).ToList();

                    OrderProductViewModel[] completedAdditionalServiceProducts = orderProductsToRemove
                        .Where(x => IsAdditionalServiceProductCompleted(x.Id))
                        .ToArray();

                    int?[] completedAdditionalServiceProductsParentRecordIds = completedAdditionalServiceProducts
                        .Select(x => x.ParentRecordId)
                        .ToArray();

                    int[] completedAdditionalServiceProductIds = completedAdditionalServiceProducts
                        .Select(x => x.Id)
                        .ToArray();

                    IEnumerable<OrderProductViewModel> parentAdditionalServiceProducts = GetOrderProducts()
                        .Where(x => completedAdditionalServiceProductsParentRecordIds.Contains(x.Id));

                    IEnumerable<OrderProductViewModel> consumableAdditionalServiceProducts = GetOrderProducts()
                        .Where(x => x.ParentRecordId.HasValue && completedAdditionalServiceProductIds.Contains(x.ParentRecordId.Value));

                    IReadOnlyCollection<OrderProductViewModel> completedAdditionalServiceProductsWithParentAndConsumables = completedAdditionalServiceProducts
                        .Union(parentAdditionalServiceProducts)
                        .Union(consumableAdditionalServiceProducts)
                        .ToArray();

                    if (completedAdditionalServiceProductsWithParentAndConsumables.Any())
                    {
                        MessageFacadeService.ShowNotificationInfo("Завершенная услуга из сборки и товар, на который она была оказана не удалены");

                        completedAdditionalServiceProductsWithParentAndConsumables.ForEach(x =>
                        {
                            x.OrderFolderId = null;
                            x.ParentId = null;
                            orderProductsToRemove.Remove(x);
                        });
                    }
                }
                else
                {
                    orderProductsToRemove = OrderProducts
                        .Where(x => x.Id == orderProduct.Id || (x.ParentRecordId == orderProduct.Id && !isChildAdditionalServiceProductCompleted))
                        .ToList();

                    int[] additionalServiceOrderProductIds = additionalServiceOrderProducts.Select(y => y.Id).ToArray();

                    OrderProductViewModel[] consumableAdditionalServiceProducts = OrderProducts
                        .Where(x => x.ParentRecordId.HasValue && additionalServiceOrderProductIds.Contains(x.ParentRecordId.Value) && x.IsAdditionalServiceConsumable)
                        .ToArray();

                    orderProductsToRemove.AddRange(consumableAdditionalServiceProducts);
                }

                if (!orderProduct.IsVirtualProduct && orderProduct.OrderFolder != null)
                {
                    OrderProductViewModel[] orderProductsToChange = OrderProducts
                        .Where(x => x.OrderFolderId == orderProduct.OrderFolderId
                        && x.ProductId == orderProduct.ProductId
                        && x.Id != orderProduct.Quantity)
                        .ToArray();

                    int newQuantity = orderProductsToChange.Sum(x => x.Quantity);

                    int newAssemblyQuantity = newQuantity / orderProduct.OrderFolder.Quantity;

                    foreach (OrderProductViewModel op in orderProductsToChange)
                    {
                        op.AssemblyQuantity = newAssemblyQuantity;
                    }
                }

                if (orderProduct.OrderPromoCodeId.HasValue)
                {
                    OrderPromoCodeDto promoCode = PromoCodes.FirstOrDefault(x => x.PromoCodeId == orderProduct.OrderPromoCodeId);

                    if (promoCode is not null && promoCode.PromoCodeTypeId == PromoCodeType.Bundle.Id)
                    {
                        if (MessageFacadeService.Confirm("Вместе с товаром будут удалены все товары из бандла. Вы уверены?"))
                        {
                            OrderProductViewModel[] promoCodeProducts = OrderProducts
                                .Where(x => x.OrderPromoCodeId == orderProduct.OrderPromoCodeId)
                                .ToArray();

                            foreach (OrderProductViewModel promoCodeProduct in promoCodeProducts)
                            {
                                orderProductsToRemove.Add(promoCodeProduct);
                            }
                        }
                        else
                        {
                            return;
                        }

                        PromoCodes.Remove(promoCode);
                    }
                }

                OrderProducts.RemoveRange(orderProductsToRemove);
            }
        }

        private void ShowCredit()
        {
            if (OrderProducts.Any(x => x.PriceOut <= 0 && x.TypeId != ProductType.GuestProductId))
            {
                MessageFacadeService.ShowNotificationError("В заказе присутствуют товары с нулевой ценой");
                return;
            }

            OrderCreditViewModel viewModel = DialogDocumentManagerService.ShowView<OrderCreditViewModel>(
                new OrderCreditParameter(
                    order.Id,
                    SelectedContractor.PriceTypeId),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
        }

        private async Task CopyCreditDataAsync()
        {
            if (SelectedCity == null)
            {
                MessageFacadeService.ShowNotificationWarning("Город не заполнен");
                return;
            }

            if (SelectedCity.AreaId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Город не привязан к области");
                return;
            }

            await ErrorHandler.HandleErrorsAsync(
                async _ =>
                {
                    Task<AreaDto> areaTask = WebClient.ExecuteApiRequestAsync(new QueryArea(SelectedCity.AreaId));
                    Task<CreditOfferDto> creditOfferTask = WebClient.ExecuteApiRequestAsync(new QueryCreditOffer(SelectedExternalPayment!.Params!.CreditOfferId!.Value));
                    Task<string> templateTask = WebClient.ExecuteApiRequestAsync(new QueryCreditEmailTemplate());

                    return await TaskExt.WhenAll(areaTask, creditOfferTask, templateTask);
                },
                "загрузке данных",
                "Данные загружены",
                this,
                true,
                onSuccess: ((AreaDto area, CreditOfferDto creditOffer, string template) result, CancellationToken ct) =>
                {
                    var data = new
                    {
                        OrderId = order.Id,
                        order.Phone,
                        order.Phone2,
                        order.Email,
                        order.Fio,
                        Area = result.area.Name,
                        result.creditOffer.Month,
                        DeliveryCost = OrderPaymentViewModel.DeliveryItem.Uah,
                        Amount = OrderPaymentViewModel.SummaryItem.Uah + OrderPaymentViewModel.DeliveryItem.Uah,
                        Products = GetOrderProducts()
                    };

                    string formattedText = Smart.Format(result.template, data);

                    Clipboard.SetText(formattedText);

                    return Task.CompletedTask;
                });
        }

        private async Task<OrderCanEditAssemblyResponse> CanEditAssemblyAsync(int orderFolderId)
        {
            if (Id <= 0)
            {
                return null;
            }

            Result<OrderCanEditAssemblyResponse> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CanEditOrderAssembly(Id, orderFolderId)),
                "редактировании сборки",
                null,
                this,
                false,
                true);

            if (result?.IsSuccess == true)
            {
                return result.Data;
            }

            return null;
        }

        private async Task<OrderCanEditAdditionalServiceResponse> CanEditAdditionalServiceAsync(int orderProductId)
        {
            if (Id <= 0 || orderProductId <= 0)
            {
                return new OrderCanEditAdditionalServiceResponse();
            }

            Result<OrderCanEditAdditionalServiceResponse> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CanEditOrderAdditionalService(Id, orderProductId)),
                "редактировании услуги",
                null,
                this,
                false,
                true);

            if (result?.IsSuccess == true)
            {
                return result.Data;
            }

            return null;
        }

        private bool CanSplitOrderProduct(OrderProductViewModel orderProduct)
        {
            return orderProduct != null
                && !orderProduct.IsGift
                && State.CanChangeSources
                && !IsLockedFromAnyUser
                && !orderProduct.IsAdditionalService
                && SelectedProducts.Count() <= 1
                && orderProduct.OrderFolder?.TypeId != OrderFolderType.BundleId;
        }

        private Task SplitOrderProductAsync(OrderProductViewModel orderProduct)
        {
            if (orderProduct.Quantity < 2)
            {
                MessageFacadeService.ShowNotificationWarning("Товар не может быть разделен");
                return Task.CompletedTask;
            }

            return LockableOperationProcessor.DoActionAsync(Id, SplitOrderProductInternal, false);

            void SplitOrderProductInternal(OrderDto lockedOrder)
            {
                OrderSplitProductParameter parameter = new OrderSplitProductParameter(Id, orderProduct.Id, orderProduct.OrderFolder, orderProduct.IsVirtualProduct, orderProduct.Quantity);

                OrderSplitProductViewModel vm = DialogDocumentManagerService.ShowView<OrderSplitProductViewModel>(parameter, this);

                if (vm.IsOk)
                {
                    SetProducts(vm.Result.Data.Products, vm.Result.Data.Folders);
                }
            }
        }

        private async Task FetchDataAsync()
        {
            await Task.WhenAll(
                FetchEmployeesAsync(),
                FetchContractorsAsync(),
                FetchCitiesAsync(),
                FetchWarehousesAsync(),
                FetchWarehouseDeliveriesAsync(),
                FetchWarehousePerformancesAsync(),
                FetchOrganizations(),
                FetchCashboxesAsync(),
                FetchChangeReasonsAsync(),
                RefreshAssemblyServicesAsync(),
                RefreshAdditionalServiceProductsAsync(),
                RefreshPromosAsync(),
                FetchLegalEntitiesAsync(),
                FetchOrderAsync(),
                RefreshDocumentsAsync());

            Cities = Cities.Where(x => x.Active || order.CityId == x.Id).ToReadOnlyObservableCollection();
            Warehouses = Warehouses.Where(x => x.Active || order.WarehouseId == x.Id).ToReadOnlyObservableCollection();

            Payments = Dictionaries.GetItems<Payment>()
                .Where(x => (x.Active && !x.OnlyCreateOnWeb && x.Id != Payment.TerminalId) || order.PaymentId == x.Id)
                .Select(x => Mapper.Map(x, new OrderPaymentViewItem())).ToReadOnlyObservableCollection();

            CarryTypes = Dictionaries.GetItems<CarryType>()
                .Where(x => x.IsActive() || x.Id == order.CarryId)
                .OrderBy(x => x.Position)
                .Select(x => Mapper.Map(x, new OrderCarryTypeViewItem()))
                .ToObservableCollection();

            Subdivisions = Dictionaries.GetItems<Subdivision>().ToReadOnlyObservableCollection();

            Warranties = Dictionaries.GetItems<Warranty>().ToReadOnlyObservableCollection();

            BonusTypes = Dictionaries.GetItems<BonusType>().ToReadOnlyObservableCollection();
            PromoCodeTypes = Dictionaries.GetItems<PromoCodeType>().ToReadOnlyObservableCollection();

            ServiceRequestsStates = Dictionaries.GetItems<ServiceRequestState>().OrderBy(x => x.Position)
                .ToReadOnlyObservableCollection();

            PaymentStates = Dictionaries.GetItems<PaymentState>()
                .ToReadOnlyObservableCollection();

            ProductPriceKinds = Dictionaries.GetItems<ProductPriceKind>().Where(x => x.Real)
                .ToReadOnlyObservableCollection();

            AssemblyServiceStates = Dictionaries.GetItems<AssemblyServiceState>().ToReadOnlyObservableCollection();

            AdditionalServiceProductStates = Dictionaries.GetItems<AdditionalServiceProductState>()
                .ToReadOnlyObservableCollection();

            OrderBillStates = Dictionaries.GetItems<OrderBillState>().ToReadOnlyObservableCollection();

            OrderSources = Dictionaries.GetItems<OrderSourceType>()
                .Where(x => x.AvailOnClient || x.Id == order.OrderSourceId).OrderBy(x => x.Position)
                .Select(x => Mapper.Map(x, new OrderSourceTypeViewItem()))
                .ToReadOnlyObservableCollection();

            string checkReceiveGuestProductAct = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryCheckReceiveGuestProductAct()),
                null,
                null,
                this,
                false,
                false);

            short.TryParse(checkReceiveGuestProductAct, out _isCheckReceiveAct);

            async Task FetchChangeReasonsAsync()
            {
                List<OrderStateChangeReasonDto> reasons = await WebClient.ExecuteApiRequestAsync(new QueryOrderStateChangeReasons(true));
                ChangeReasons = reasons.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            }

            async Task FetchWarehousesAsync()
            {
                List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

                Warehouses = warehouses
                    .OrderByDescending(x => x.Position)
                    .Select(x => Mapper.Map<OrderWarehouseViewItem>(x))
                    .ToReadOnlyObservableCollection();

                AssemblyWarehouses = warehouses
                    .Where(x => x.WarehousePerformances.Where(z => z.Activity).Select(y => y.WorkId).Contains(WarehouseWorkTypeIds.AssemblyServiceWorkTypeId))
                    .OrderByDescending(x => x.Position)
                    .ToReadOnlyObservableCollection();

                AdditionalServiceWarehouses = warehouses
                  .Where(x => x.Active == 1 && (x.TypeId == WarehouseKind.Main.Id || x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.Assembly.Id || x.TypeId == WarehouseKind.Service.Id))
                  .OrderBy(x => x.Name)
                  .Select(x => Mapper.Map<OrderWarehouseViewItem>(x))
                  .ToReadOnlyObservableCollection();
            }

            async Task FetchCitiesAsync()
            {
                List<CityDto> citiesList = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

                Cities = citiesList
                    .OrderBy(x => x.Position)
                    .ThenBy(x => x.Name)
                    .Select(x => Mapper.Map<OrderCityViewItem>(x))
                    .ToReadOnlyObservableCollection();

                Cities.ForEach(x => x.Valid = true);
            }

            async Task FetchContractorsAsync()
            {
                contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

                Contractors = contractors
                    .Where(x => x.IsFolder == false && x.IsClient && x.Active)
                    .OrderBy(x => x.SubdivisionId)
                    .ThenBy(x => x.Name)
                    .Select(x => Mapper.Map<OrderContractorViewItem>(x))
                    .ToObservableCollection();
            }

            async Task FetchEmployeesAsync()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
                Employees = employees.ToReadOnlyObservableCollection();
                employeeNames = Employees.ToDictionary(x => x.Id, x => x.Name);
            }

            async Task FetchWarehouseDeliveriesAsync()
            {
                warehouseDeliveries = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseDeliveries(), true);
            }

            async Task FetchWarehousePerformancesAsync()
            {
                warehousePerformances = await WebClient.ExecuteApiRequestAsync(new QueryAllPerformances(), true);
            }

            async Task FetchOrganizations()
            {
                List<OrganizationDto> organizations = await WebClient.ExecuteApiRequestAsync(new QueryOrganizations(), true).GetPagedResultDataAsync();
                Organizations = organizations.ToReadOnlyObservableCollection();
            }

            async Task FetchCashboxesAsync()
            {
                List<CashboxDto> cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);
                Cashboxes = cashboxes.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            }

            async Task FetchLegalEntitiesAsync()
            {
                List<LegalEntityDto> legalEntities = await WebClient.ExecuteApiRequestAsync(new QueryLegalEntities(), true);

                LegalEntities = legalEntities.OrderBy(x => x.Name).Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            }

            async Task FetchOrderAsync()
            {
                order ??= await WebClient.ExecuteApiRequestAsync(new QueryOrder(Id));
            }
        }

        private GetTextFromUserViewModel GetInfoDialog(string title, string contentCaption)
        {
            return DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                new GetTextFromUserParameter(contentCaption, title),
                this);
        }

        private void LoadValues(OrderProductViewModel orderProduct)
        {
            ProductInformation.ClearProduct();

            if (orderProduct?.Product != null && !orderProduct.IsVirtualProduct)
            {
                ProductInformation.ProductId = new ProductInfoId(orderProduct.Product.Id, orderProduct.CurrencyOutId, SelectedContractor?.Id, new TradeInProductInfoDto(order?.Id, order?.StateId));
            }
        }

        private void MapOrder()
        {
            Loaded = false;

            CreatedOn = order.CreatedOn;

            Id = order.Id;
            LastName = order.LastName;
            FirstName = order.FirstName;
            MiddleName = order.MiddleName;
            Phone = order.Phone;
            Phone2 = order.Phone2;
            Email = order.Email;
            Address = order.Address;
            DeliveryData = order.DeliveryData;
            JoinedComment = OrderCommentHelper.GetJoinedComment(order.CustomerComment, order.EmployeeComment, order.SystemComment);
            EmployeeComment = order.EmployeeComment;
            CustomerComment = order.CustomerComment;
            SystemComment = order.SystemComment;
            DeliveryTime = order.DeliveryTime;
            DeliveryTimeTo = order.DeliveryTimeTo;
            ReceiveTime = order.ReceiveTime;
            PackageDeliveryPaid = Convert.ToBoolean(order.PackageDeliveryPaid);
            PackageDeliveryCost = (PackageDeliveryCost == null && order.PackageDeliveryCost == 0) ? null : order.PackageDeliveryCost;
            PackageTtn = order.PackageTtn;
            PackageWeight = order.PackageWeight;
            LockerId = order.EmployeeLockId;
            State = Dictionaries.GetItemById<OrderStatus>(order.StateId);
            Rt = order.Rt;
            Pko = order.Pko;
            DontCall = order.Options?.DontCall ?? false;
            OrganizationRecipient = order.Options?.OrganizationRecipient ?? false;
            CreatedBy = order.CreatedBy;
            ConfirmedBy = order.ConfirmedBy;
            EmployeePackId = order.EmployeePackId;
            CallsCount = order.CallsCount;
            ServiceRequestsCount = order.ServiceRequestsCount;
            ExternalPaymentsCount = order.ExternalPayments?.Count ?? 0;
            NewCallsCount = order.NewCallsCount;
            ComplaintsCount = order.ComplaintsCount;
            NewComplaintsCount = order.NewComplaintsCount;
            SeparateWarrantyCards = order.Options?.SeparateWarrantyCards ?? false;
            FreeDelivery = order.Options?.FreeDelivery ?? false;
            BasedOnServiceRequestId = order.BasedOnServiceRequestId;
            CompletedBy = order.CompletedBy;
            CompletedOn = order.CompletedOn;
            BufferWarehouseId = order.BufferWarehouseId;
            SelectLegalEntityId = order.LegalEntity?.Id;

            OrderPayments = order.OrderPayments
                .OrderBy(x => x.CreatedOn)
                .Select(x => Mapper.Map<OrderPaymentRecordViewItem>(x))
                .ToObservableCollection();

            OrderProducts = new ObservableRangeCollection<OrderProductViewModel>();

            SetProducts(order.Products, order.Folders);

            SetBonuses(order.Bonuses);

            ExternalPayments = order.ExternalPayments?
                .Select(x => Mapper.Map<ExternalPaymentViewItem>(x))
                .ToObservableCollection() ?? new ObservableCollection<ExternalPaymentViewItem>();

            PromoCodes = order.PromoCodes.ToObservableCollection();

            Loaded = true;
        }

        private OrderProductViewModel MapOrderProduct(OrderProductDto source, OrderProductViewModel target, OrderFolderDto orderFolder)
        {
            target.Id = source.Id;
            target.OrderPromoCodeId = source.OrderPromoCodeId;
            target.Product = new ProductSimpleDto
            {
                Id = source.Product.Id,
                Active = source.Product.Active,
                Name = source.Product.Name,
                NameFullRu = source.Product.NameFullRu,
                NameFullUkr = source.Product.NameFullUkr,
                Weight = source.Product.Weight,
                TypeId = source.Product.TypeId,
                Prices = source.Product.Prices,
                BonusesToCharge = source.Product.BonusesToCharge,
                ParentCategoryId = source.Product.ParentCategoryId
            };

            target.OrderFolderId = source.OrderFolderId;
            target.OrderFolder = orderFolder;

            if (source.Promo != null)
            {
                target.Promo = new CatalogPromoSimpleDto
                {
                    Id = source.Promo.Id,
                    DateStart = source.Promo.DateStart,
                    DateEnd = source.Promo.DateEnd,
                    Title = source.Promo.Title
                };
            }

            target.Quantity = source.Quantity;
            target.ParentRecordId = source.ParentRecordId;
            target.PriceOut = source.PriceOut;
            target.CurrencyOutId = source.CurrencyOutId;
            target.Price = source.Price;
            target.PriceId = source.PriceId;
            target.PriceIdOld = source.PriceId;
            target.AdditionalServicePercent = source.Product.AdditionalServicePercent;
            target.AdditionalServiceMinPrice = source.Product.AdditionalServiceMinPrice;
            target.InvoiceId = source.InvoiceId;
            target.CurrencyId = source.CurrencyId;
            target.Price1C = source.Price1C;
            target.WarrantyId = source.WarrantyId;
            target.AssemblyId = source.AssemblyId;
            target.AssemblyQuantity = source.AssemblyQuantity;
            target.AssemblyIncluded = source.AssemblyIncluded;
            target.Sn = GetSn(source.SerialNumbers?.Select(x => x.SerialNumber).ToReadOnlyCollection());
            target.State = Dictionaries.GetItemById<OrderProductStatus>(source.StateId);
            target.Source = Dictionaries.GetOrderProductSource(
                source.SourceId,
                source.WarehouseId,
                source.SourceText,
                source.SourceDate);
            target.OrderWarehouseId = SelectedWarehouse?.Id;
            target.BonusTypeId = source.BonusTypeId;
            target.TypeId = source.Product.TypeId;
            target.MaxBonusesToUse = source.MaxBonusesToUse;
            target.BonusesToCharge = source.BonusesToCharge;
            target.BonusesCharged = source.BonusesCharged;
            target.AdditionalWarranty = source.AdditionalWarranty;
            target.IsGift = source.IsGift;
            target.IsAdditionalService = source.IsAdditionalService;
            target.IsAdditionalServiceConsumable = source.IsAdditionalServiceConsumable;
            target.ShowAccessoryAdditionalServiceIcon = source.IsAdditionalService && ProductType.IsAccessoryAdditionalServiceProductType(source.Product.TypeId);
            target.ShowAdditionalServiceIcon = source.IsAdditionalService && ProductType.IsAdditionalServiceProductType(source.Product.TypeId);
            target.AdditionalServiceId = source.Product.AdditionalServiceId;
            target.PromoDiscount = source.PromoDiscount;
            target.FreeDelivery = source.FreeDelivery;
            target.UnavailableOrderProductPrice = source.UnavailableOrderProductPrice;
            target.CreatedBy = source.CreatedBy;
            target.CreatedOn = source.CreatedOn;

            if (source.UnavailableProduct != null)
            {
                target.UnavailableProduct = new ProductSimpleDto
                {
                    Id = source.UnavailableProduct.Id,
                    Active = source.UnavailableProduct.Active,
                    Name = source.UnavailableProduct.Name,
                    NameFullRu = string.IsNullOrEmpty(source.UnavailableProduct.NameFullUkr) ? source.UnavailableProduct.NameFullRu : source.UnavailableProduct.NameFullUkr,
                    Weight = source.UnavailableProduct.Weight,
                    TypeId = source.UnavailableProduct.TypeId,
                    Prices = source.UnavailableProduct.Prices,
                    BonusesToCharge = source.UnavailableProduct.BonusesToCharge,
                    ParentLinkRewrite = source.Product.ParentLinkRewrite,
                    ParentCategoryId = source.Product.ParentCategoryId
                };
            }

            return target;

            static string GetSn(IReadOnlyCollection<string> serialNumbers)
            {
                string serialsCount = "-";

                if (serialNumbers != null && serialNumbers.Count > 0)
                {
                    serialsCount = serialNumbers.Count.ToString();
                }

                return serialsCount;
            }
        }

        private async Task PostProcessAsync()
        {
            const string WhiteHex = "ffffff";
            const string RedHex = "E07F8A";
            const string GreenHex = "57d25f";

            string phoneHexBackground = WhiteHex;

            try
            {
                int[] productIds = OrderProducts.Select(x => x.ProductId).ToArray();

                Task<PagedResult<AdditionalServiceDto>> additionalServicesTask = WebClient.ExecuteApiRequestAsync(new QueryAdditionalServices(new AdditionalServicesFilteringItem(productIds, true)), true);
                Task<PagedResult<PromoDto>> promosTask = WebClient.ExecuteApiRequestAsync(new QueryPromos(new PromoFilteringItem(order.SubdivisionId, productIds, true)), true);
                Task<List<HashtagDto>> hashtagsTask = WebClient.ExecuteApiRequestAsync(new QueryHashtagsByPhone(order.Phone));

                await Task.WhenAll(additionalServicesTask, promosTask, hashtagsTask);

                additionalServices = additionalServicesTask.Result.Data.ToDictionary(x => x.Id);

                if (hashtagsTask.Result.Any(x => x.TypeId == HashtagType.MinusId))
                {
                    phoneHexBackground = RedHex;
                }
                else if (hashtagsTask.Result.Any(x => x.TypeId == HashtagType.PlusId))
                {
                    phoneHexBackground = GreenHex;
                }

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    PhoneHexBackground = phoneHexBackground;

                    foreach (OrderProductViewModel orderProduct in OrderProducts)
                    {
                        orderProduct.AnyAdditionalServices = additionalServicesTask.Result.Data?.Any(x => x.AppliedToProductIds.Contains(orderProduct.ProductId)) == true;

                        orderProduct.AnyAdditionalServiceProvideProducts = orderProduct.ParentRecordId.HasValue
                                                                            && orderProduct.IsAdditionalService
                                                                            && additionalServicesTask.Result.Data?.Any(x => x.Id == orderProduct.AdditionalServiceId && x.RequireProductsToProvide) == true;

                        orderProduct.Promo ??= promosTask.Result.Data.FirstOrDefault(x => x.ProductIds.Contains(orderProduct.ProductId));
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to post process");
                MessageFacadeService.ShowNotificationError("Непредвиденная ошибка");
            }

            await SetInvoicePriceAsync();
        }

        private async Task SetInvoicePriceAsync()
        {
            OrderProductViewModel[] orderProductsWithInvoices = OrderProducts
                .Where(x => x.InvoiceId.HasValue)
                .ToArray();

            if (!orderProductsWithInvoices.Any())
            {
                return;
            }

            IFilteringItem filteringItem = new InvoiceFilteringItem
            {
                InvoicesIds = string.Join(",", orderProductsWithInvoices.Select(x => x.InvoiceId))
            };

            List<InvoiceDto> invoices = await WebClient.ExecuteApiRequestAsync(new QueryInvoices(filteringItem)).GetPagedResultDataAsync();

            InvoiceProductDto[] invoiceProducts = invoices.SelectMany(x => x.InvoiceProducts).ToArray();

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (OrderProductViewModel orderProduct in orderProductsWithInvoices)
                {
                    orderProduct.InvoicePrice = invoiceProducts.FirstOrDefault(x => x.InvoiceId == orderProduct.InvoiceId && x.ProductId == orderProduct.ProductId)?.Price;
                }
            });
        }

        private void PostProcessOrderProducts(IReadOnlyCollection<OrderProductViewModel> orderProducts, PostProcessProductsMode postProcessProductsMode, bool setParent = false)
        {
            List<OrderProductViewModel> postProcessOrderProducts = new List<OrderProductViewModel>();

            if (setParent)
            {
                foreach (OrderProductViewModel orderProduct in orderProducts)
                {
                    orderProduct.SetParentViewModel(this);
                    orderProduct.ValidationStarted += OrderProductValidationStarted;
                }
            }

            Random random = new Random();

            Dictionary<int, OrderProductViewModel> assemblyProductFolders = orderProducts
                .Where(x => x.IsVirtualProduct && x.OrderFolderId is not null && x.OrderFolder.TypeId != OrderFolderType.BundleId)
                .ToDictionary(x => x.OrderFolderId!.Value, x => x);

            orderProducts
                .Where(x => x.IsAssemblyOrAssembledComputerRule() && x.ParentId == null && !assemblyProductFolders.Keys.Contains(x.OrderFolderId!.Value))
                .GroupBy(x => x.OrderFolderId.Value)
                .ForEach(x =>
                {
                    OrderProductViewModel orderProduct = x.First();

                    List<int> orderProductIds = orderProducts
                        .Where(z => z.OrderFolderId == orderProduct.OrderFolderId)
                        .Select(q => q.Id)
                        .ToList();

                    OrderProductViewModel assemblyOrderProduct = new OrderProductViewModel(Dictionaries, MessageFacadeService, OrderRules)
                    {
                        Id = random.GetRandomId(),
                        CurrencyId = orderProduct.CurrencyOutId,
                        CurrencyOutId = orderProduct.CurrencyOutId,
                        OrderFolderId = orderProduct.OrderFolderId,
                        OrderFolder = orderProduct.OrderFolder,
                        AssemblyId = orderProduct.AssemblyId,
                        State = OrderProductStatus.None,
                        Source = new NoneOrderProductSource(),
                        IsVirtualProduct = true,
                        AssembliesInAssemblyModule = GetQuantityAssembliesInAssemblyModule(orderProductIds, orderProduct.OrderFolderId),
                        Product = new ProductSimpleDto { NameFullRu = orderProduct.OrderFolder.Name },
                        FreeDelivery = orderProduct.OrderFolder.FreeDelivery
                    };

                    assemblyOrderProduct.ValidationStarted += OrderProductValidationStarted;

                    assemblyProductFolders[x.Key] = assemblyOrderProduct;
                });

            Dictionary<int, OrderProductViewModel> bundleProductFolders = orderProducts
                .Where(x => x.OrderFolderId is not null && x.OrderFolder?.TypeId == OrderFolderType.Bundle.Id)
                .GroupBy(x => x.OrderFolderId!.Value)
                .ToDictionary(x => x.Key, x => x.First());

            bundleProductFolders.ForEach(x =>
            {
                OrderProductViewModel orderProduct = x.Value;

                List<int> orderProductIds = orderProducts
                    .Where(z => z.OrderFolderId == orderProduct.OrderFolderId)
                    .Select(q => q.Id)
                    .ToList();

                OrderProductViewModel bundleFolderOrderProduct = new OrderProductViewModel(Dictionaries, MessageFacadeService, OrderRules)
                {
                    Id = random.GetRandomId(),
                    CurrencyId = orderProduct.CurrencyOutId,
                    CurrencyOutId = orderProduct.CurrencyOutId,
                    OrderFolderId = orderProduct.OrderFolderId,
                    OrderFolder = orderProduct.OrderFolder,
                    State = OrderProductStatus.None,
                    Source = new NoneOrderProductSource(),
                    IsVirtualProduct = true,
                    Product = new ProductSimpleDto { NameFullRu = orderProduct.OrderFolder.Name },
                    FreeDelivery = orderProduct.OrderFolder.FreeDelivery
                };

                bundleFolderOrderProduct.ValidationStarted += OrderProductValidationStarted;

                bundleProductFolders[x.Key] = bundleFolderOrderProduct;
            });

            orderProducts.Where(x => x.IsAdditionalService)
                .ForEach(x =>
                {
                    x.CompletedAdditionalServiceProductsQuantity = GetCompletedAdditionalServiceProducts(x.Id);
                });

            foreach (KeyValuePair<int, OrderProductViewModel> assemblyParentProduct in assemblyProductFolders.OrderBy(x => x.Value.OrderFolderId))
            {
                postProcessOrderProducts.Add(assemblyParentProduct.Value);
            }

            foreach (KeyValuePair<int, OrderProductViewModel> bundleParentProduct in bundleProductFolders.OrderBy(x => x.Value.OrderFolderId))
            {
                postProcessOrderProducts.Add(bundleParentProduct.Value);
            }

            foreach (OrderProductViewModel orderProduct in orderProducts.Where(x => !x.IsVirtualProduct))
            {
                if (orderProduct.OrderFolderId.HasValue && assemblyProductFolders.TryGetValue(orderProduct.OrderFolderId.Value, out OrderProductViewModel assemblyGroupParentProduct))
                {
                    orderProduct.ParentId = assemblyGroupParentProduct.Id;
                }
                else if (orderProduct.OrderFolderId.HasValue && bundleProductFolders.TryGetValue(orderProduct.OrderFolderId.Value, out OrderProductViewModel bundleGroupParentProduct))
                {
                    orderProduct.ParentId = bundleGroupParentProduct.Id;
                }

                postProcessOrderProducts.Add(orderProduct);
            }

            var groupParameters = orderProducts
                .Where(x => !x.IsVirtualProduct && x.OrderFolderId.HasValue)
                .GroupBy(x => x.OrderFolderId.Value)
                .ToDictionary(x => x.Key, x => new
                {
                    Quantity = x.GroupBy(y => y.Product.Id)
                        .Select(y => y.Sum(z => z.Quantity) / y.First().AssemblyQuantity!.Value)
                        .First(),
                    PriceOut = x.GroupBy(y => y.Product.Id)
                        .Sum(y => y.Select(z => z.PriceOut * z.AssemblyQuantity!.Value).First())
                });

            assemblyProductFolders.ForEach(x =>
            {
                if (groupParameters.TryGetValue(x.Key, out var parameters))
                {
                    x.Value.Quantity = parameters.Quantity;
                    x.Value.PriceOut = parameters.PriceOut;

                    x.Value.SetParentViewModel(this);
                }
            });

            bundleProductFolders.ForEach(x =>
            {
                if (groupParameters.TryGetValue(x.Key, out var parameters))
                {
                    x.Value.Quantity = parameters.Quantity;
                    x.Value.PriceOut = parameters.PriceOut;

                    x.Value.SetParentViewModel(this);
                }
            });

            switch (postProcessProductsMode)
            {
                case PostProcessProductsMode.AddRange:
                    OrderProducts.AddRange(postProcessOrderProducts);
                    break;
                case PostProcessProductsMode.ReplaceRange:
                    OrderProducts.ReplaceRange(postProcessOrderProducts);
                    break;
            }

            RaisePropertyChanged(nameof(GuestProductColumnIsVisible));

            PostProcessAsync();
        }

        private void EditServiceRequest(ServiceRequestViewItem viewItem)
        {
            Messenger.Send(new ServiceRequestViewMessage(viewItem.Id));
        }

        private async Task SaveAsync()
        {
            bool processed = await SaveInternalAsync();

            if (processed)
            {
                Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
            }
        }

        private async Task OkAsync()
        {
            bool processed = await SaveInternalAsync();

            if (processed)
            {
                Close();
            }
        }

        private async Task<bool> SaveInternalAsync()
        {
            ShowLoadingIndicator = true;

            bool processed = false;

            try
            {
                Result<OrderDto> result;

                ValidationResultItem[] validationItems = await CanSaveAsync(this).ToArrayAsync();

                if (validationItems.Any())
                {
                    bool isOk = ShowValidationResultView("Нарушены правила валидации", validationItems);

                    if (!isOk)
                    {
                        return false;
                    }
                }

                if (IsAddMode)
                {
                    result = await WebClient.ExecuteApiRequestAsync(new CreateOrder(GetOrderCreateDto()));
                    Messenger.Send(new OrderMessage(result.Data, MessageType.Added));
                }
                else
                {
                    result = await WebClient.ExecuteApiRequestAsync(new UpdateOrder(order.Id, GetOrderSaveDto()));
                    Messenger.Send(new OrderMessage(result.Data, MessageType.Changed));
                }

                order = result.Data;

                string successfullySavedMessage = $"Заказ №{order.Id.ToString(CultureInfo.CurrentUICulture)} успешно сохранен";
                string savedWithWarningsMessage = $"Заказ №{order.Id.ToString(CultureInfo.CurrentUICulture)} сохранен с предупреждениями";

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning(savedWithWarningsMessage);

                    bool isOk = ShowValidationResultView(
                        "Предупреждения при сохранении заказа",
                        result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());

                    processed = IsAddMode || isOk;
                }
                else
                {
                    if (validationItems.Any())
                    {
                        MessageFacadeService.ShowNotificationWarning(savedWithWarningsMessage);
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationInfo(successfullySavedMessage);
                    }

                    processed = true;
                }

                if (State == OrderStatus.Received && !IsAddMode && result.Data != null && AutoFillSources)
                {
                    await FillSourcesInternalAsync(result.Data, false);
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(order.Id));
                ShowValidationResultView("Ошибки при сохранении заказа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(order.Id));
                ShowValidationResultView("Ошибки при сохранении заказа", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save order");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении заказа");
            }
            finally
            {
                ShowLoadingIndicator = false;
            }

            return processed;
        }

        private OrderSaveDto GetOrderSaveDto()
        {
            return MapToSaveDto(this, new OrderSaveDto());
        }

        private OrderCreateDto GetOrderCreateDto()
        {
            return MapToCreateDto(this, new OrderCreateDto());
        }

        private string GetErrorMessage(int orderId)
        {
            string message = "Ошибка при сохранении заказа";

            if (orderId > 0)
            {
                message = string.Concat(message, $" №{order.Id.ToString(CultureInfo.CurrentUICulture)}");
            }

            return message;
        }

        private void OrderProductsCollectionChangedEvent(object sender, NotifyCollectionChangedEventArgs e)
        {
            OrderProductsCollectionChanged();
        }

        private void OrderProductsCollectionChanged()
        {
            if (AnyAssemblies == false)
            {
                AssemblyWarehouse = null;
                BufferWarehouseId = null;
            }

            if (IsNotReadonlyAdditionalServiceWarehouse == false)
            {
                SelectedAdditionalServiceWarehouse = null;
            }

            if (AnyAssemblies && AssemblyWarehouse == null && IsLockedByCurrentUserAndEditingAllowed)
            {
                AssemblyWarehouse = AssemblyWarehouses.FirstOrDefault(x => x.Id == SelectedWarehouse?.AssemblyWarehouseId);
            }

            OrderProductsChanged();
            RefreshPaymentInfo();
            RefreshSummaryItems();
            OrderProducts.ForEach(x => x.RefreshAllowEdit());
            OrderProductValidationStarted();
            ResetAdditionalServiceWarehouseValidation();
            ResetCarryTypeValidation();

            RaisePropertiesChanged(
                nameof(AssemblyWarehouse),
                nameof(AnyAssemblies),
                nameof(IsNotReadonlyAssemblyWarehouse),
                nameof(IsNotReadonlyAdditionalServiceWarehouse),
                nameof(SelectedAdditionalServiceWarehouse));
        }

        private void OrderStateChangedCallback()
        {
            RaisePropertiesChanged(
                nameof(DeliveryTime),
                nameof(DeliveryTimeTo),
                nameof(SelectedContractor),
                nameof(SelectedWarehouse),
                nameof(Address),
                nameof(AssemblyWarehouse),
                nameof(NeedCalculateLogistics));

            UpdateCanAddProduct();
            RefreshSummaryItems();
        }

        private void ProcessSelectedForAddingProducts(
            IReadOnlyCollection<NomenclatureViewItem> nomenclatureItems,
            OrderFolderDto orderFolder = null,
            int assemblyQuantity = 1,
            int? additionalServicesAppliedToOrderProductId = null,
            bool isAdditionalServiceConsumable = false)
        {
            Random random = new Random();

            foreach (NomenclatureViewItem item in nomenclatureItems)
            {
                int currencyId = item.CurrencyId;
                decimal minPrice = currencyId == Currency.Uah.Id ? MinUahPrice : MinUsdPrice;
                int quantity = item.TypeId == ProductType.GuestProductId ? 1 : item.Quantity * assemblyQuantity;

                decimal price = Math.Max(orderFolder?.Price != null && additionalServicesAppliedToOrderProductId == null ? item.PriceIn ?? 0 : item.Price, minPrice);

                OrderProductViewModel orderProductViewModel = new OrderProductViewModel(Dictionaries, MessageFacadeService, OrderRules)
                {
                    Id = random.GetRandomId(),
                    OrderFolder = ReflectionObjectCloner.Clone(orderFolder),
                    OrderFolderId = orderFolder?.Id,
                    ParentRecordId = additionalServicesAppliedToOrderProductId,
                    Quantity = quantity,
                    AssemblyQuantity = IsAssemblyOrAssembledComputerRuleFolder(orderFolder) ? item.Quantity : null,
                    Price = price,
                    PriceId = orderFolder?.TypeId == OrderFolderType.AssembledComputerRuleId ? null : SelectedContractor.PriceTypeId,
                    PriceIdOld = orderFolder?.TypeId == OrderFolderType.AssembledComputerRuleId ? null : SelectedContractor.PriceTypeId,
                    CurrencyId = currencyId,
                    PriceOut = price,
                    CurrencyOutId = currencyId,
                    Price1C = 0m,
                    Product = new ProductSimpleDto
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Weight = item.Weight,
                        NameFullRu = item.NameFullRu,
                        NameFullUkr = item.NameFullUa,
                        TypeId = item.TypeId,
                        Prices = item.Prices,
                        BonusesToCharge = item.BonusesToCharge,
                        ParentCategoryId = item.ParentCategoryId
                    },
                    Source = item.TypeId == ProductType.GuestProductId ? new GuestOrderProductSource(SelectedAdditionalServiceWarehouse?.Id ?? SelectedWarehouse?.Id ?? 0, "Клиент", DateTime.Now.AddDays(3)) : new NoneOrderProductSource(),
                    State = OrderProductStatus.New,
                    OrderPromoCodeId = null,
                    WarrantyId = item.WarrantyId,
                    AdditionalWarranty = item.AdditionalService?.AdditionalWarranty ?? false,
                    TypeId = item.TypeId,
                    MaxBonusesToUse = item.MaxBonusesToUse,
                    BonusesCharged = false,
                    IsGift = false,
                    IsAdditionalService = additionalServicesAppliedToOrderProductId.HasValue && !isAdditionalServiceConsumable,
                    IsAdditionalServiceConsumable = isAdditionalServiceConsumable,
                    ShowAccessoryAdditionalServiceIcon = additionalServicesAppliedToOrderProductId.HasValue && ProductType.IsAccessoryAdditionalServiceProductType(item.TypeId),
                    ShowAdditionalServiceIcon = additionalServicesAppliedToOrderProductId.HasValue && ProductType.IsAdditionalServiceProductType(item.TypeId),
                    AdditionalServiceId = item.AdditionalService?.Id,
                    AdditionalServiceMinPrice = item.AdditionalService?.MinPrice,
                    AdditionalServicePercent = item.AdditionalService?.Percent,
                    AssemblyIncluded = item.AssemblyIncluded || (IsAssemblyOrAssembledComputerRuleFolder(orderFolder)
                                                                 && (item.AdditionalService?.AssemblyPart ?? orderFolder.TypeId == OrderFolderType.AssembledComputerRuleId)
                                                                 && !isAdditionalServiceConsumable),
                    FreeDelivery = item.FreeDelivery
                };
                orderProductViewModel.Price = orderProductViewModel.PriceOut;
                orderProductViewModel.CurrencyId = orderProductViewModel.CurrencyOutId;
                orderProductViewModel.ValidationStarted += OrderProductValidationStarted;
                orderProductViewModel.SetParentViewModel(this);

                if (CanChargeBonuses())
                {
                    orderProductViewModel.BonusTypeId = item.BonusTypeId;
                    orderProductViewModel.BonusesToCharge = item.BonusesToCharge;
                }

                int position;

                if (additionalServicesAppliedToOrderProductId.HasValue)
                {
                    OrderProductViewModel parentOrderProduct = OrderProducts.First(x => x.Id == orderProductViewModel.ParentRecordId);

                    orderProductViewModel.OrderFolderId = parentOrderProduct.OrderFolderId;
                    orderProductViewModel.OrderFolder = ReflectionObjectCloner.Clone(parentOrderProduct.OrderFolder);
                    orderProductViewModel.AssemblyQuantity = parentOrderProduct.AssemblyQuantity;

                    position = OrderProducts.IndexOf(parentOrderProduct);

                    OrderProducts.Insert(++position, orderProductViewModel);
                    orderProductViewModel.Quantity = parentOrderProduct.Quantity;
                }
                else
                {
                    OrderProducts.Add(orderProductViewModel);

                    position = OrderProducts.Count - 1;
                }

                if (item.Gifts == null)
                {
                    continue;
                }

                ProductAdditionalServiceDto[] availAdditionalServices = TreeStructureHelper
                    .Deconstruct(item.AdditionalServiceGroups)
                    .SelectMany(x => x.AdditionalServices)
                    .ToArray();

                ProductAdditionalServiceDto[] autoAddAdditionalServices = availAdditionalServices
                    .Where(x => x.AutoAdd)
                    .ToArray();

                foreach (ProductAdditionalServiceDto additionalService in autoAddAdditionalServices)
                {
                    ProductDto additionalServiceProduct = additionalService.Product;

                    if (additionalServiceProduct == null)
                    {
                        continue;
                    }

                    currencyId = additionalServiceProduct.CurrencyId;
                    minPrice = currencyId == Currency.Uah.Id ? MinUahPrice : MinUsdPrice;

                    price = Math.Max(additionalServiceProduct.Price, minPrice);

                    OrderProductViewModel orderProductAdditionalServiceViewModel = new OrderProductViewModel(Dictionaries, MessageFacadeService, OrderRules)
                    {
                        Id = random.GetRandomId(),
                        OrderFolderId = orderFolder?.Id,
                        OrderFolder = ReflectionObjectCloner.Clone(orderFolder),
                        ParentRecordId = orderProductViewModel.Id,
                        Quantity = item.Quantity * assemblyQuantity,
                        AssemblyQuantity = IsAssemblyOrAssembledComputerRuleFolder(orderFolder) ? item.Quantity : null,
                        Price = price,
                        PriceId = SelectedContractor.PriceTypeId,
                        PriceIdOld = SelectedContractor.PriceTypeId,
                        CurrencyId = currencyId,
                        PriceOut = price,
                        CurrencyOutId = currencyId,
                        Price1C = 0m,
                        Product = new ProductSimpleDto
                        {
                            Id = additionalServiceProduct.Id,
                            Name = additionalServiceProduct.Name,
                            Weight = additionalServiceProduct.Weight ?? additionalServiceProduct.WeightEstimated,
                            NameFullRu = additionalServiceProduct.NameFullRu,
                            NameFullUkr = additionalServiceProduct.NameFullUa,
                            TypeId = additionalServiceProduct.TypeId,
                            Prices = additionalServiceProduct.Prices,
                            BonusesToCharge = item.BonusesToCharge,
                            ParentCategoryId = additionalServiceProduct.ParentCategoryId
                        },
                        Source = new NoneOrderProductSource(),
                        State = OrderProductStatus.New,
                        OrderPromoCodeId = null,
                        WarrantyId = additionalServiceProduct.WarrantyId,
                        TypeId = additionalServiceProduct.TypeId,
                        MaxBonusesToUse = additionalServiceProduct.MaxBonusesToUse,
                        BonusesCharged = false,
                        IsGift = false,
                        IsAdditionalService = ProductType.IsAdditionalServiceProductType(additionalServiceProduct.TypeId),
                        ShowAdditionalServiceIcon = ProductType.IsAdditionalServiceProductType(additionalServiceProduct.TypeId),
                        ShowAccessoryAdditionalServiceIcon = ProductType.IsAccessoryAdditionalServiceProductType(additionalServiceProduct.TypeId),
                        AdditionalServiceId = additionalService.Id,
                        AdditionalWarranty = additionalService.AdditionalWarranty,
                        AdditionalServiceMinPrice = additionalService.MinPrice,
                        AdditionalServicePercent = additionalService.Percent,
                        AssemblyIncluded = IsAssemblyOrAssembledComputerRuleFolder(orderFolder) && additionalService.AssemblyPart,
                        InitiatedBy = Constants.SystemEmployeeId,
                        FreeDelivery = additionalServiceProduct.FreeDelivery
                    };
                    orderProductAdditionalServiceViewModel.Price = orderProductAdditionalServiceViewModel.PriceOut;
                    orderProductAdditionalServiceViewModel.CurrencyId = orderProductAdditionalServiceViewModel.CurrencyOutId;
                    orderProductAdditionalServiceViewModel.ValidationStarted += OrderProductValidationStarted;
                    orderProductAdditionalServiceViewModel.SetParentViewModel(this);

                    if (CanChargeBonuses())
                    {
                        orderProductAdditionalServiceViewModel.BonusTypeId = additionalServiceProduct.BonusTypeId;
                        orderProductAdditionalServiceViewModel.BonusesToCharge = additionalServiceProduct.BonusesToCharge;
                    }

                    orderProductAdditionalServiceViewModel.OrderFolderId = orderProductViewModel.OrderFolderId;
                    orderProductAdditionalServiceViewModel.OrderFolder = ReflectionObjectCloner.Clone(orderProductViewModel.OrderFolder);
                    orderProductAdditionalServiceViewModel.AssemblyQuantity = orderProductViewModel.AssemblyQuantity;

                    position = OrderProducts.IndexOf(orderProductViewModel);

                    OrderProducts.Insert(++position, orderProductAdditionalServiceViewModel);
                    orderProductAdditionalServiceViewModel.Quantity = orderProductViewModel.Quantity;
                }

                foreach (NomenclatureViewItem giftProduct in item.Gifts)
                {
                    ProductAdditionalServiceDto additionalService = availAdditionalServices.FirstOrDefault(x => x.ProductId == giftProduct.Id);

                    decimal giftPrice = additionalService != null
                                        && additionalService.Product.TypeId != ProductType.AccessoryId
                                        && additionalService.Product.TypeId != ProductType.CertificateId
                        ? additionalService.Price
                        : giftProduct.Price;

                    int giftCurrencyId = additionalService == null
                        ? giftProduct.CurrencyId
                        : Currency.UahId;

                    OrderProductViewModel orderGiftProductViewModel = new OrderProductViewModel(Dictionaries, MessageFacadeService, OrderRules)
                    {
                        Id = random.GetRandomId(),
                        OrderFolderId = orderFolder?.Id,
                        OrderFolder = ReflectionObjectCloner.Clone(orderFolder),
                        ParentRecordId = orderProductViewModel.Id,
                        AssemblyQuantity = IsAssemblyOrAssembledComputerRuleFolder(orderFolder) ? item.Quantity : null,
                        Quantity = item.Quantity * assemblyQuantity,
                        PriceOut = giftPrice,
                        CurrencyOutId = giftCurrencyId,
                        Price = giftPrice,
                        PriceId = SelectedContractor.PriceTypeId,
                        PriceIdOld = SelectedContractor.PriceTypeId,
                        CurrencyId = giftCurrencyId,
                        IsGift = true,
                        Price1C = 0m,
                        Product = new ProductSimpleDto
                        {
                            Id = giftProduct.Id,
                            Name = giftProduct.Name,
                            Weight = giftProduct.Weight,
                            NameFullRu = giftProduct.NameFullRu,
                            NameFullUkr = giftProduct.NameFullUa,
                            TypeId = giftProduct.TypeId,
                            BonusesToCharge = giftProduct.BonusesToCharge,
                            ParentCategoryId = giftProduct.ParentCategoryId
                        },
                        Source = new NoneOrderProductSource(),
                        State = OrderProductStatus.New,
                        OrderPromoCodeId = null,
                        WarrantyId = giftProduct.WarrantyId,
                        MaxBonusesToUse = giftProduct.MaxBonusesToUse,
                        BonusesCharged = false,
                        AdditionalServiceId = additionalService?.Id,
                        AdditionalServicePercent = additionalService?.Percent,
                        AdditionalServiceMinPrice = additionalService?.MinPrice,
                        IsAdditionalService = additionalService != null,
                        ShowAdditionalServiceIcon = additionalService != null && ProductType.IsAdditionalServiceProductType(additionalService.ProductTypeId),
                        ShowAccessoryAdditionalServiceIcon = additionalService != null && ProductType.IsAccessoryAdditionalServiceProductType(additionalService.ProductTypeId),
                        TypeId = giftProduct.TypeId,
                        FreeDelivery = giftProduct.FreeDelivery
                    };

                    OrderProducts.Insert(++position, orderGiftProductViewModel);
                }
            }

            if (orderFolder?.Price > 0)
            {
                decimal totalProductPrice = OrderProducts
                    .Where(x => x.OrderFolderId == orderFolder.Id && x.ParentRecordId == null && !x.IsVirtualProduct)
                    .Sum(x => x.PriceOut * x.Quantity);

                if (totalProductPrice != orderFolder.Price)
                {
                    OrderProductViewModel assemblyServiceProductViewModel = OrderProducts
                        .First(x => x.OrderFolderId == orderFolder.Id && x.ProductId == Constants.AssemblyServiceProductId);

                    assemblyServiceProductViewModel.PriceOut += orderFolder.Price.Value - totalProductPrice;
                }
            }

            foreach (var orderFolderGroup in OrderProducts.Select(x => x.OrderFolder).Where(x => x != null && x.TypeId != OrderFolderType.AssembledComputerRuleId).GroupBy(x => x.Id))
            {
                bool freeDelivery = OrderProducts.Where(x => x.OrderFolder?.Id == orderFolderGroup.Key)
                    .Any(x => x.FreeDelivery);

                foreach (OrderFolderDto orderFolderDto in orderFolderGroup)
                {
                    orderFolderDto.FreeDelivery = freeDelivery;
                }
            }

            PostProcessOrderProducts(OrderProducts, PostProcessProductsMode.ReplaceRange);

            if (nomenclatureItems.Any(x => x.TypeId == ProductType.GuestProductId && x.Quantity > 1))
            {
                MessageFacadeService.ShowNotificationWarning($"Гостевой товар можна внести только {Environment.NewLine}в количестве 1 шт.");
            }
        }

        private void ProcessSelectedForAddingProducts(
            IReadOnlyCollection<AssemblyViewModelResult> assemblies,
            OrderFolderDto orderFolder,
            int assemblyQuantity = 1,
            int? additionalServicesAppliedToOrderProductId = null)
        {
            ProcessSelectedForAddingProducts(assemblies.Select(x => x.NomenclatureItem).ToArray(), orderFolder, assemblyQuantity, additionalServicesAppliedToOrderProductId);

            foreach (AssemblyViewModelResult assembly in assemblies)
            {
                foreach (OrderProductViewModel orderProduct in OrderProducts.Where(x => x.ProductId == assembly.NomenclatureItem.Id && x.OrderFolderId == orderFolder?.Id))
                {
                    orderProduct.AssemblyIncluded = assembly.AssemblyIncluded;
                    orderProduct.IsGift = assembly.IsGift;
                }
            }
        }

        private Task RemoveSourceAsync(OrderProductViewModel orderProduct)
        {
            Task task = Task.CompletedTask;

            if (orderProduct.Source.Id == OrderProductSourceType.None.Id && !orderProduct.IsVirtualProduct)
            {
                MessageFacadeService.ShowNotificationWarning("Нечего удалять");
            }
            else if (MessageFacadeService.Confirm("Вы уверены?"))
            {
                task = RemovePurchaseSourceAsync(orderProduct);
            }

            return task;
        }

        private bool CanSetState(IEnumerable<OrderProductViewModel> orderProducts)
        {
            if (orderProducts is null)
            {
                return false;
            }

            return orderProducts.All(x => LockerId == null && State?.CanChangeStates == true && x != null && x.Id > 0);
        }

        private Task SetNewOrderProductsStateAsync(IEnumerable<OrderProductViewModel> orderProducts)
        {
            return SetOrderProductsStateAsync(orderProducts.ToArray(), OrderProductStatus.New);
        }

        private Task SetClarifyOrderProductsStateAsync(IEnumerable<OrderProductViewModel> orderProducts)
        {
            return SetOrderProductsStateAsync(orderProducts.ToArray(), OrderProductStatus.Clarify);
        }

        private Task SetAgreedOrderProductsStateAsync(IEnumerable<OrderProductViewModel> orderProducts)
        {
            return SetOrderProductsStateAsync(orderProducts.ToArray(), OrderProductStatus.Agreed);
        }

        private Task SetAgreedToAllOrderProductsStateAsync(IEnumerable<OrderProductViewModel> orderProducts)
        {
            return SetOrderProductsStateAsync(GetOrderProducts().ToArray(), OrderProductStatus.Agreed);
        }

        private async Task SetOrderProductsStateAsync(OrderProductViewModel[] orderProducts, OrderProductStatus orderProductStatus)
        {
            bool products = orderProducts.Length > 1;

            if (products)
            {
                if (!MessageFacadeService.Confirm("Вы уверены, что хотите согласовать все товары?"))
                {
                    return;
                }
            }
            else
            {
                if (!MessageFacadeService.Confirm("Вы уверены?"))
                {
                    return;
                }
            }

            const string errorMessage = "Ошибка при изменении статуса товара";

            IsLongOperationInProgress = true;

            List<ValidationResultItem> validationResults = new List<ValidationResultItem>();

            foreach (OrderProductViewModel orderProduct in orderProducts)
            {
                try
                {
                    Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateOrderProductState(Id, orderProduct.Id, orderProductStatus.Id));

                    order = result.Data;

                    OrderProductDto changedOrderProduct = result.Data.Products.First(x => x.Id == orderProduct.Id);

                    orderProduct.State = Dictionaries.GetItemById<OrderProductStatus>(changedOrderProduct.StateId);
                    orderProduct.Source = Dictionaries.GetOrderProductSource(
                        changedOrderProduct.SourceId,
                        changedOrderProduct.WarehouseId,
                        changedOrderProduct.SourceText,
                        changedOrderProduct.SourceDate);

                    Messenger.Send(new OrderMessage(result.Data, MessageType.Changed));

                    if (result.Warnings.Any())
                    {
                        validationResults.AddRange(products
                            ? result.Warnings.Select(x => new ValidationResultItem($"{orderProduct.Product.NameFullRu}: {x}", false))
                            : result.Warnings.Select(x => new ValidationResultItem(x, false)));
                    }
                }
                catch (UnexpectedSatusException exception)
                {
                    validationResults.AddRange(exception.GetErrorItems().Select(x => new ValidationResultItem($"{orderProduct.Product.NameFullRu}: {x.Message}", x.IsError)));
                }
                catch (UnexpectedErrorException)
                {
                    MessageFacadeService.ShowNotificationError(errorMessage);
                    ShowValidationResultView("Ошибки", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                    return;
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to change order product state");
                    MessageFacadeService.ShowNotificationError(errorMessage);
                    return;
                }
            }

            if (validationResults.Any())
            {
                MessageFacadeService.ShowNotificationError(errorMessage);
                ShowValidationResultView("Ошибки", validationResults);
            }
            else
            {
                MessageFacadeService.ShowNotificationInfo(products ? "Статус всех товаров заказа успешно изменен" : "Статус товара успешно изменен");
            }

            IsLongOperationInProgress = false;
        }

        private void SelectedCarryTypeChangedCallback()
        {
            if (IsInitMode)
            {
                return;
            }

            Debug.WriteLine($"{nameof(SelectedCarryTypeChangedCallback)} {SelectedCarryType?.Name}");

            RaisePropertiesChanged(nameof(Address), nameof(LastName), nameof(FirstName), nameof(MiddleName));

            RefreshPaymentInfo();

            ClearAddressFields();

            ResetValidation();
        }

        private void ClearAddressFields()
        {
            DeliveryData = new DeliveryDataDto();
            Address = null;

            RaisePropertyChanged(nameof(Address));
        }

        private void SelectedCityChangedCallback()
        {
            if (IsInitMode)
            {
                return;
            }

            Debug.WriteLine($"{nameof(SelectedCityChangedCallback)} {SelectedCity?.Name}");

            RefreshPaymentInfo();
            ClearAddressFields();

            ResetValidation();
        }

        private void SelectedContractorChangedCallback()
        {
            if (SelectedContractor != null)
            {
                EmployeeManagerId = SelectedContractor.EmployeeId;

                RaisePropertiesChanged(nameof(EmployeeManagerId), nameof(OrderSummaryItems));
                RefreshSummaryItems();

                PriceColumn = Dictionaries.GetItemById<ProductPriceKind>(SelectedContractor.PriceTypeId).PriceColumn;

                ContractorTemplates = null;
                ContractorContacts = null;

                if (SelectedSubdivision?.Id != SelectedContractor.Subdivision.Id)
                {
                    SelectedSubdivision = Subdivisions.FirstOrDefault(x => x.Id == SelectedContractor.Subdivision.Id);
                }

                Task.Factory.StartNew(async () =>
                {
                    IsLongOperationInProgress = true;

                    try
                    {
                        Task<List<ContractorTemplateDto>> t1 = WebClient.ExecuteApiRequestAsync(new QueryContractorTemplates(SelectedContractor.Id)).GetPagedResultDataAsync();
                        Task<List<ContractorContactDto>> t2 = WebClient.ExecuteApiRequestAsync(new QueryContractorContacts(SelectedContractor.Id));

                        await Task.WhenAll(t1, t2);

                        List<ContractorTemplateDto> contractorTemplates = t1.Result;
                        ContractorContacts = t2.Result;

                        if (contractorTemplates.Any())
                        {
                            ContractorTemplates = contractorTemplates.ToReadOnlyObservableCollection();
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.LogError(exception, "Failed to get contractor templates");
                        MessageFacadeService.ShowNotificationError("Ошибка при получении шаблонов");
                    }
                    finally
                    {
                        IsLongOperationInProgress = false;
                    }
                });
            }

            ResetValidation();
        }

        private void SelectedPaymentChangedCallback()
        {
            ResetValidation();

            NotifyBySms = SelectedPayment?.Id != Payment.MonobankId && SelectedPayment?.Id != Payment.PumbId && SelectedPayment?.Id != Payment.ABankId && IsAddMode;

            RaisePropertiesChanged(nameof(OrderBillsVisible), nameof(Tab3Header), nameof(OrganizationRecipientVisible));

            OrderProducts?.ForEach(x => x.RaiseProperties(nameof(OrderProductViewModel.PriceOut)));

            if (IsLocked || order is null)
            {
                OrganizationRecipient = Payment.IsCachlessPayment(SelectedPayment?.Id);
            }
        }

        private void SelectedSubdivisionChangedCallback()
        {
            BufferWarehouseRequired = SelectedSubdivision?.BufferWarehouseRequired ?? true;

            UpdateCanAddProduct();

            if (!IsInitMode)
            {
                RefreshPaymentInfo();
            }

            ResetValidation();
        }

        private void SelectedWarehouseChangedCallback()
        {
            Debug.WriteLine($"{nameof(SelectedWarehouseChangedCallback)} {SelectedWarehouse?.Name}");

            foreach (OrderProductViewModel orderProduct in OrderProducts)
            {
                orderProduct.OrderWarehouseId = SelectedWarehouse?.Id;
            }

            ResetValidation();

            if (!Loaded)
            {
                if (AssemblyWarehouse != null && AnyAssemblies && IsLockedByCurrentUserAndEditingAllowed)
                {
                    AssemblyWarehouse = AssemblyWarehouses.FirstOrDefault(x => x.Id == SelectedWarehouse?.AssemblyWarehouseId);
                }
            }
            else
            {
                if (AnyAssemblies && IsLockedByCurrentUserAndEditingAllowed)
                {
                    AssemblyWarehouse = AssemblyWarehouses.FirstOrDefault(x => x.Id == SelectedWarehouse?.AssemblyWarehouseId);
                }
            }

            if (SelectedCarryType?.Id == CarryType.PickupId && SelectedWarehouse != null)
            {
                Address = SelectedWarehouse.Address;
                order.Address = SelectedWarehouse.Address;

                DeliveryData = new DeliveryDataDto
                {
                    CityId = SelectedWarehouse.CityId.ToString(),
                    PlaceId = SelectedWarehouse.Id.ToString(),
                    Street = null,
                    House = null,
                    Flat = null,
                    Extra = null,
                    MaxAllowedWeight = SelectedWarehouse.MaxPackageWeight,
                    Address = SelectedWarehouse.Address,
                    AddressUkr = SelectedWarehouse.AddressUa,
                    AddressEn = SelectedWarehouse.AddressEn
                };

                RaisePropertyChanged(nameof(Address));
            }
        }

        private void AssemblyWarehouseChangedCallBack()
        {
            BufferWarehouses = Warehouses
                .Where(x => x.Id == AssemblyWarehouse?.BufferWarehouseId)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            if (BufferWarehouseRequired || BufferWarehouseId.HasValue)
            {
                BufferWarehouseId = AssemblyWarehouse?.BufferWarehouseId;
            }
        }

        private void SelectDeliveryAddress()
        {
            if (SelectedCity == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите город");
                return;
            }

            CarryType carryType = Dictionaries.GetItemById<CarryType>(SelectedCarryType.Id);

            SelectDeliveryDataParameter parameter = new SelectDeliveryDataParameter(
                carryType.Id,
                SelectedCity.Id,
                order.Address,
                DeliveryData);

            if (carryType.Kind == CarryTypeKind.Courier)
            {
                SelectDeliveryAddressViewModel viewModel = DialogDocumentManagerService.ShowView<SelectDeliveryAddressViewModel>(
                    parameter,
                    this);

                if (viewModel.IsOk)
                {
                    DeliveryData = viewModel.GetDeliveryServiceData();
                    Address = viewModel.FullAddressString;
                    order.Address = Address;
                    RaisePropertyChanged(nameof(Address));
                }
            }
            else if (carryType.Kind == CarryTypeKind.Pickup)
            {
                SelectDeliveryWarehouseViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<SelectDeliveryWarehouseViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    DeliveryData = viewModel.GetDeliveryServiceData();
                    Address = viewModel.Place.PlaceName;
                    order.Address = Address;
                    RaisePropertyChanged(nameof(Address));
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private async Task GetDeliveryDateAsync()
        {
            ITrackNumberProvider provider = Dictionaries.GetItemById<CarryType>(SelectedCarryType.Id).GetTrackNumberProvider();

            ValidationResultItem[] validationItems = CanGetDeliveryDate(this, provider).ToArray();

            if (validationItems.Any())
            {
                MessageFacadeService.ShowMessageBoxError(string.Join(Environment.NewLine, validationItems.Select(x => x.Message)));
                return;
            }

            GetDateTimeFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetDateTimeFromUserViewModel>(
                new GetDateTimeFromUserParameter("Дата отправки", "Дата", false, DeliveryTime),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            ShowLoadingIndicator = true;

            try
            {
                DateTime deliveryDate = await provider.GetDeliveryDateAsync(SelectedCarryType.Id, SelectedWarehouse.CityId, SelectedCity.Id, viewModel.DateTime!.Value);

                MessageFacadeService.ShowNotificationInfo($"Ориентировочная дата доставки {deliveryDate:dd.MM.yy}");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get delivery date");
                MessageFacadeService.ShowNotificationError("Ошибка расчета даты доставки");
            }
            finally
            {
                ShowLoadingIndicator = false;
            }
        }

        private void SendSms(string phone)
        {
            if (SelectedSubdivision == null)
            {
                MessageFacadeService.ShowNotificationWarning("Подразделение не задано");
                return;
            }

            if (SelectedContractor == null)
            {
                MessageFacadeService.ShowNotificationWarning("Контрагент не выбран");
                return;
            }

            if (SelectedPayment == null)
            {
                MessageFacadeService.ShowNotificationWarning("Способ оплаты не выбран");
                return;
            }

            if (SelectedSubdivision == Subdivision.Retail)
            {
                MessageFacadeService.ShowNotificationWarning("У заказа недопустимое подразделение");
                return;
            }

            var orderSource = Dictionaries.GetItems<OrderSourceType>().FirstOrDefault(x => x.Id == order.OrderSourceId);

            if (orderSource?.CanContactCustomer == false)
            {
                MessageFacadeService.ShowNotificationWarning($"Запрещено связываться с клиентом по заказу с источником '{orderSource.Name}'");
                return;
            }

            decimal toPayUah = OrderPaymentViewModel.ToPayItem.Uah;

            SendSmsViewModel viewModel = DialogDocumentManagerService.ShowView<SendSmsViewModel>(
                new SendSmsParameter(
                    order.Id,
                    null,
                    SelectedContractor.PriceTypeId,
                    SelectedSubdivision,
                    phone,
                    Phone2,
                    OrderRules.GetSmsTemplates(
                        Id,
                        toPayUah,
                        Dictionaries.GetItemById<Payment>(SelectedPayment.Id),
                        SelectedSubdivision,
                        order.LegalEntity?.Id,
                        SelectedContractor.OldClient,
                        order.StateId),
                    (order.GetTotalAmount() + order.GetDeliveryCostAmount()).Uah),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (viewModel.Template?.TemplateId is SmsTemplate.PrivatPartialOrderPrepaymentId or SmsTemplate.PrivatCreditOrderPrepaymentId)
            {
                Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
            }

            if (viewModel.Template?.TemplateId is SmsTemplate.NovaPayOrderPrepaymentId
                or SmsTemplate.MonoPayOrderPrepaymentId
                or SmsTemplate.PortmoneOrderPrepaymentId
                or SmsTemplate.NovaPayOrderPrepaymentNovakLegalEntityId
                or SmsTemplate.NovaPayOrderPrepaymentTkachLegalEntityId)
            {
                RefreshExternalPaymentsCommand.Execute(null);
            }
        }

        private void SetDisabledFields()
        {
            IsLockedOrEdidingCompleted = IsLocked || IsEditingCompleted;
            IsLockedByCurrentUserAndEditingAllowed = !IsEditingCompleted && LockerId == WebClient.AuthenticatedEmployee.Id;

            if (IsLocked)
            {
                WindowStatusInfo = "Редактирует: ";
                LockPerson = order.EmployeeLock.Name;
                IsLockedByCurrentUserAndEditingAllowed &= order.EmployeeLock.Id == WebClient.AuthenticatedEmployee.Id;
            }
        }

        private async Task SetNoProductAsync(OrderProductViewModel orderProduct)
        {
            if (orderProduct.Source.Real)
            {
                MessageFacadeService.ShowNotificationWarning(SourceAlreadySetWarning);
                return;
            }

            IsLongOperationInProgress = true;

            try
            {
                GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                    new GetTextFromUserParameter("Предложите альтернативу", "Альтернатива"),
                    this);

                if (viewModel.IsOk)
                {
                    await UpdatePurchaseSourceAsync(orderProduct, PurchaseSourceSaveDto.NoProduct(viewModel.Content));
                }

                MessageFacadeService.ShowNotificationInfo("Источник успешно установлен");

                Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private async Task SetNoPurchaseAsync()
        {
            IsLongOperationInProgress = true;

            try
            {
                HashSet<int> productIds = new HashSet<int>();

                foreach (OrderProductViewModel orderProduct in OrderProducts.Where(x => x.Source is NoneOrderProductSource && x.Id > 0))
                {
                    await WebClient.ExecuteApiRequestAsync(new UpdatePurchaseSource(orderProduct.Id, PurchaseSourceSaveDto.NoProduct("Товар нет необходимости покупать")));
                    productIds.Add(orderProduct.ProductId);
                }

                foreach (int productId in productIds)
                {
                    await WebClient.ExecuteApiRequestAsync(new CreateNoProductHistory(new NoProductCreateDto(NoProductReasonType.NoPurchase, Id, productId)));
                }

                Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(exception.Args.Error.GetErrorMessage());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to set purchase source");
                MessageFacadeService.ShowNotificationError("Ошибка установки источника");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private void CreateServiceRequest(OrderProductViewModel orderProduct)
        {
            Messenger.Send(new ServiceRequestCreateFromOrderViewMessage(Id, orderProduct.Product.Id));
        }

        private void UpdateCanAddProduct()
        {
            AddProductCommand.RaiseCanExecuteChanged();
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            if (State != OrderStatus.Received)
            {
                yield return new SummaryViewItem("Дата Х", GetDeliveryTimeInfo());
            }

            if (ReceiveTime.HasValue)
            {
                yield return new SummaryViewItem("Заберет", ReceiveTime.Value.ToString(DateFormattingRules.FullDateTimeFormat));
            }

            yield return new SummaryViewItem("Создал", employeeNames.GetValueOrDefault(CreatedBy));

            if (ConfirmedBy.HasValue)
            {
                int level = State == OrderStatus.Received
                    ? SummaryViewItem.RedLevel
                    : SummaryViewItem.NormalLevel;

                yield return new SummaryViewItem("Согласовал", employeeNames.GetValueOrDefault(ConfirmedBy.Value), level);
            }

            yield return new SummaryViewItem("Менеджер", EmployeeManagerId.HasValue ? employeeNames.GetValueOrDefault(EmployeeManagerId.Value) : SignalsConstants.EmptyValue);

            if (EmployeePackId.HasValue)
            {
                yield return new SummaryViewItem("Упаковал", employeeNames.GetValueOrDefault(EmployeePackId.Value));
            }

            if (CompletedBy.HasValue)
            {
                yield return new SummaryViewItem("Завершил", $"{employeeNames.GetValueOrDefault(CompletedBy.Value)} ({CompletedOn:dd.MM.yyyy HH:mm})");
            }

            yield return new SummaryViewItem("Статус", State?.Name);

            if (!string.IsNullOrWhiteSpace(order.CustomerStateText))
            {
                yield return new SummaryViewItem("Детализация", order.CustomerStateText);
            }

            if (order.CustomerReceivedOn.HasValue)
            {
                yield return new SummaryViewItem("Получен", order.CustomerReceivedOn.Value.ToString(DateFormattingRules.FullDateTimeFormat));
            }

            if (Id > 0)
            {
                yield return new SummaryViewItem("Заказ", Id.ToString());
            }

            if (!string.IsNullOrWhiteSpace(order.ExternalOrderId))
            {
                yield return new SummaryViewItem("Внешний заказ", order.ExternalOrderId);
            }

            if (BasedOnServiceRequestId.HasValue)
            {
                yield return new SummaryViewItem("Серв. заявка", BasedOnServiceRequestId.Value.ToString());
            }

            yield return OrderHelper.GetPayedInfoSummaryItem(Pko, SelectedPayment?.Id, "Оплата");

            yield return new SummaryViewItem("Веc", GetWeightInfo());

            if (!string.IsNullOrEmpty(PackageTtn))
            {
                yield return new SummaryViewItem("ТТН", PackageTtn);
            }

            string changeReasonComment = !string.IsNullOrEmpty(order.OrderStateChangeComment)
              ? $" ({order.OrderStateChangeComment})"
              : string.Empty;

            if (order.OrderStateChangeReasonId.HasValue)
            {
                ComboBoxItem changeReason = ChangeReasons.FirstOrDefault(x => x.Id == order.OrderStateChangeReasonId);

                if (!string.IsNullOrWhiteSpace(changeReason.DisplayValue))
                {
                    yield return new SummaryViewItem("Причина", $"{changeReason.DisplayValue}{changeReasonComment}");
                }
            }

            yield return new SummaryViewItem("РТиУ", BoolToStr(Rt == 1));

            yield return new SummaryViewItem("Готов к упаковке", BoolToStr(order.ReadyForPacking));

            if (!string.IsNullOrWhiteSpace(order.FiscalId))
            {
                yield return new SummaryViewItem("Фиск. чек", SignalsConstants.TrueStr);
            }

            string GetDeliveryTimeInfo()
            {
                string deliveryTime;

                if (DeliveryTime.HasValue && DeliveryTimeTo.HasValue)
                {
                    const string DateFormat = "dd.MM";
                    const string TimeFormat = "HH:mm";
                    const string DateTimeFormat = $"{DateFormat} {TimeFormat}";

                    deliveryTime = DeliveryTime.Value.Date == DeliveryTimeTo.Value.Date
                        ? $"{DeliveryTime.Value.ToString(DateFormat)} {DeliveryTime.Value.ToString(TimeFormat)}-{DeliveryTimeTo.Value.ToString(TimeFormat)}"
                        : $"{DeliveryTime.Value.ToString(DateTimeFormat)}-{DeliveryTimeTo.Value.ToString(DateTimeFormat)}";
                }
                else
                {
                    deliveryTime = "-";
                }

                return deliveryTime;
            }

            string GetWeightInfo()
            {
                double weight = PackageWeight > 0
                    ? PackageWeight
                    : GetOrderProducts()?.Sum(x => x.Quantity * x.Product.Weight) ?? 0;

                return $"{weight:F1} кг";
            }

            static string BoolToStr(bool value)
            {
                return value ? SignalsConstants.TrueStr : SignalsConstants.FalseStr;
            }
        }

        private async Task EditAsync()
        {
            IsLongOperationInProgress = true;

            OrderProductValidationStarted();
            RaisePropertyChanged(nameof(OrderProducts));

            try
            {
                LockResponse<OrderDto> lockResponse = await WebClient.ExecuteApiRequestAsync(new LockOrder(order.Id, checkPermissions: true));

                if (lockResponse.Success)
                {
                    IsLockedByCurrentUserAndEditingAllowed = !IsEditingCompleted;

                    order.EmployeeLock = lockResponse.Dto.EmployeeLock;
                    order.EmployeeLockId = WebClient.AuthenticatedEmployee.Id;

                    Messenger.Send(new OrderMessage(lockResponse.Dto, MessageType.Changed));

                    LockerId = WebClient.AuthenticatedEmployee.Id;

                    if (AssemblyWarehouse == null && AnyAssemblies && IsLockedByCurrentUserAndEditingAllowed)
                    {
                        AssemblyWarehouse = AssemblyWarehouses.FirstOrDefault(x => x.Id == SelectedWarehouse?.AssemblyWarehouseId);
                    }
                }
                else
                {
                    await RefreshOrderAsync(lockResponse.Dto);

                    ShowOrderIsAlreadyLockedMessage(lockResponse.Dto.EmployeeLock?.ShortName);
                }
            }
            catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to lock order");
                MessageFacadeService.ShowNotificationError("Ошибка при попытке редактирования заказа");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private void ResetValidation()
        {
            ResetContractorValidation();
            ResetPaymentValidation();
            ResetCarryTypeValidation();
            ResetWarehouseValidation();
            ResetAdditionalServiceWarehouseValidation();

            RaisePropertiesChanged(
                nameof(SelectedSubdivision),
                nameof(SelectedContractor),
                nameof(SelectedPayment),
                nameof(SelectedCity),
                nameof(SelectedCarryType),
                nameof(SelectedWarehouse));
        }

        private void ResetWarehouseValidation()
        {
            if (SelectedCarryType == null || !SelectedCarryType.Valid)
            {
                Warehouses.ForEach(x => x.Valid = false);
            }
            else
            {
                foreach (OrderWarehouseViewItem warehouse in Warehouses)
                {
                    bool valid = warehouseDeliveries.Any(x => x.WarehouseId == warehouse.Id
                        && x.CarryIds.Contains(SelectedCarryType.Id)
                        && (x.SubdivisionId == null || x.SubdivisionId.Value == SelectedSubdivision?.Id));

                    valid &= warehouse.Active;

                    if (SelectedCarryType.IsLocal)
                    {
                        valid &= warehouse.CityId == SelectedCity?.Id;
                    }

                    warehouse.Valid = valid;
                }
            }

            Warehouses = Warehouses.OrderByDescending(x => x.Valid).ThenByDescending(x => x.Position).ToReadOnlyObservableCollection();
        }

        private void ResetAdditionalServiceWarehouseValidation()
        {
            int?[] additionalServiceIds = OrderProducts
                .Where(x => x.AdditionalServiceId.HasValue && x.IsAdditionalService)
                .Select(x => x.AdditionalServiceId)
                .ToArray();

            foreach (OrderWarehouseViewItem warehouse in AdditionalServiceWarehouses)
            {
                bool valid = warehousePerformances.Any(x => x.WarehouseId == warehouse.Id
                && x.Activity
                && (additionalServiceIds.Length == 0 || additionalServiceIds.Contains(x.AdditionalServiceId)));

                valid &= warehouse.Active;

                warehouse.Valid = valid;
            }

            AdditionalServiceWarehouses = AdditionalServiceWarehouses
                .OrderByDescending(x => x.Valid)
                .ThenByDescending(x => x.Position)
                .ToReadOnlyObservableCollection();
        }

        private void ResetCarryTypeValidation()
        {
            if (SelectedCity == null || !SelectedCity.Valid)
            {
                CarryTypes.ForEach(x => x.Valid = false);
            }
            else
            {
                double totalWeight = GetOrderProducts().Sum(x => x.Product.Weight * x.Quantity);

                bool confirmOrderWithWarehouseFreeDeliveryValidationError = WebClient.IsOperationAllowed(BusinessOperation.ConfirmOrderWithWarehouseFreeDeliveryValidationError);

                foreach (OrderCarryTypeViewItem carryType in CarryTypes)
                {
                    carryType.Valid = carryType.Active
                        && SelectedCity.CityCarries.Contains(carryType.Id)
                        && warehouseDeliveries.Any(x => x.CarryIds.Contains(carryType.Id))
                        && (carryType.WeightLimit == null || carryType.WeightLimit >= totalWeight)
                        && (SelectedOrderSource?.Id == OrderSourceType.Monomarket || carryType.OrderCostLimit == null || carryType.OrderCostLimit >= OrderPaymentViewModel.SummaryItem.Uah)
                        && (carryType.AllowFreeUnderLimit
                            || carryType.MinFreeDeliveryCost == 0
                            || (GetOrderProducts().All(x => x.FreeDelivery != true) && FreeDelivery != true)
                            || (FreeDelivery && confirmOrderWithWarehouseFreeDeliveryValidationError)
                            || OrderPaymentViewModel.SummaryItem.Uah + OrderPaymentViewModel.BonusesItem.Uah >= carryType.MinFreeDeliveryCost);
                }
            }

            CarryTypes = CarryTypes.OrderByDescending(x => x.Valid).ThenBy(x => x.Position).ToObservableCollection();

            RaisePropertyChanged(nameof(SelectedCarryType));
        }

        private void ResetPaymentValidation()
        {
            IEnumerable<Payment> allowedPayments = Dictionaries.GetItems<Payment>();

            if (SelectedContractor != null)
            {
                allowedPayments = allowedPayments
                    .Where(x => Dictionaries.GetPaymentsBySubdivision(SelectedContractor.Subdivision.Id).Contains(x.Id));
            }

            Payments.ForEach(payment => payment.Valid = allowedPayments.Any(x => x.Id == payment.Id));
            Payments = Payments.OrderByDescending(x => x.Valid).ThenBy(x => x.Id).ToReadOnlyObservableCollection();
        }

        private void ResetContractorValidation()
        {
            foreach (OrderContractorViewItem contractor in Contractors)
            {
                contractor.Valid = WebClient.AuthenticatedEmployee.AllowSubdivisions.Contains(contractor.Subdivision.Id);
            }
        }

        private void OrderProductValidationStarted()
        {
            RaisePropertyChanged(nameof(SelectedWarehouse));
            RaisePropertyChanged(nameof(OrderProducts));

            foreach (OrderProductViewModel orderProductViewModel in OrderProducts)
            {
                ValidationContext context = new ValidationContext(orderProductViewModel);

                List<ValidationResult> validationResults = new List<ValidationResult>();

                Validator.TryValidateObject(orderProductViewModel, context, validationResults, true);

                bool isValid = validationResults.Count == 0;

                if (!isValid)
                {
                    IsOrderProductsValid = false;
                    return;
                }
            }

            IsOrderProductsValid = true;
        }

        private void ApplyContractorTemplate(ContractorTemplateDto template)
        {
            if (!MessageFacadeService.Confirm("Данные в заказе будут заменены, продолжить?"))
            {
                return;
            }

            LastName = template.LastName;
            FirstName = template.LastName;
            MiddleName = template.MiddleName;
            Phone = template.Phone;
            Phone2 = template.Phone2;
            Email = template.Email;

            SelectedCity = Cities.FirstOrDefault(x => x.Id == template.CityId);
            SelectedCarryType = CarryTypes.FirstOrDefault(x => x.Id == template.CarryId);
            SelectedWarehouse = Warehouses.FirstOrDefault(x => x.Id == template.WarehouseId);

            Address = template.Address;
            DeliveryData = template.DeliveryData;

            RaisePropertyChanged(nameof(Address));
        }

        private async Task RefreshAuditEntriesAsync(OrderHistoryFilterItem filterItem)
        {
            if (filterItem == null)
            {
                return;
            }

            AuditEntries = null;

            try
            {
                IAuditEntryProcessorBuilder auditEntryProcessorBuilder;
                int[] orderProductIds;
                int[] externalPaymentIds;

                switch (filterItem.FilterType)
                {
                    case OrderHistoryType.Order:
                        orderProductIds = GetOrderProducts().Select(x => x.Id).ToArray();
                        externalPaymentIds = Array.Empty<int>();
                        auditEntryProcessorBuilder = new OrderAuditEntryProcessorBuilder(WebClient, Dictionaries);
                        break;
                    case OrderHistoryType.Product:
                        orderProductIds = new[] { filterItem.Id };
                        externalPaymentIds = Array.Empty<int>();
                        auditEntryProcessorBuilder = new OrderProductAuditEntryProcessorBuilder(WebClient, Dictionaries);
                        break;
                    case OrderHistoryType.DeletedProducts:
                        orderProductIds = Array.Empty<int>();
                        externalPaymentIds = Array.Empty<int>();
                        auditEntryProcessorBuilder = new OrderProductAuditEntryProcessorBuilder(WebClient, Dictionaries);
                        break;
                    case OrderHistoryType.ExternalPayment:
                        orderProductIds = Array.Empty<int>();
                        externalPaymentIds = ExternalPayments?.Where(x => x.Id == filterItem.Id).Select(x => x.Id).ToArray() ?? Array.Empty<int>();
                        auditEntryProcessorBuilder = new ExternalPaymentAuditEntryProcessorBuilder(WebClient, Dictionaries);
                        break;
                    default:
                        throw new NotSupportedException();
                }

                OrderIdentityDto orderIdentity = new OrderIdentityDto(Id, orderProductIds, externalPaymentIds);

                QueryOrderHistory request = new QueryOrderHistory(Id, orderIdentity, filterItem.FilterType);

                IReadOnlyCollection<AuditEntryDto> auditEntries = await WebClient.ExecuteApiRequestAsync(request);

                IAuditEntryProcessor processor = await auditEntryProcessorBuilder.BuildAsync();

                AuditEntries = processor.Process(auditEntries).OrderByDescending(y => y.PropertyName).ToObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get order history");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void MailTo(string email)
        {
            ProcessHelper.Start($"mailto:{email}");
        }

        private void HandleSelectionChanged(ValueChangedEventArgs<FrameworkElement> e)
        {
            switch (e.NewValue.Name)
            {
                case "Tab2":
                    IsHelpVisible = true;

                    if (Calls == null)
                    {
                        RefreshCallsCommand.Execute(null);
                    }

                    break;
                case "Tab3":
                    IsHelpVisible = true;

                    if (OrderBills == null && OrderBillsVisible)
                    {
                        RefreshOrderBillsCommand.Execute(null);
                    }

                    break;
                case "CrmTab":
                    IsHelpVisible = false;

                    if (ClientContactsHistoryItems == null)
                    {
                        RefreshCrmCommand.Execute(null);
                    }

                    break;
                case "HistoryTab":
                    IsHelpVisible = false;

                    if (AuditEntries == null)
                    {
                        RefreshAuditEntriesCommand.Execute(SelectedHistoryFilterItem);
                    }

                    break;

                case "Tab5":
                    IsHelpVisible = false;

                    if (Complaints == null)
                    {
                        RefreshComplaintsCommand.Execute(null);
                    }

                    break;

                case "Tab6":
                    IsHelpVisible = false;

                    if (ServiceRequests == null)
                    {
                        RefreshServiceRequestsCommand.Execute(null);
                    }

                    break;

                default:
                    IsHelpVisible = false;
                    break;
            }
        }

        private void DragOrderProductOver(TreeListDragOverEventArgs args)
        {
            DraggedProductsErrors.Clear();

            if (args.DropTargetType == DropTargetType.InsertRowsIntoNode || args.DropTargetType == DropTargetType.InsertRowsBefore)
            {
                args.AllowDrop = false;
                args.Handled = true;
                return;
            }

            if (args.TargetNode?.Content is OrderProductViewModel targetRow)
            {
                foreach (var row in args.DraggedRows)
                {
                    if (row is TreeListNode { Content: OrderProductViewModel draggedRow })
                    {
                        DraggedProductsErrors.Add(draggedRow.ProductName, null);

                        if (draggedRow.OrderFolderId.HasValue)
                        {
                            if (targetRow.OrderFolderId is null)
                            {
                                args.AllowDrop = false;
                                DraggedProductsErrors[draggedRow.ProductName] = $"Нельзя доставать товар из {(draggedRow.OrderFolder?.TypeId == OrderFolderType.BundleId ? "бандла" : "сборки")}";
                                continue;
                            }

                            if (targetRow.OrderFolderId != draggedRow.OrderFolderId)
                            {
                                args.AllowDrop = false;
                                DraggedProductsErrors[draggedRow.ProductName] = $"Товар уже в {(draggedRow.OrderFolder?.TypeId == OrderFolderType.BundleId ? "бандле" : "сборке")}";
                                continue;
                            }

                            if (args.DropTargetType == DropTargetType.InsertRowsBefore
                                && targetRow.IsVirtualProduct
                                && targetRow.OrderFolderId == draggedRow.OrderFolderId)
                            {
                                args.AllowDrop = false;
                                DraggedProductsErrors[draggedRow.ProductName] = $"Нельзя доставать товар из {(draggedRow.OrderFolder?.TypeId == OrderFolderType.BundleId ? "бандла" : "сборки")}";
                                continue;
                            }
                        }

                        if (targetRow.OrderFolderId.HasValue)
                        {
                            OrderFolderDto firstOrderFolder = OrderProducts.Where(x => x.OrderFolder != null).Select(x => x.OrderFolder).First(x => x.Id == targetRow.OrderFolderId);

                            if (firstOrderFolder.TypeId == OrderFolderType.BundleId)
                            {
                                args.AllowDrop = false;
                                DraggedProductsErrors[draggedRow.ProductName] = "Нельзя вставлять товар в бандл";
                                continue;
                            }

                            if (firstOrderFolder.TypeId == OrderFolderType.AssemblyServiceId)
                            {
                                AssemblyServiceDto[] folderAssemblyServices = AssemblyServices.Where(p => p.Products.Any(k => k.OrderFolderId == targetRow.OrderFolderId.Value)).ToArray();

                                if (folderAssemblyServices.Any(x => x.StateId != AssemblyServiceState.Waiting.Id && x.StateId != AssemblyServiceState.Warehouse.Id))
                                {
                                    args.AllowDrop = false;
                                    DraggedProductsErrors[draggedRow.ProductName] = "Сборка уже взята в работу";
                                    continue;
                                }

                                if (folderAssemblyServices.Any(x => x.EmployeeLockId.HasValue))
                                {
                                    args.AllowDrop = false;
                                    DraggedProductsErrors[draggedRow.ProductName] = "Сборка заблокирована";
                                    continue;
                                }
                            }

                            if (draggedRow.OrderFolderId != targetRow.OrderFolderId &&
                                (draggedRow.Quantity % firstOrderFolder.Quantity > 0
                                 || (Constants.UniqueComplectCategoryIds.Contains(draggedRow.Product.ParentCategoryId)
                                     && (OrderProducts.Any(x => x.OrderFolderId == firstOrderFolder.Id && x.Product.ParentCategoryId == draggedRow.Product.ParentCategoryId)
                                         || draggedRow.Quantity > firstOrderFolder.Quantity))))
                            {
                                args.AllowDrop = false;
                                DraggedProductsErrors[draggedRow.ProductName] = "Некорректное количество товара в сборке";
                                continue;
                            }
                        }

                        if (draggedRow.IsAdditionalService)
                        {
                            args.AllowDrop = false;
                            DraggedProductsErrors[draggedRow.ProductName] = "Нельзя перемещать услугу";
                            continue;
                        }

                        if (draggedRow.IsGift)
                        {
                            args.AllowDrop = false;
                            DraggedProductsErrors[draggedRow.ProductName] = "Нельзя перемещать подарок";
                            continue;
                        }

                        if (draggedRow.IsVirtualProduct)
                        {
                            args.AllowDrop = false;
                            DraggedProductsErrors[draggedRow.ProductName] = "Нельзя перемещать папку";
                            continue;
                        }

                        if (draggedRow.IsAdditionalServiceConsumable)
                        {
                            args.AllowDrop = false;
                            DraggedProductsErrors[draggedRow.ProductName] = "Нельзя перемещать товар для оказания";
                            continue;
                        }

                        if (OrderProducts.Any(x => x.ParentRecordId == targetRow.Id && x.IsAdditionalService))
                        {
                            args.AllowDrop = false;
                            DraggedProductsErrors[draggedRow.ProductName] = "Нельзя перемещать товар в услугу";
                            continue;
                        }

                        if (targetRow.IsAdditionalService && GetAdditionalServiceProduct(targetRow.Id)?.StateId == AdditionalServiceProductState.DoingId)
                        {

                            args.AllowDrop = false;
                            DraggedProductsErrors[draggedRow.ProductName] = "Услуга находится в статусе \"Выполняется\"";
                            continue;
                        }

                        if (OrderProducts.Any(x => x.ParentRecordId == draggedRow.Id && !x.IsGift))
                        {
                            args.AllowDrop = false;
                            DraggedProductsErrors[draggedRow.ProductName] = "У товара есть дочерние товары";
                            continue;
                        }

                        OrderProductViewModel[] targetChildProducts = OrderProducts.Where(x => x.ParentRecordId == targetRow.Id).ToArray();

                        if (targetChildProducts.Any(x => x.IsGift))
                        {
                            args.AllowDrop = false;
                            DraggedProductsErrors[draggedRow.ProductName] = "Нельзя перемещать товар в подарок";
                            continue;
                        }

                        if (targetRow.IsAdditionalService)
                        {
                            AdditionalServiceProductDto additionalServiceProduct = AdditionalServiceProducts.FirstOrDefault(x => x.OrderProductId == targetRow.Id);

                            if (additionalServiceProduct is not null)
                            {
                                if (additionalServiceProduct.StateId == AdditionalServiceProductState.CompletedId)
                                {
                                    args.AllowDrop = false;
                                    DraggedProductsErrors[draggedRow.ProductName] = "Услуга завершена";
                                    continue;
                                }
                            }

                            if (targetRow.AdditionalServiceId.HasValue && additionalServices.TryGetValue(targetRow.AdditionalServiceId.Value, out AdditionalServiceDto additionalService) && !additionalService.RequireProductsToProvide)
                            {
                                args.AllowDrop = false;
                                DraggedProductsErrors[draggedRow.ProductName] = "Услуга не требует товар для оказания";
                                continue;
                            }

                            if (draggedRow.Quantity > targetRow.Quantity)
                            {
                                args.AllowDrop = false;
                                DraggedProductsErrors[draggedRow.ProductName] = "Некорректное количество товара";
                                continue;
                            }
                        }

                        if (draggedRow.OrderFolderId == targetRow.OrderFolderId && !targetRow.IsAdditionalService)
                        {
                            args.AllowDrop = false;
                            DraggedProductsErrors[draggedRow.ProductName] = "Перестановка не имеет смысла";
                            continue;
                        }
                    }
                }

                args.Handled = true;
                return;
            }

            args.AllowDrop = false;
            args.Handled = true;
        }

        private async Task DropOrderProductAsync(TreeListDropEventArgs args)
        {
            try
            {
                List<OrderProductViewModel> draggedRows = new List<OrderProductViewModel>();
                foreach (var row in args.DraggedRows)
                {
                    if (row is TreeListNode node && node.Content is OrderProductViewModel draggedRow)
                    {
                        draggedRows.Add(draggedRow);
                    }
                }

                if (args.TargetNode.Content is OrderProductViewModel targetRow)
                {
                    if (targetRow.IsAdditionalService)
                    {
                        draggedRows.ForEach(x =>
                        {
                            x.ParentRecordId = targetRow.Id;
                            x.IsAdditionalServiceConsumable = true;
                        });
                    }

                    var groupedGiftOrderProducts = OrderProducts
                        .Where(x => draggedRows.Any(y => y.Id == x.ParentRecordId) && x.IsGift)
                        .GroupBy(x => x.ParentRecordId)
                        .Select(x => new { ParentRecord = OrderProducts.First(y => y.Id == x.Key), Childrens = x.ToArray() })
                        .ToArray();

                    if (targetRow.OrderFolderId.HasValue && draggedRows.All(x => x.OrderFolderId != targetRow.OrderFolderId))
                    {
                        OrderFolderDto orderFolder = targetRow.OrderFolder;

                        OrderProductViewModel[] oldAssemblyProducts = OrderProducts.Where(x => x.OrderFolderId == targetRow.OrderFolderId).ToArray();

                        OrderProductViewModel assemblyVirtualProduct = oldAssemblyProducts.First(x => x.IsVirtualProduct);

                        OrderProductViewModel[] allDraggedOrderProducts = groupedGiftOrderProducts.SelectMany(x => x.Childrens).Union(draggedRows).ToArray();

                        foreach (OrderProductViewModel draggedOrderProduct in allDraggedOrderProducts)
                        {
                            draggedOrderProduct.OrderFolderId = targetRow.OrderFolderId;

                            draggedOrderProduct.ParentId = assemblyVirtualProduct.Id;

                            if (draggedOrderProduct.Quantity % orderFolder.Quantity > 0)
                            {
                                throw new Exception("Incorrect product quantity put into order folder");
                            }

                            draggedOrderProduct.AssemblyQuantity = draggedOrderProduct.Quantity / orderFolder.Quantity;

                            draggedOrderProduct.OrderFolder = new OrderFolderDto()
                            {
                                Id = targetRow.OrderFolderId.Value,
                                TypeId = orderFolder.TypeId,
                                Name = orderFolder.Name,
                                PriceId = orderFolder.PriceId,
                                ProductId = orderFolder.ProductId,
                                Quantity = orderFolder.Quantity,
                                BonusesToCharge = orderFolder.BonusesToCharge
                            };

                            AdditionalServiceDto additionalServiceDto = null;

                            if (draggedOrderProduct.Product.TypeId == ProductType.ServiceCertificateId || draggedOrderProduct.Product.TypeId == ProductType.ServiceId)
                            {
                                int[] productIds = draggedOrderProduct.Product.Id.Yield().ToArray();

                                PagedResult<AdditionalServiceDto> additionalServices = await WebClient.ExecuteApiRequestAsync(new QueryAdditionalServices(new AdditionalServicesFilteringItem(productIds, true)), true);

                                additionalServiceDto = additionalServices.Data.FirstOrDefault(x => x.ProductId == draggedOrderProduct.Product.Id);
                            }

                            CategoryFullDto categoryFullDto = await WebClient.ExecuteApiRequestAsync(new QueryCategoryFull(draggedOrderProduct.Product.ParentCategoryId));

                            draggedOrderProduct.AssemblyIncluded = IsAssemblyOrAssembledComputerRuleFolder(orderFolder)
                                                                   && (categoryFullDto?.TypeId == CategoryType.PcComponents.Id ||
                                                                       (additionalServiceDto?.AssemblyPart ?? orderFolder.TypeId == OrderFolderType.AssembledComputerRuleId))
                                                                   && !draggedOrderProduct.IsAdditionalServiceConsumable;
                        }

                        if (OrderProducts.Any(x => x.OrderFolderId == targetRow.OrderFolderId && x.ProductId == Constants.AssemblyServiceProductId))
                        {
                            bool compatible = await CheckCompatibilityMultipleAsync(allDraggedOrderProducts);

                            if (!compatible)
                            {
                                var viewModel = SizeableDialogDocumentManagerService
                                    .ShowView<AssemblyViewModel>(
                                        new AssemblyParameter(
                                            SelectedContractor.Id,
                                            OrderRules.NeedCheckGifts(SelectedSubdivision),
                                            true,
                                            orderFolder,
                                            allDraggedOrderProducts.Union(oldAssemblyProducts).Select(x => new AssemblyParameterProduct(x.ProductId, x.Quantity, false, true, x.IsGift, x.Price)).ToReadOnlyCollection()),
                                        this);

                                if (viewModel.IsOk)
                                {
                                    List<OrderProductViewModel> orderProductsToDelete = new List<OrderProductViewModel>();

                                    List<AssemblyViewModelResult> assemblyProducts = viewModel.GetProducts().ToList();

                                    var draggedProducts = SelectedProducts.ToArray();

                                    foreach (OrderProductViewModel orderProductInternal in draggedProducts)
                                    {
                                        AssemblyViewModelResult product = assemblyProducts.FirstOrDefault(x => x.NomenclatureItem.Id == orderProductInternal.Product.Id);

                                        if (product is null)
                                        {
                                            orderProductsToDelete.Add(orderProductInternal);
                                        }
                                        else
                                        {
                                            if (orderProductInternal.AssemblyQuantity < product.NomenclatureItem.Quantity)
                                            {
                                                orderProductInternal.Quantity += product.NomenclatureItem.Quantity - orderProductInternal.AssemblyQuantity.Value;
                                            }
                                            else if (orderProductInternal.AssemblyQuantity > product.NomenclatureItem.Quantity)
                                            {
                                                int removeQuantity = orderProductInternal.AssemblyQuantity.Value - product.NomenclatureItem.Quantity;

                                                foreach (OrderProductViewModel orderProductViewModel in OrderProducts.Where(x => x.Product.Id == product.NomenclatureItem.Id && x.OrderFolderId == targetRow.OrderFolderId))
                                                {
                                                    if (orderProductViewModel.Quantity <= removeQuantity)
                                                    {
                                                        removeQuantity -= orderProductViewModel.Quantity;
                                                        orderProductsToDelete.Add(orderProductViewModel);
                                                    }
                                                    else
                                                    {
                                                        orderProductViewModel.Quantity -= removeQuantity;

                                                        break;
                                                    }
                                                }
                                            }

                                            foreach (OrderProductViewModel orderProductViewModel in OrderProducts.Where(x => x.Product.Id == product.NomenclatureItem.Id && x.OrderFolderId == targetRow.Id))
                                            {
                                                orderProductViewModel.AssemblyQuantity = product.NomenclatureItem.Quantity;
                                                orderProductViewModel.AssemblyIncluded = product.AssemblyIncluded;
                                            }
                                        }
                                    }

                                    foreach (OrderProductViewModel orderProductViewModel in orderProductsToDelete)
                                    {
                                        foreach (OrderProductViewModel childOrderProduct in OrderProducts.Where(x => x.ParentRecordId == orderProductViewModel.Id).ToArray())
                                        {
                                            if (!(childOrderProduct.IsAdditionalService && IsAdditionalServiceProductCompleted(childOrderProduct.Id)))
                                            {
                                                childOrderProduct.ValidationStarted -= OrderProductValidationStarted;
                                                OrderProducts.Remove(childOrderProduct);
                                            }
                                        }

                                        orderProductViewModel.ValidationStarted -= OrderProductValidationStarted;

                                        OrderProducts.Remove(orderProductViewModel);
                                    }

                                    int[] draggedProductIds = draggedProducts
                                        .Select(x => x.ProductId)
                                        .ToArray();

                                    int[] oldProductIds = oldAssemblyProducts
                                        .Select(x => x.ProductId)
                                        .ToArray();

                                    List<AssemblyViewModelResult> assemblyResults = assemblyProducts
                                        .Where(x => !draggedProductIds.Union(oldProductIds).Contains(x.NomenclatureItem?.Id ?? 0) && !x.IsGift)
                                        .ToList();

                                    if (assemblyResults.Any())
                                    {
                                        var productsFromCatalog = await WebClient.ExecuteCatalogApiRequestAsync(
                                            new QueryProductByIds(
                                                SelectedContractor.Id,
                                                assemblyResults.Select(x => x.NomenclatureItem.Id).ToArray(),
                                                OrderRules.NeedCheckGifts(SelectedSubdivision),
                                                true,
                                                priceIn: true,
                                                cartProductIds: draggedProductIds,
                                                conditionGifts: targetRow.OrderFolder?.TypeId != OrderFolderType.AssembledComputerRuleId));

                                        NomenclatureViewItem[] nomenclatureItems = Mapper.Map<NomenclatureViewItem[]>(productsFromCatalog);

                                        foreach (AssemblyViewModelResult assemblyResult in assemblyResults)
                                        {
                                            NomenclatureViewItem nomenclatureItem = nomenclatureItems.First(x => x.Id == assemblyResult.NomenclatureItem.Id);

                                            nomenclatureItem.Quantity = assemblyResult.NomenclatureItem.Quantity;
                                            nomenclatureItem.AssemblyIncluded = assemblyResult.NomenclatureItem.AssemblyIncluded;

                                            assemblyResult.NomenclatureItem = nomenclatureItem;
                                        }

                                        ProcessSelectedForAddingProducts(assemblyResults, targetRow.OrderFolder, 1);
                                    }
                                    else
                                    {
                                        PostProcessOrderProducts(OrderProducts.ToList(), PostProcessProductsMode.ReplaceRange);
                                    }

                                    if (OrderProducts.Any(x => x.OrderFolderId == targetRow.OrderFolder.Id && x.ProductId == Constants.AssemblyServiceProductId))
                                    {
                                        await CalculateAssemblyServiceProductPriceAsync(targetRow.Id);
                                    }
                                }
                                else
                                {
                                    allDraggedOrderProducts.ForEach(x =>
                                    {
                                        x.OrderFolderId = null;
                                        x.AssemblyQuantity = null;
                                        x.AssemblyIncluded = false;
                                        x.OrderFolder = null;
                                        x.ParentRecordId = null;
                                        x.ParentId = null;
                                        x.IsAdditionalServiceConsumable = false;
                                    });

                                    foreach (var groupedGiftOrderProduct in groupedGiftOrderProducts)
                                    {
                                        groupedGiftOrderProduct.Childrens.ForEach(x =>
                                        {
                                            x.OrderFolderId = null;
                                            x.AssemblyQuantity = null;
                                            x.AssemblyIncluded = false;
                                            x.OrderFolder = null;
                                            x.ParentId = null;
                                            x.IsAdditionalServiceConsumable = false;

                                        });

                                        int parentPosition = OrderProducts.IndexOf(groupedGiftOrderProduct.ParentRecord);

                                        OrderProducts.RemoveRange(groupedGiftOrderProduct.Childrens);

                                        OrderProducts.InsertRange(parentPosition + 1, groupedGiftOrderProduct.Childrens);
                                    }

                                    OrderProducts = OrderProducts.ToObservableRangeCollection();

                                    return;
                                }
                            }
                        }

                        bool freeDelivery = OrderProducts.All(x => x.OrderFolderId == targetRow.OrderFolderId.Value && x.FreeDelivery) && draggedRows.All(x => x.FreeDelivery);

                        foreach (OrderProductViewModel folderOrderProduct in OrderProducts.Where(x => x.OrderFolderId == targetRow.OrderFolderId.Value && x.OrderFolder is not null))
                        {
                            folderOrderProduct.OrderFolder.Price += draggedRows.Sum(x => x.Price * x.Quantity);
                            folderOrderProduct.OrderFolder.FreeDelivery = freeDelivery;
                        }

                        assemblyVirtualProduct.PriceOut += draggedRows.Sum(x => x.Price);

                        if (orderFolder.TypeId != OrderFolderType.AssembledComputerRuleId)
                        {
                            await CalculateAssemblyServiceProductPriceAsync(orderFolder.Id);
                        }

                        // reset orderProducts to recalculate folders on UI
                        OrderProductViewModel[] tempOrderProducts = OrderProducts.ToArray();

                        OrderProducts.CollectionChanged -= OrderProductsCollectionChangedEvent;

                        OrderProducts.Clear();

                        OrderProducts = new ObservableRangeCollection<OrderProductViewModel>();

                        PostProcessOrderProducts(
                            tempOrderProducts,
                            PostProcessProductsMode.AddRange,
                            true);

                        OrderProductsCollectionChanged();

                        OrderProducts.CollectionChanged += OrderProductsCollectionChangedEvent;

                        if (groupedGiftOrderProducts.Any())
                        {
                            foreach (var groupedGiftOrderProduct in groupedGiftOrderProducts)
                            {
                                int parentPosition = OrderProducts.IndexOf(groupedGiftOrderProduct.ParentRecord);

                                OrderProducts.RemoveRange(groupedGiftOrderProduct.Childrens);

                                OrderProducts.InsertRange(parentPosition + 1, groupedGiftOrderProduct.Childrens);
                            }

                            // reset orderProducts to recalculate gifts positions
                            tempOrderProducts = OrderProducts.ToArray();

                            OrderProducts.CollectionChanged -= OrderProductsCollectionChangedEvent;

                            OrderProducts.Clear();

                            OrderProducts = new ObservableRangeCollection<OrderProductViewModel>();

                            PostProcessOrderProducts(
                                tempOrderProducts,
                                PostProcessProductsMode.AddRange,
                                true);

                            OrderProductsCollectionChanged();

                            OrderProducts.CollectionChanged += OrderProductsCollectionChangedEvent;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to drop order products");
                MessageFacadeService.ShowNotificationError("Непредвиденная ошибка");

                if (order?.Id > 0)
                {
                    Close();
                }
            }
        }

        private void HandleSelectionProductInfoChanged(ValueChangedEventArgs<FrameworkElement> e)
        {
            switch (e.NewValue.Name)
            {
                case "ProductAssemblyServicesTab":

                    RefreshAssemblyServicesCommand.Execute(null);

                    break;
                case "ProductAdditionalServiceProductsTab":

                    RefreshAdditionalServiceProductsCommand.Execute(null);

                    break;
                case "ProductPromosTab":

                    RefreshPromosCommand.Execute(null);

                    break;
            }
        }

        private async Task RemovePromoCodeAsync(OrderPromoCodeDto promoCode)
        {
            (bool Success, CheckPromoCodesResponse Result) response = await ProcessPromoCodesAsync(
                PromoCodes.Where(x => x.Value != promoCode.Value && x.PromoCodeTypeId != PromoCodeType.Bundle.Id).Select(x => x.Value).ToArray(),
                null,
                OrderProducts.Where(x => x.OrderPromoCodeId is not null && PromoCodes.First(y => y.PromoCodeId == x.OrderPromoCodeId).PromoCodeTypeId != PromoCodeType.Bundle.Id));

            if (response.Success && response.Result != null)
            {
                PromoCodes.Remove(promoCode);
                RecalculateCanChangePriceIdForCurrentProduct();
            }
        }

        private Task RefreshPromoCodesAsync()
        {
            if (PromoCodes.Count == 0)
            {
                return Task.CompletedTask;
            }

            if (SelectedSubdivision != Subdivision.Telemart)
            {
                MessageFacadeService.ShowNotificationWarning("Использование промо-кодов возможно лишь с подразделением «Телемарт»");
                return Task.CompletedTask;
            }

            return ProcessPromoCodesAsync(PromoCodes.Select(x => x.Value).ToArray(), null);
        }

        private async Task AddPromoCodeAsync()
        {
            if (SelectedSubdivision != Subdivision.Telemart)
            {
                MessageFacadeService.ShowNotificationWarning("Использование промо-кодов возможно лишь с подразделением «Телемарт»");
                return;
            }

            GetTextFromUserViewModel vm = GetInfoDialog("Введите промо-код или код акции", "Промо-код / код акции");

            if (!vm.IsOk)
            {
                return;
            }

            string promoCode = vm.Content;
            string[] promoCodes = null;
            int[] promoCodeIds = null;

            if (int.TryParse(promoCode, out int promoCodeId))
            {
                if (PromoCodes.Any(x => x.PromoCodeId == promoCodeId))
                {
                    MessageFacadeService.ShowNotificationWarning($"Промо-код «{promoCode}» уже добавлен в заказ");
                    return;
                }

                promoCodeIds = PromoCodes.Where(x => x.PromoCodeTypeId != PromoCodeType.Bundle.Id).Select(x => x.PromoCodeId).Concat(promoCodeId).ToArray();
            }
            else
            {
                if (PromoCodes.Any(x => string.Equals(x.Value, promoCode, StringComparison.Ordinal)))
                {
                    MessageFacadeService.ShowNotificationWarning($"Промо-код «{promoCode}» уже добавлен в заказ");
                    return;
                }

                promoCodes = PromoCodes.Where(x => x.PromoCodeTypeId != PromoCodeType.Bundle.Id).Select(x => x.Value).Concat(promoCode).ToArray();
            }

            var request = promoCodeId > 0
                ? new QueryPromoCode(promoCodeId)
                : new QueryPromoCode(promoCode);

            PromoCodeFullDto promoCodeData = await WebClient.ExecuteApiRequestAsync(request);

            if (promoCodeData.TypeId == PromoCodeType.Bundle.Id)
            {
                MessageFacadeService.ShowNotificationWarning($"Для добавления промо-кода с типом \"{PromoCodeType.Bundle.Name}\" воспользуйтесь функционалом 'Бандл -> Добавить'");
                return;
            }

            (bool Success, CheckPromoCodesResponse Result) response = await ProcessPromoCodesAsync(promoCodes, promoCodeIds, OrderProducts.Where(x => x.OrderPromoCodeId is null));

            if (response.Success && response.Result != null)
            {
                OrderPromoCodeDto dto = new OrderPromoCodeDto
                {
                    Id = new Random().GetRandomId(),
                    PromoCodeId = promoCodeData.Id,
                    Value = promoCodeData.Value,
                    PromoCodeMetaTitle = promoCodeData.MetaTitle,
                    PromoCodeTypeId = promoCodeData.TypeId
                };

                PromoCodes.Add(dto);
                RecalculateCanChangePriceIdForCurrentProduct();
            }
        }

        private void AddCall()
        {
            CreateCallParameter parameter = new CreateCallParameter(
                CallDocumentType.Order,
                Id,
                BasedOnServiceRequestId,
                SelectedSubdivision?.Id,
                SelectedContractor?.Id,
                FioStr,
                Phone,
                Phone2);

            CreateCallViewModel viewModel = DialogDocumentManagerService.ShowView<CreateCallViewModel>(parameter, this);

            if (viewModel.IsOk && viewModel.ResultCall != null)
            {
                CallViewItem callViewItem = Mapper.Map<CallViewItem>(viewModel.ResultCall);

                Calls.Add(callViewItem);
                Calls = new ObservableCollection<CallViewItem>(GetSortedCalls(Calls));

                MessageFacadeService.ShowNotificationInfo($"Звонок №{viewModel.ResultCall.Id} успешно создан");

                NewCallsCount = (NewCallsCount ?? 0) + 1;
                CallsCount = (CallsCount ?? 0) + 1;
            }
        }

        private void EditCall(CallViewItem callViewItem)
        {
            Messenger.Send(new CallViewMessage(callViewItem.Id));
        }

        private void OnCallMessage(CallMessage message)
        {
            if (message.Entity.OrderId == Id)
            {
                switch (message.MessageType)
                {
                    case MessageType.Changed:
                        {
                            CallViewItem existingCall = Calls?.FirstOrDefault(x => x.Id == message.Entity.Id);

                            if (existingCall != null)
                            {
                                Mapper.Map(message.Entity, existingCall);
                            }

                            break;
                        }
                }
            }
        }

        private void OnPurchaseMessage(PurchaseMessage message)
        {
            PurchaseDto purchase = message.Entity;

            if (message.MessageType == MessageType.Changed && purchase.OrderId == Id)
            {
                OrderProductViewModel orderProduct = GetOrderProducts().FirstOrDefault(x => x.Id == purchase.ProductId);

                if (orderProduct != null)
                {
                    orderProduct.Source = Dictionaries.GetOrderProductSource(
                        purchase.ProductSourceId,
                        purchase.ProductWarehouseId,
                        purchase.ProductSourceText,
                        purchase.ProductSourceDate);
                }
            }
        }

        private async Task RefreshCallsAsync()
        {
            Calls = null;

            try
            {
                CallFilteringItem filteringItem = new CallFilteringItem { OrderId = Id };

                PagedResult<CallDto> calls = await WebClient.ExecuteApiRequestAsync(new QueryCalls(filteringItem));

                IEnumerable<CallViewItem> viewItems = GetSortedCalls(calls.Data.Select(x => Mapper.Map<CallViewItem>(x)));

                Calls = new ObservableCollection<CallViewItem>(viewItems);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get order calls");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void OpenAssemblyService(int assemblyServiceId)
        {
            Messenger.Send(new AssemblyServiceViewMessage(assemblyServiceId));
        }

        private void OpenAdditionalServiceProduct(int additionalServiceProductId)
        {
            Messenger.Send(new AdditionalServiceProductViewMessage(additionalServiceProductId));
        }

        private void OpenCustomer()
        {
            DialogDocumentManagerService.ShowView<CustomerViewModel>(new CustomerParameter(order.CustomerId!.Value), this);
        }

        private async Task FindCustomerAsync()
        {
            CustomerFilteringItem item = new(Phone);

            PagedResult<CustomerDto> customers = await WebClient.ExecuteApiRequestAsync(new QueryCustomers(item));

            if (!customers.Data.Any())
            {
                MessageFacadeService.ShowNotificationInfo("Клиент не найден");
                return;
            }

            CustomerDto customer = customers.Data.First();

            if (!MessageFacadeService.Confirm($"ФИО: {customer.Fio}\nE-mail: {customer.Email}", "Верно?"))
            {
                return;
            }

            Fio fio = new(customer.Fio);

            FirstName = fio.FirstName;
            LastName = fio.LastName;
            MiddleName = fio.MiddleName;
            Email = customer.Email;
        }

        private async Task RefreshAdditionalServiceProductsAsync()
        {
            AdditionalServiceProductsFilteringItem filter = new AdditionalServiceProductsFilteringItem()
            {
                OrderIds = Id.ToString()
            };

            PagedResult<AdditionalServiceProductDto> additionalServiceProducts = await WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceProducts(filter));
            AdditionalServiceProducts = additionalServiceProducts.Data.ToObservableCollection();

            if (CurrentOrderProduct is null)
            {
                ProductAdditionalServiceProducts = null;
            }
            else
            {
                SetCurrentProductAdditionalServiceProducts();
            }
        }

        private async Task RefreshAssemblyServicesAsync()
        {
            AssemblyServicesFilteringItem filter = new AssemblyServicesFilteringItem()
            {
                OrderIds = Id.ToString()
            };

            PagedResult<AssemblyServiceDto> assemblies = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyServices(filter));
            AssemblyServices = assemblies.Data.ToReadOnlyObservableCollection();

            if (CurrentOrderProduct is null)
            {
                ProductAssemblyServices = null;
            }
            else
            {
                SetCurrentProductAssemblyServices();
            }
        }

        private async Task RefreshPromosAsync()
        {
            if (SelectedSubdivision is null)
            {
                return;
            }

            QueryProductByIdsDto filter = new QueryProductByIdsDto(
                OrderProducts
                    .Select(x => x.ProductId)
                    .ToArray(),
                Constants.TelemartContractorId)
            {
                PromoCodes = true,
                Bundles = true
            };

            List<ProductDto> products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(filter));

            Dictionary<int, int[]> productsPromoCodeIds = products
                .ToDictionary(
                    x => x.Id,
                    x => x.PromoCodes
                        .Select(y => y.Id)
                        .Union(x.Bundles.Select(y => y.Id))
                        .ToArray());

            List<int> promoCodeIds = productsPromoCodeIds.SelectMany(x => x.Value).Distinct().ToList();

            List<PromoCodeDto> promoCodes = await WebClient.ExecuteApiRequestAsync(new QueryPromoCodes(new PromoCodeFilteringItem { Active = true, Ids = promoCodeIds }));
            ProductsPromoCodes = productsPromoCodeIds.ToDictionary(x => x.Key, x => promoCodes.Where(y => x.Value.Contains(y.Id)).ToArray());

            if (CurrentOrderProduct is null)
            {
                ProductPromos = null;
            }
            else
            {
                SetCurrentProductPromos();
            }
        }

        private async Task RefreshServiceRequestsAsync()
        {
            try
            {
                ServiceRequests = null;

                PagedResult<ServiceRequestDto> serviceRequests = await WebClient.ExecuteApiRequestAsync(
                    new QueryServiceRequests(
                    new ServiceRequestFilteringItem() { OrderNumbers = Id.ToString() }));
                ServiceRequests = serviceRequests.Data.Select(x => Mapper.Map<ServiceRequestViewItem>(x)).ToObservableCollection();

                ServiceRequestsCount = ServiceRequests.Count;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to load service requests");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task SetWarehouseSourceAsync(OrderProductViewModel orderProduct)
        {
            if (orderProduct.IsAdditionalService && orderProduct.Product?.TypeId == ProductType.ServiceId && !WebClient.IsOperationAllowed(BusinessOperation.OrderChangeSourceServiceWithoutCertificate))
            {
                const string message = @"Для товаров с типом 'Услуга без сертификата' запрещена ручная установка источника,
так как товар не участвует в перемещениях. Автоматически будет установлен источником склад выдачи заказа при сохранении.";

                MessageFacadeService.ShowMessageBox(
                    message,
                    orderProduct.Product.NameFullRu,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (orderProduct.Source.Real)
            {
                MessageFacadeService.ShowNotificationWarning(SourceAlreadySetWarning);
                return;
            }

            IsLongOperationInProgress = true;

            try
            {
                QueryPurchaseWarehouseSources gatewayRequest = new QueryPurchaseWarehouseSources(orderProduct.Product!.Id, SelectedWarehouse?.Id, orderProduct.OrderFolder?.TypeId == OrderFolderType.AssembledComputerRule.Id);

                List<PurchaseWarehouseSourceDto> sources = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                if (sources.Any(x => (x.WarehouseItems - x.ReservedQuantity) >= orderProduct.Quantity))
                {
                    SetSourceParameter parameter = new SetSourceParameter
                    {
                        ProductRecordId = orderProduct.Id,
                        CurrencyId = orderProduct.CurrencyOutId,
                        Quantity = orderProduct.Quantity,
                        Price = orderProduct.PriceOut,
                        ProductId = orderProduct.Product.Id,
                        ProductName = orderProduct.Product.Name,
                        OrderProductState = orderProduct.State,
                        OrderId = Id,
                        OrderState = State,
                        OrderWarehouseId = SelectedWarehouse?.Id,
                        OrderWarehouseName = SelectedWarehouse?.Name,
                        OrderDeliveryTime = DeliveryTime,
                        OrderPaymentId = SelectedPayment?.Id
                    };

                    SizeableDialogDocumentManagerService.ShowView<SetWarehouseSourceViewModel>(new object[] { parameter, sources }, this);
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning($"Товара нигде нет в свободном остатке {orderProduct.Quantity.ToString(CultureInfo.InvariantCulture)} шт.");
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(exception.Args.Error.GetErrorMessage());
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private async Task SetMovementAsync(OrderProductViewModel orderProduct)
        {
            if (orderProduct.IsAdditionalService && orderProduct.Product?.TypeId == ProductType.ServiceId && !WebClient.IsOperationAllowed(BusinessOperation.OrderChangeSourceServiceWithoutCertificate))
            {
                const string message = @"Для товаров с типом 'Услуга без сертификата' запрещена ручная установка источника,
так как товар не участвует в перемещениях. Автоматически будет установлен источником склад выдачи заказа при сохранении.";

                MessageFacadeService.ShowMessageBox(
                    message,
                    orderProduct.Product.NameFullRu,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (orderProduct.Source.Real)
            {
                MessageFacadeService.ShowNotificationWarning(SourceAlreadySetWarning);
                return;
            }

            IsLongOperationInProgress = true;

            try
            {
                QueryPurchaseMovementSources gatewayRequest = new QueryPurchaseMovementSources(
                    orderProduct.ProductId,
                    Id);

                List<PurchaseMovementSourceDto> sources = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                if (sources.Any(x => x.AvailableQuantity >= orderProduct.Quantity))
                {
                    SetSourceParameter setSourceParameter = new SetSourceParameter
                    {
                        ProductRecordId = orderProduct.Id,
                        CurrencyId = orderProduct.CurrencyOutId,
                        Price = orderProduct.PriceOut,
                        Quantity = orderProduct.Quantity,
                        ProductId = orderProduct.ProductId,
                        ProductName = orderProduct.Product?.Name,
                        OrderProductState = orderProduct.State,
                        OrderId = Id,
                        OrderState = State,
                        OrderWarehouseId = SelectedWarehouse?.Id,
                        OrderWarehouseName = SelectedWarehouse?.Name,
                        OrderDeliveryTime = DeliveryTime,
                        ProductInvoiceId = orderProduct.InvoiceId
                    };

                    SizeableDialogDocumentManagerService.ShowView<SetMovementSourceViewModel>(
                        new object[] { setSourceParameter, sources },
                        this);
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning(
                        $"Товара нигде нет в свободном остатке {orderProduct.Quantity.ToString(CultureInfo.InvariantCulture)} шт.");
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(exception.Args.Error.GetErrorMessage());
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private async Task PackAsync()
        {
            OrderPackInfoDto info = await GetOrderPackInfoAsync(order.Id);

            if (info == null)
            {
                return;
            }

            LockResponse<OrderDto> lockResponse = await TryLockAndNotifyAsync(order.Id);

            if (!lockResponse.Success)
            {
                await RefreshOrderAsync(lockResponse.Dto);
                ShowOrderIsAlreadyLockedMessage(lockResponse.Dto.EmployeeLock?.ShortName);
                return;
            }

            OrderPackViewModel orderPackModel = SizeableDialogDocumentManagerService.ShowView<OrderPackViewModel>(info, this);

            await TryUnlockAndNotifyAsync(order.Id);

            if (orderPackModel.IsOk)
            {
                Close();

                if (order.CarryId == CarryType.UklonId)
                {
                    DialogDocumentManagerService.ShowView<UklonOrderViewModel>(new UklonOrderParameter() { OrderId = order.Id }, this);
                }
            }
        }

        private async Task GiveAsync()
        {
            IsLongOperationInProgress = true;

            (bool success, OrderDto order) result = await OrderGiveHelper.GiveAsync(
                order.Id,
                SelectedContractor.Name,
                SelectedCity.Name,
                SelectedWarehouse.UseCells,
                this);

            Pko = result.order.Pko;
            RefreshSummaryItems();
            order = result.order;

            IsLongOperationInProgress = false;

            if (result.success)
            {
                Close();
            }
        }

        private void CreateOrderBill()
        {
            DialogDocumentManagerService.ShowView<CreateOrderBillViewModel>(new CreateOrderBillParameter(order.Id, order.PaymentId), this);
        }

        private async Task RefreshOrderBillsAsync()
        {
            List<OrderBillDto> result = await WebClient.ExecuteApiRequestAsync(new QueryOrderBills(new QueryOrderBills.QueryOrderBillsFilteringItem(order.Id)));
            OrderBills = result.Select(x => Mapper.Map<OrderBillViewItem>(x)).ToObservableCollection();
        }

        private async Task CancelOrderBillAsync()
        {
            (await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new CancelOrderBill(SelectedOrderBill.Id)), "отмене счета", "Счет успешно отменен", this, true, true, true, "Отмена счета"))
              .IfNotNull(x => Messenger.Send(new OrderBillMessage(x.Data, MessageType.Changed)));
        }

        private async Task RecalculateOrderBillAsync()
        {
            (await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new RecalculateOrderBill(SelectedOrderBill.Id)), "актуализации счета", "Счет успешно актуализирован", this, true, true, true, "Актуализация счета"))
              .IfNotNull(x => Messenger.Send(new OrderBillMessage(x.Data, MessageType.Changed)));
        }

        private async Task PrintBillAsync()
        {
            try
            {
                byte[] response = await WebClient.ExecuteApiRequestAsBytesAsync(new ExportOrderBill(SelectedOrderBill.Id));

                await FileHelper.OpenAsFileAsync(response, Constants.XlsFileExtension);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to print order bill");
                MessageFacadeService.ShowNotificationWarning("Ошибка при печати счета");
            }
        }

        private async Task PrintBillInvoiceAsync()
        {
            try
            {
                if (SaveFileDialogService.ShowDialog())
                {
                    byte[] response = await WebClient.ExecuteApiRequestAsBytesAsync(new ExportOrderBillInvoice(SelectedOrderBill.Id));

                    await FileHelper.OpenAsFileAsync(response, Constants.XlsFileExtension);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to print order bill invoice");
                MessageFacadeService.ShowNotificationWarning("Ошибка при печати расходной накладной");
            }
        }

        private async Task TryUnlockAndNotifyAsync(int orderId)
        {
            IsLongOperationInProgress = true;

            try
            {
                LockResponse<OrderDto> unlockResponse = await WebClient.ExecuteApiRequestAsync(new UnlockOrder(orderId));

                if (unlockResponse.Success)
                {
                    Messenger.Send(new OrderMessage(unlockResponse.Dto, MessageType.Changed));
                }
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при разблокировании заказа");
                Logger.LogError(exception, "Failed to unblock order");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private bool CanPack()
        {
            return !IsInDesignMode
                && !IsAddMode
                && LockerId == null
                && WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.Warehouse, Role.Packager, Role.Seller, Role.TechSupport)
                && IsOrderReadyToPack(order);
        }

        private bool CanGive()
        {
            return !IsInDesignMode
                && !IsAddMode
                && LockerId == null
                && WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.Warehouse, Role.Packager, Role.Seller, Role.TechSupport)
                && IsOrderReadyToGive(order);
        }

        private async Task<OrderPackInfoDto> GetOrderPackInfoAsync(int orderId)
        {
            IsLongOperationInProgress = true;

            OrderPackInfoDto info = null;

            try
            {
                Result<OrderPackInfoDto> result = await WebClient.ExecuteApiRequestAsync(new QueryOrderPackInfo(orderId));

                if (result.Warnings.Any())
                {
                    if (ShowValidationResultView("Предупреждения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray()))
                    {
                        info = result.Data;
                    }
                }
                else
                {
                    info = result.Data;
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
            finally
            {
                IsLongOperationInProgress = false;
            }

            return info;
        }

        private async Task<LockResponse<OrderDto>> TryLockAndNotifyAsync(int orderId, bool allowLockedByMe = true)
        {
            IsLongOperationInProgress = true;
            LockResponse<OrderDto> lockResponse = null;

            try
            {
                lockResponse = await WebClient.ExecuteApiRequestAsync(new LockOrder(orderId, allowLockedByMe));
                Messenger.Send(new OrderMessage(lockResponse.Dto, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while locking order");
                MessageFacadeService.ShowNotificationError("Ошибка при блокировании заказа");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }

            return lockResponse;
        }

        private void ShowOrderIsAlreadyLockedMessage(string employeeName)
        {
            MessageFacadeService.ShowNotificationError($"Заказ уже заблокирован пользователем {employeeName}");
        }

        private Task RefreshOrderAsync(OrderDto dto)
        {
            order = dto;

            return InitializeEditAsync(order.Id);
        }

        private Task CancelOrderAsync()
        {
            return LockableOperationProcessor.DoOperationAsync(order.Id, CancelOrderInternalAsync, false);
        }

        private async Task ReceiveOrderAsync()
        {
            if (!MessageFacadeService.Confirm($"Нельзя переводить в статус \"Принят\",{Environment.NewLine}если вы ожидаете оплату от покупателя. Вы действительно хотите перевести заказ в статус \"Принят\"?"))
            {
                return;
            }

            IsLongOperationInProgress = true;

            await LockableOperationProcessor.DoOperationAsync(order.Id, ReceiveOrderInternalAsync, false);

            OrderDto result = await WebClient.ExecuteApiRequestAsync(new QueryOrder(order.Id));

            await RefreshOrderAsync(result);

            IsLongOperationInProgress = false;

            return;

            async Task ReceiveOrderInternalAsync(OrderDto lockedOrder)
            {
                await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new ReceiveOrder(lockedOrder.Id)),
                    "переводе в статус \"Принят\"",
                    "Заказ переведен в статус \"Принят\"",
                    this,
                    true);
            }
        }

        private async Task CancelOrderInternalAsync(OrderDto lockedOrder)
        {
            int cancelReasonId = 0;

            OrderCancelInfoDto info = await GetOrderCancelInfoAsync(lockedOrder.Id);

            if (info is null)
            {
                return;
            }

            Task<List<EventDto>> queryEventsTask = WebClient.ExecuteApiRequestAsync(new QueryEvents(new EventFilteringItem()
            {
                EntityId = lockedOrder.Id,
                TypeId = EventType.UnpackAndCancelOrderId,
                Completed = false
            }));

            Task<PagedResult<TaskDto>> queryTasksTask = WebClient.ExecuteApiRequestAsync(new QueryTasks(new TaskFilteringItem()
            {
                States = new[] { TaskState.New.Id, TaskState.InProgress.Id },
                Types = new[] { TaskType.UnpackAndCancelOrder.Id },
                AllTasks = true
            }));

            Task<List<OrderStateChangeReasonDto>> stateChangeReasonTask = WebClient.ExecuteApiRequestAsync(new QueryOrderStateChangeReasons(false));

            await Task.WhenAll(queryEventsTask, queryTasksTask, stateChangeReasonTask);

            if (queryEventsTask.Result?.Any() == true)
            {
                EventDto eventDto = queryEventsTask.Result.First();

                JToken jToken = eventDto?.Documents?.GetValueOrDefault(EventConstants.OrderStateChangeReasonId);

                cancelReasonId = GetCancelReasonId(jToken);
            }

            if (queryTasksTask.Result?.Data.Any() == true)
            {
                TaskDto taskDto = queryTasksTask.Result.Data
                    .FirstOrDefault(x => x.Documents.GetValueOrDefault(EventConstants.OrderId)?.ToString() == lockedOrder.Id.ToString());

                JToken jToken = taskDto?.Documents?.GetValueOrDefault(EventConstants.OrderStateChangeReasonId);

                cancelReasonId = GetCancelReasonId(jToken);
            }

            int[] cellIds = null;

            if (order.StateId == OrderStatus.Packed.Id)
            {
                (bool Success, int[] CellIds) prepareUnpackResponse = await PrepareUnpackAsync();

                if (!prepareUnpackResponse.Success)
                {
                    return;
                }

                cellIds = prepareUnpackResponse.CellIds;
            }

            if (cancelReasonId > 0)
            {
                OrderStateChangeReasonDto changeReason = stateChangeReasonTask.Result.FirstOrDefault(x => x.Id == cancelReasonId);

                if (changeReason is not null)
                {
                    if (MessageFacadeService.Confirm($"Была инициирована отмена заказа с причиной '{changeReason.Name}'. Отменить сразу?"))
                    {
                        CancelOrder gatewayRequest = new CancelOrder(lockedOrder.Id, changeReason.Id, null, cellIds);

                        await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(gatewayRequest), "отмене заказа", "Заказ отменен", this, true);

                        Close();

                        return;
                    }
                }
            }

            info.CellIds = cellIds;

            OrderCancelViewModel viewModel = DialogDocumentManagerService.ShowView<OrderCancelViewModel>(info, this);

            if (viewModel.IsOk)
            {
                Close();
            }
        }

        private async Task<OrderCancelInfoDto> GetOrderCancelInfoAsync(int orderId)
        {
            IsLongOperationInProgress = true;

            OrderCancelInfoDto info = null;

            try
            {
                Result<OrderCancelInfoDto> result = await WebClient.ExecuteApiRequestAsync(new QueryOrderCancelInfo(orderId));

                if (result.Warnings.Count == 0 || ShowValidationResultView("Предупреждения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray()))
                {
                    info = result.Data;
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
            finally
            {
                IsLongOperationInProgress = false;
            }

            return info;
        }

        private void CalculateLogistics()
        {
            ValidationResultItem[] errors = CanCalculateLogistics().ToArray();

            if (errors.Any())
            {
                ShowValidationResultView("Ошибки при расчете логистики", errors);
            }
            else
            {
                OrderProductViewItem[] orderProducts = GetOrderProducts().Select(x => new OrderProductViewItem
                {
                    Id = x.Id,
                    ParentRecordId = x.ParentRecordId,
                    IsGift = x.IsGift,
                    IsAdditionalService = x.IsAdditionalService,
                    IsAdditionalServiceConsumable = x.IsAdditionalServiceConsumable,
                    ShowAdditionalServiceIcon = x.IsAdditionalService && ProductType.IsAdditionalServiceProductType(x.TypeId),
                    ShowAccessoryAdditionalServiceIcon = x.IsAdditionalService && ProductType.IsAccessoryAdditionalServiceProductType(x.TypeId),
                    State = x.State,
                    Source = x.Source,
                    Price = x.Price,
                    CurrencyId = x.CurrencyId,
                    PriceOut = x.PriceOut,
                    CurrencyOutId = x.CurrencyOutId,
                    Quantity = x.Quantity,
                    ProductId = x.Product.Id,
                    ProductName = x.Product.Name,
                    ProductPrefixRus = x.Product.PrefixRus,
                    ProductNameUkr = x.Product.PrefixUkr,
                    ProductPrefixEn = x.Product.PrefixEn,
                    DeliveryDateTime = null,
                    OrderWarehouseId = SelectedWarehouse?.Id,
                    OrderConfirmedBy = ConfirmedBy,
                    OrderFolderId = x.OrderFolderId,
                    AssemblyQuantity = x.AssemblyQuantity,
                    AssemblyIncluded = x.AssemblyIncluded,
                    FreeDelivery = x.FreeDelivery
                }).ToArray();

                OrderLogisticsParameter parameter = new OrderLogisticsParameter(
                    Id,
                    SelectedSubdivision.Id,
                    SelectedPayment.Id,
                    SelectedCarryType.Id,
                    SelectedCity.Id,
                    SelectedWarehouse.Id,
                    AssemblyWarehouse?.Id,
                    BufferWarehouseId,
                    SelectedAdditionalServiceWarehouse?.Id,
                    State.Id,
                    FreeDelivery,
                    order.IgnoreDeliveryCostCalculation,
                    OrderPayments.ToArray(),
                    orderProducts,
                    GetBonusesQuantity(),
                    GetBonusesToChargeQuantity(),
                    order.PackageDeliveryCost,
                    order.Folders,
                    order.MoneyBackAmount);

                SizeableDialogDocumentManagerService.ShowView<OrderLogisticsViewModel>(parameter, this);
            }

            IEnumerable<ValidationResultItem> CanCalculateLogistics()
            {
                if (SelectedSubdivision == null)
                {
                    yield return new ValidationResultItem("Поле \"Клиент\" не заполнено", true);
                }

                if (SelectedPayment == null)
                {
                    yield return new ValidationResultItem("Поле \"Оплата\" не заполнено", true);
                }

                if (SelectedCarryType == null)
                {
                    yield return new ValidationResultItem("Поле \"Доставка\" не заполнено", true);
                }

                if (SelectedCity == null)
                {
                    yield return new ValidationResultItem("Поле \"Город\" не заполнено", true);
                }

                if (SelectedWarehouse == null)
                {
                    yield return new ValidationResultItem("Поле \"Склад\" не заполнено", true);
                }

                if (IDataErrorInfoHelper.HasErrors(this, true, propertyFilter: x => string.Equals(nameof(AssemblyWarehouse), x.Name)))
                {
                    yield return new ValidationResultItem("Поле \"Склад сборки\" не валидно", true);
                }

                if (IDataErrorInfoHelper.HasErrors(this, true, propertyFilter: x => string.Equals(nameof(SelectedAdditionalServiceWarehouse), x.Name)))
                {
                    yield return new ValidationResultItem("Поле \"Склад услуг\" не валидно", true);
                }

                if (!GetOrderProducts().Any())
                {
                    yield return new ValidationResultItem("В заказе нет ни одного товара", true);
                }

                if (BufferWarehouseRequired && BufferWarehouseId == null)
                {
                    Dictionary<int, OrderProductViewModel[]> orderProductsFolders = OrderProducts
                        .Where(x => x.OrderFolderId.HasValue)
                        .GroupBy(x => x.OrderFolderId.Value)
                        .ToDictionary(x => x.Key, y => y.ToArray());

                    foreach (KeyValuePair<int, OrderProductViewModel[]> orderProductsFolder in orderProductsFolders)
                    {
                        OrderProductViewModel[] orderProducts = orderProductsFolder.Value;

                        if (orderProducts.Any(x => x.AssemblyIncluded && x.Product.TypeId != ProductType.AssemblyServiceId)
                            && orderProducts.Any(x => x.Product.TypeId == ProductType.AssemblyServiceId))
                        {
                            yield return new ValidationResultItem("В заказе не заполнен буферный склад.", true);
                        }
                    }
                }
            }
        }

        private Task ConfirmOrderAsync()
        {
            return LockableOperationProcessor.DoActionAsync(Id, ConfirmOrderInternal, false);

            void ConfirmOrderInternal(OrderDto actualOrderObj)
            {
                if (IDataErrorInfoHelper.HasErrors(this, true, propertyFilter: x => string.Equals(nameof(AssemblyWarehouse), x.Name)))
                {
                    MessageFacadeService.ShowNotificationWarning("Поле \"Склад сборки\" не валидно");
                    return;
                }

                if (IDataErrorInfoHelper.HasErrors(this, true, propertyFilter: x => string.Equals(nameof(SelectedWarehouse), x.Name)))
                {
                    MessageFacadeService.ShowNotificationWarning("Поле \"Склад\" не валидно");
                    return;
                }

                if (IDataErrorInfoHelper.HasErrors(this, true, propertyFilter: x => string.Equals(nameof(SelectedAdditionalServiceWarehouse), x.Name)))
                {
                    MessageFacadeService.ShowNotificationWarning("Поле \"Склад услуг\" не валидно");
                    return;
                }


                var orderProductWithAdditionalServicesIds = OrderProducts
                    .Select(x => x.ParentRecordId)
                    .OfType<int>()
                    .Distinct();

                if (OrderProducts.Any(x => !x.IsAdditionalServiceConsumable && x.IsGuestProduct && !orderProductWithAdditionalServicesIds.Contains(x.Id)))
                {
                    MessageFacadeService.ShowNotificationWarning("Запрещено согласовывать заказ с гостевыми товарами без услуг");
                    return;
                }

                if (BufferWarehouseRequired && BufferWarehouseId == null)
                {
                    Dictionary<int, OrderProductViewModel[]> orderProductsFolders = OrderProducts
                        .Where(x => x.OrderFolderId.HasValue)
                        .GroupBy(x => x.OrderFolderId.Value)
                        .ToDictionary(x => x.Key, y => y.ToArray());

                    foreach (KeyValuePair<int, OrderProductViewModel[]> orderProductsFolder in orderProductsFolders)
                    {
                        OrderProductViewModel[] orderProducts = orderProductsFolder.Value;

                        if (orderProducts.Any(x => x.AssemblyIncluded && x.Product.TypeId != ProductType.AssemblyServiceId)
                            && orderProducts.Any(x => x.Product.TypeId == ProductType.AssemblyServiceId))
                        {
                            MessageFacadeService.ShowNotificationWarning("В заказе не заполнен буферный склад.");
                            return;
                        }
                    }
                }

                OrderConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<OrderConfirmViewModel>(actualOrderObj, this);

                if (viewModel.IsOk)
                {
                    Close();
                }
            }
        }

        private async Task EditExternalPaymentAsync()
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Номер договора",
                $"Эквайринг ({SelectedExternalPayment.Payment.Name})");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (!fromUserViewModel.IsOk)
            {
                return;
            }

            UpdateExternalPaymentDto updateExternalPaymentDto = new UpdateExternalPaymentDto()
            {
                ExternalOrderId = fromUserViewModel.Content
            };

            await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new UpdateExternalPayment(SelectedExternalPayment.Id, updateExternalPaymentDto)),
                "изменении номера договора",
                "Номер договора изменен",
                this,
                true);

            await RefreshExternalPaymentsAsync();
        }

        private async Task CancelExternalPaymentAsync()
        {
            if (!MessageFacadeService.Confirm("Вы точно уверены что хотите отменить эквайринг?"))
            {
                return;
            }

            await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CancelExternalPayment(SelectedExternalPayment!.Id)),
                "отмене эквайринга",
                "Эквайринг отменен",
                this,
                true);

            await RefreshExternalPaymentsAsync();
        }

        private async Task QueryExternalPaymentStateAsync()
        {
            Result<ContractorPaymentStateDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryExternalPaymentState(SelectedExternalPayment!.Id)),
                "запросе статусе",
                null,
                this,
                true);

            if (result?.IsSuccess == true)
            {
                PaymentState state = Dictionaries.GetItemById<PaymentState>(result.Data.PaymentStateId);

                string message = $"Статус: {state.Name}{Environment.NewLine}Инфо: {result.Data.Description}";

                MessageFacadeService.ShowMessageBox(
                    message,
                    "Статус оплаты",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private async Task ReconfirmOrderAsync()
        {
            await LockableOperationProcessor.DoActionAsync(order.Id, ReconfirmOrderInternal, false);

            Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));

            void ReconfirmOrderInternal(OrderDto actualOrderObj)
            {
                DialogDocumentManagerService.ShowView<OrderReconfirmViewModel>(new OrderReconfirmParameter(actualOrderObj.Id), this);
            }
        }

        private bool CanCancelOrder()
        {
            ExternalPaymentViewItem[] privatPartialPayExternalPayments = ExternalPayments?
                .Where(x => x.Payment.Id == Payment.PrivatPartialPayId)
                .ToArray();

            return !IsInDesignMode
                && !IsAddMode
                && LockerId == null
                && Rt != 1
                && order != null
                && ((State == OrderStatus.Received || State == OrderStatus.Confirmed)
                    || (State == OrderStatus.Packed
                        && SelectedCarryType?.Id == CarryType.PickupId
                        && order?.WarehouseId.HasValue == true
                        && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(order.WarehouseId.Value))
                    || (State == OrderStatus.New
                        && privatPartialPayExternalPayments?.Any() == true
                        && privatPartialPayExternalPayments.All(x => x.PaymentStateId == PaymentState.Error.Id || x.PaymentStateId == PaymentState.Cancelled.Id)))
                && (!order.ExternalPayments.Any() || order.ExternalPayments.All(x => x.PaymentStateId != PaymentState.Confirmed.Id));
        }

        private bool CanConfirmOrder()
        {
            bool canConfirmBaseCheck = !IsInDesignMode
                                       && !IsAddMode
                                       && LockerId == null
                                       && State == OrderStatus.Received;

            bool canConfirmBasedOnExternalPayments = true;

            if (ExternalPayments != null)
            {
                foreach (ExternalPaymentViewItem externalPayment in ExternalPayments)
                {
                    if (!PaymentState.IsActual(externalPayment.PaymentStateId))
                    {
                        continue;
                    }

                    canConfirmBasedOnExternalPayments = (SelectedPayment?.Id == Payment.CreditId
                                                         && externalPayment.PaymentStateId == PaymentState.Completed.Id)
                                                        || (SelectedPayment?.Id == Payment.PumbId
                                                         && externalPayment.PaymentStateId == PaymentState.Completed.Id)
                                                        || (SelectedPayment?.Id == Payment.ABankId
                                                            && externalPayment.PaymentStateId == PaymentState.Completed.Id)
                                                        || (SelectedPayment?.Id == Payment.PrivatPartialPayId
                                                            && externalPayment.PaymentStateId == PaymentState.Completed.Id)
                                                        || (SelectedPayment?.Id == Payment.MonobankId
                                                            && externalPayment.PaymentStateId is PaymentState.AfterConfirmedId or PaymentState.CompletedId)
                                                        || (externalPayment.Payment.Id == Payment.PaylaterId
                                                            && externalPayment.PaymentStateId == PaymentState.Completed.Id)
                                                        || (SelectedPayment?.Id == Payment.CashId
                                                            && externalPayment.PaymentStateId == PaymentState.Error.Id)
                                                        || (SelectedPayment?.Id == Payment.CashId
                                                            && externalPayment.Payment.Id == Payment.MonobankId
                                                            && externalPayment.PaymentStateId is PaymentState.AfterConfirmedId or PaymentState.CompletedId)
                                                        || (SelectedPayment?.Id == Payment.CashId
                                                            && externalPayment.Payment.Id is Payment.CreditId or Payment.PrivatPartialPayId or Payment.PumbId or Payment.ABankId or Payment.LiqPayId
                                                            && externalPayment.PaymentStateId == PaymentState.Completed.Id)
                                                        || (externalPayment.PaymentStateId == PaymentState.Payed.Id
                                                            && externalPayment.Payment.Id != Payment.CreditId
                                                            && externalPayment.Payment.Id != Payment.MonobankId
                                                            && externalPayment.Payment.Id != Payment.PrivatPartialPayId)
                                                        || (externalPayment.PaymentStateId == PaymentState.Created.Id
                                                            && SelectedPayment?.Id != Payment.CreditId
                                                            && SelectedPayment?.Id != Payment.MonobankId
                                                            && SelectedPayment?.Id != Payment.PrivatPartialPayId)
                                                        || (externalPayment.PaymentStateId == PaymentState.Cancelled.Id
                                                            && SelectedPayment?.Id is Payment.CashId or Payment.BankId or Payment.CashlessNoTaxId or Payment.CashlessTaxId);

                    if (canConfirmBasedOnExternalPayments == false)
                    {
                        break;
                    }
                }
            }


            return canConfirmBaseCheck && canConfirmBasedOnExternalPayments;
        }

        private bool CanReconfirmOrder()
        {
            return !IsInDesignMode
                   && !IsAddMode
                   && LockerId == null
                   && State == OrderStatus.Confirmed;
        }

        private bool CanPaymentControlOrder()
        {
            return !IsInDesignMode
                   && !IsAddMode
                   && LockerId == null
                   && State == OrderStatus.Received
                   && ExternalPayments.Any(x => x.PaymentStateId == PaymentState.Confirmed.Id && x.HoldedAmount > 0);
        }

        private bool CanEditBonuses()
        {
            bool baseCanEditBonuses = !IsInDesignMode
                                      && !IsAddMode
                                      && LockerId == null
                                      && (State == OrderStatus.Received
                                          || State == OrderStatus.New
                                          || (State == OrderStatus.Packed
                                              && order.PaymentId == Payment.CashId
                                              && order.CarryId == CarryType.PickupId))
                                      && order.PaymentId != Payment.CashlessTaxId;

            bool canEditBonusesBasedOnExternalPayments = true;

            foreach (ExternalPaymentViewItem externalPayment in ExternalPayments)
            {
                canEditBonusesBasedOnExternalPayments = Payment.IsEditingAllowed(externalPayment.Payment.Id, externalPayment.PaymentStateId, externalPayment.Payment.Credit, externalPayment.Payment.PartialCredit);

                if (canEditBonusesBasedOnExternalPayments == false)
                {
                    break;
                }
            }

            return baseCanEditBonuses && canEditBonusesBasedOnExternalPayments;
        }

        private bool CanPrintOrderPaymentOnFiscalRegistrar(OrderPaymentRecordViewItem item)
        {
            return item != null
                && item.CompletedOnFiscalRegistrar == false
                && item.Sign > 0;
        }

        private async Task UnpackAsync()
        {
            IsLongOperationInProgress = true;

            try
            {
                // those two checks are also present on server
                if (Rt == 1)
                {
                    MessageFacadeService.ShowNotificationError("Сначала отмените проведение заказа в 1С");
                    return;
                }

                (bool Success, int[] cellIds) prepareUnpackResult = await PrepareUnpackAsync();

                if (!prepareUnpackResult.Success)
                {
                    return;
                }

                LockResponse<OrderDto> lockResponse = await TryLockAndNotifyAsync(order.Id);

                if (!lockResponse.Success)
                {
                    await RefreshOrderAsync(lockResponse.Dto);
                    ShowOrderIsAlreadyLockedMessage(lockResponse.Dto.EmployeeLock?.ShortName);
                    return;
                }

                var vm = DialogDocumentManagerService.ShowView<OrderUnpackViewModel>(new OrderUnpackParameter(order.Id, prepareUnpackResult.cellIds), this);

                if (vm.IsOk)
                {
                    Close();
                }
            }
            finally
            {
                await TryUnlockAndNotifyAsync(order.Id);
                IsLongOperationInProgress = false;
            }
        }

        private bool CanUnpack()
        {
            return !IsInDesignMode
                && !IsAddMode
                && LockerId == null
                && State == OrderStatus.Packed
                && WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.Warehouse, Role.Seller, Role.TechSupport);
        }

        private bool ShowValidationResultView(string title, IEnumerable<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }

        private async Task<(bool Success, CheckPromoCodesResponse Result)> ProcessPromoCodesAsync(string[] promoCodes, int[] promoCodeIds, IEnumerable<OrderProductViewModel> orderProducts = null, bool skipConfirmation = false, bool bundle = false)
        {
            int[] removedPromoCodeIds = Array.Empty<int>();

            removedPromoCodeIds = PromoCodes
                .Where(x => promoCodes?.Contains(x.Value) == false && promoCodeIds?.Contains(x.PromoCodeId) == false)
                .Select(x => x.PromoCodeId)
                .ToArray();

            int[] exceptProductIds = GetOrderProducts()
                .Where(x => Bonuses?.Any(y => y.OrderProductId == x.Id) == true)
                .Select(x => x.Product.Id)
                .Distinct()
                .ToArray();

            OrderProductViewModel[] productsToCheck =
                (orderProducts?.Any() == true
                    ? orderProducts.Where(x => !x.IsVirtualProduct)
                    : GetOrderProducts()
                        .Where(x => !exceptProductIds.Contains(x.Product.Id)))
                .ToArray();

            CheckPromoCodeProductRequest[] promoProducts = productsToCheck
                .Select(x => new CheckPromoCodeProductRequest(x.Id, x.Product.Id, x.Quantity, x.Price))
                .ToArray();

            if (bundle)
            {
                if (promoCodeIds?.Length != 1)
                {
                    MessageFacadeService.ShowNotificationError("Можна обрати тільки 1 бандл");
                    return (false, null);
                }

                promoProducts.ForEach(x => x.BundleId = promoCodeIds[0]);
            }

            CheckPromoCodesRequest request = promoCodeIds?.Any(x => x > 0) == true
                ? new CheckPromoCodesRequest(
                    promoCodeIds,
                    promoProducts,
                    productsToCheck.Select(x => x.CreatedOn).Min())
                : new CheckPromoCodesRequest(
                    promoCodes,
                    promoProducts,
                    productsToCheck.Select(x => x.CreatedOn).Min());

            CheckPromoCodesResponse response = await CheckPromoCodesAsync(request);

            if (response is null)
            {
                return (false, null);
            }

            List<ValidationResultItem> errors = response.PromoCodes
                .Where(x => !string.IsNullOrWhiteSpace(x.Error))
                .Select(x => new ValidationResultItem(x.Error, true))
                .ToList();

            if (errors.Any())
            {
                ShowValidationResultView("Ошибки при добавлении промо-кода", errors);
                return (false, null);
            }

            List<(OrderProductViewModel OrderProduct, int Position)> giftOrderProducts = new List<(OrderProductViewModel OrderProduct, int Position)>();

            Dictionary<int, int> giftProductsWithOtherPromoCodeOrderProductIds = response.Products
                .Where(x => x.PromoCodeId.HasValue)
                .GroupBy(x => x.PromoCodeId.Value)
                .Where(x => x.Any(z => z.IsGift) && x.Any(z => !z.IsGift))
                .SelectMany(
                    x =>
                        x.Where(z => z.IsGift)
                            .Select(
                                giftProduct => new
                                {
                                    GiftProductId = giftProduct.ProductId,
                                    ParentRecordId = x.First(product => !product.IsGift).Id
                                }))
                .DistinctBy(x => x.GiftProductId)
                .ToDictionary(x => x.GiftProductId, x => x.ParentRecordId);

            int[] giftProductIds = response.Products
                .Where(x => x.IsGift)
                .Select(x => x.ProductId)
                .ToArray();

            if (giftProductIds.Any())
            {
                List<ProductDto> giftProducts = await WebClient.ExecuteCatalogApiRequestAsync(
                    new QueryProductByIds(Constants.TelemartContractorId, giftProductIds));

                foreach (CheckPromoCodeProductResponse giftProduct in response.Products.Where(x => x.IsGift))
                {
                    if (!giftProductsWithOtherPromoCodeOrderProductIds.TryGetValue(
                            giftProduct.ProductId,
                            out int orderProductId))
                    {
                        continue;
                    }

                    ProductDto giftProductDto = giftProducts.First(x => x.Id == giftProduct.ProductId);

                    ProductAdditionalServiceDto[] availAdditionalServices = TreeStructureHelper
                        .Deconstruct(giftProductDto.AdditionalServiceGroups)
                        .SelectMany(x => x.AdditionalServices)
                        .ToArray();

                    ProductAdditionalServiceDto additionalService =
                        availAdditionalServices.FirstOrDefault(x => x.ProductId == giftProduct.Id);

                    OrderProductViewModel parentOrderProduct = GetOrderProducts().First(x => x.Id == orderProductId);

                    OrderProductViewModel orderGiftProductViewModel =
                        new OrderProductViewModel(Dictionaries, MessageFacadeService, OrderRules)
                        {
                            Id = Random.Shared.GetRandomId(),
                            OrderFolderId = parentOrderProduct.OrderFolder?.Id,
                            OrderFolder = parentOrderProduct.OrderFolder is null ? null : ReflectionObjectCloner.Clone(parentOrderProduct.OrderFolder),
                            ParentRecordId = parentOrderProduct.Id,
                            AssemblyQuantity = IsAssemblyOrAssembledComputerRuleFolder(parentOrderProduct.OrderFolder) ? 1 : null,
                            Quantity = giftProduct.Quantity,
                            PriceOut = 1,
                            CurrencyOutId = Currency.UahId,
                            Price = 1,
                            PriceId = SelectedContractor.PriceTypeId,
                            PriceIdOld = SelectedContractor.PriceTypeId,
                            CurrencyId = Currency.UahId,
                            IsGift = true,
                            Price1C = 0m,
                            Product = new ProductSimpleDto
                            {
                                Id = giftProductDto.Id,
                                Name = giftProductDto.Name,
                                Weight = giftProductDto.Weight ?? 0,
                                NameFullRu = giftProductDto.NameFullRu,
                                NameFullUkr = giftProductDto.NameFullUa,
                                TypeId = giftProductDto.TypeId,
                                BonusesToCharge = null,
                                ParentCategoryId = giftProductDto.ParentCategoryId
                            },
                            Source = new NoneOrderProductSource(),
                            State = OrderProductStatus.New,
                            OrderPromoCodeId = giftProduct.PromoCodeId,
                            WarrantyId = giftProductDto.WarrantyId,
                            MaxBonusesToUse = giftProductDto.MaxBonusesToUse,
                            BonusesCharged = false,
                            AdditionalServiceId = additionalService?.Id,
                            AdditionalServicePercent = additionalService?.Percent,
                            AdditionalServiceMinPrice = additionalService?.MinPrice,
                            IsAdditionalService = additionalService != null,
                            ShowAdditionalServiceIcon = additionalService != null && ProductType.IsAdditionalServiceProductType(additionalService.ProductTypeId),
                            ShowAccessoryAdditionalServiceIcon = additionalService != null && ProductType.IsAccessoryAdditionalServiceProductType(additionalService.ProductTypeId),
                            TypeId = giftProductDto.TypeId,
                            FreeDelivery = giftProductDto.FreeDelivery
                        };

                    int parentOrderPosition = OrderProducts.IndexOf(parentOrderProduct);

                    giftOrderProducts.Add((orderGiftProductViewModel, parentOrderPosition));
                }
            }

            List<PromoCodeProductViewItem> productChanges = (from p in response.Products
                                                             let op = p.IsGift ? giftOrderProducts.First(x => x.OrderProduct.ProductId == p.ProductId).OrderProduct : GetOrderProducts().First(x => x.Id == p.Id)
                                                             where !op.IsGift || op.OrderPromoCodeId is null || !removedPromoCodeIds.Contains(op.OrderPromoCodeId.Value)
                                                             select new PromoCodeProductViewItem
                                                             {
                                                                 Id = p.Id,
                                                                 ProductId = p.ProductId,
                                                                 Name = op.Product.NameFullRu,
                                                                 NameUkr = op.Product.NameFullUkr,
                                                                 PromoCodeId = p.PromoCodeId,
                                                                 PromoCode = response.PromoCodes.FirstOrDefault(x => x.PromoCodeId == p.PromoCodeId)?.PromoCodeValue,
                                                                 Price = p.IsGift ? null : op.Price,
                                                                 CurrencyId = op.CurrencyId,
                                                                 PriceCurrent = p.IsGift ? null : op.PriceOut,
                                                                 IsGift = p.IsGift,
                                                                 CurrencyCurrentId = op.CurrencyOutId,
                                                                 PriceNew = op.OrderPromoCodeId == null && p.PromoCodeId == null ? op.PriceOut : p.PriceNew ?? op.PriceOut,
                                                                 BonusesToChargeCurrent = op.BonusesToCharge,
                                                                 BonusesToChargeNew = (p.BonusesToCharge ?? 0) >= (op.BonusesToCharge ?? 0) || !p.BonusesToCharge.HasValue ? (int?)p.BonusesToCharge : op.BonusesToCharge,
                                                                 CurrencyNewId = op.OrderPromoCodeId == null && p.PromoCodeId == null ? op.CurrencyOutId : op.CurrencyId
                                                             }).ToList();

            if (productChanges.Any())
            {
                // TODO: Recalculate bonuses on products without promo (case when promo was deleted from order)

                PromoCodeConfirmationViewModel confirmationViewModel = null;

                if (!skipConfirmation)
                {
                    confirmationViewModel = SizeableDialogDocumentManagerService.ShowView<PromoCodeConfirmationViewModel>(productChanges, this);
                }

                if (skipConfirmation || confirmationViewModel.IsOk)
                {
                    // Processing for non gifts

                    foreach (PromoCodeProductViewItem item in productChanges.Where(x => !x.IsGift))
                    {
                        OrderProductViewModel orderProduct = GetOrderProducts().First(x => x.Id == item.Id);

                        decimal priceDelta = orderProduct.PriceOut - item.PriceNew;

                        orderProduct.PromoDiscount = priceDelta > 0 ? priceDelta : 0;

                        orderProduct.PriceOut = item.PriceNew;
                        orderProduct.CurrencyOutId = item.CurrencyNewId;
                        orderProduct.OrderPromoCodeId = item.PromoCodeId;
                        orderProduct.BonusesToCharge = item.BonusesToChargeNew;
                    }

                    // Processing for new gifts

                    foreach ((OrderProductViewModel OrderProduct, int Position) giftOrderProduct in giftOrderProducts)
                    {
                        OrderProducts.Insert(giftOrderProduct.Position + 1, giftOrderProduct.OrderProduct);
                    }

                    // delete gifts from deleted promocode

                    OrderProductViewModel[] giftsToDelete = OrderProducts.Where(x => x.OrderPromoCodeId.HasValue && removedPromoCodeIds.Contains(x.OrderPromoCodeId.Value) && x.IsGift).ToArray();

                    foreach (OrderProductViewModel giftToDelete in giftsToDelete)
                    {
                        OrderProducts.Remove(giftToDelete);
                    }
                }
                else
                {
                    return (false, null);
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationInfo("Нет изменений в товарах");
            }

            return (true, response);
        }

        private void ResetProductPrices()
        {
            if (Bonuses?.Any() == true)
            {
                MessageFacadeService.ShowNotificationError("К заказу уже применены бонусы");
                return;
            }

            if (PromoCodes?.Any() == true)
            {
                MessageFacadeService.ShowNotificationError("В заказе присутствуют промокоды");
                return;
            }

            OrderProductViewModel[] orderProductsToResetPrice = OrderProducts
                .Where(x => !x.IsGift
                    && !x.IsVirtualProduct
                    && (x.OrderFolder is null || x.OrderFolder.TypeId != OrderFolderType.AssembledComputerRuleId))
                .ToArray();

            if (!orderProductsToResetPrice.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нет товаров, которым можно вернуть цену к исходной");
                return;
            }

            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            foreach (OrderProductViewModel orderProduct in orderProductsToResetPrice.OrderBy(x => x.IsAdditionalService))
            {
                if (orderProduct.IsAdditionalService)
                {
                    OrderProductViewModel parentOrderProduct = OrderProducts.First(x => x.Id == orderProduct.Id);

                    orderProduct.PriceOut = PriceHelper.CalculatePriceForAdditionalService(parentOrderProduct.PriceOut, orderProduct.AdditionalServicePercent!.Value, orderProduct.AdditionalServiceMinPrice!.Value);
                }
                else
                {
                    orderProduct.PriceOut = orderProduct.Price;
                }
            }
        }

        private void SetProductPrices()
        {
            if (Bonuses?.Any() == true)
            {
                MessageFacadeService.ShowNotificationError("К заказу уже применены бонусы.");
                return;
            }

            if (PromoCodes?.Any() == true)
            {
                MessageFacadeService.ShowNotificationError("К заказу уже применены промокоды.");
                return;
            }

            int orderProductsCount = OrderProducts
                .Count(x => !x.IsGift
                            && !x.IsAdditionalService
                            && !x.IsVirtualProduct
                            && (x.OrderFolder is null ||
                                x.OrderFolder.TypeId != OrderFolderType.AssembledComputerRuleId));

            if (orderProductsCount == 0)
            {
                MessageFacadeService.ShowNotificationWarning("Нет товаров, у которых можно изменить тип цены");
                return;
            }

            SetOrderProductPriceViewModel viewModel = DialogDocumentManagerService
                .ShowView<SetOrderProductPriceViewModel>(new SetOrderProductPriceParameter(OrderProducts.ToReadOnlyObservableCollection(), SelectedContractor), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            foreach (SetOrderProductPriceViewItem productPriceViewItem in viewModel.Products)
            {
                OrderProductViewModel orderProduct = OrderProducts.First(x => x.Id == productPriceViewItem.OrderProductId);

                orderProduct.PriceOut = productPriceViewItem.PriceNew;
                orderProduct.PriceId = productPriceViewItem.PriceId;
                orderProduct.PriceIdOld = productPriceViewItem.PriceId;
                orderProduct.MaxBonusesToUse = productPriceViewItem.MaxBonusesToUse;
            }

            foreach (OrderProductViewModel additionalServiceOrderProduct in OrderProducts.Where(x => x.IsAdditionalService))
            {
                OrderProductViewModel parentOrderProduct = OrderProducts.First(x => x.Id == additionalServiceOrderProduct.ParentRecordId);

                additionalServiceOrderProduct.PriceOut = PriceHelper.CalculatePriceForAdditionalService(
                    parentOrderProduct.PriceOut,
                    additionalServiceOrderProduct.AdditionalServicePercent!.Value,
                    additionalServiceOrderProduct.AdditionalServiceMinPrice!.Value);
            }
        }

        private async Task<CheckPromoCodesResponse> CheckPromoCodesAsync(CheckPromoCodesRequest request)
        {
            CheckPromoCodesResponse response = null;

            ShowLoadingIndicator = true;

            try
            {
                response = await WebClient.ExecuteApiRequestAsync(new CheckPromoCodes(request));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(order.Id));
                ShowValidationResultView("Ошибки при добавлении промо-кода", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError(GetErrorMessage(order.Id));
                ShowValidationResultView("Ошибки при добавлении промо-кода", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to add promo code");
                MessageFacadeService.ShowNotificationError("Ошибка при добавлении промо-кода");
            }
            finally
            {
                ShowLoadingIndicator = false;
            }

            return response;
        }

        private async Task ShowGuestProductAsync(OrderProductViewModel orderProduct, bool info)
        {
            bool allowChange = IsLockedByCurrentUserAndEditingAllowed;

            if (!IsLockedByCurrentUserAndEditingAllowed && !info)
            {
                LockResponse<OrderDto> lockResponse = await TryLockAndNotifyAsync(order.Id);

                allowChange = lockResponse?.Success == true;
            }

            bool firstTime = false;

            try
            {
                List<(int OrderProductId, GuestProductDto GuestProduct)> guestProducts = new();

                guestProducts.AddRange(
                    AdditionalServiceProducts.Where(x => x.ParentOrderProductId.HasValue)
                        .Select(x => (x.ParentOrderProductId!.Value, x.GuestProduct)));

                guestProducts.AddRange(
                    AdditionalServiceProducts.SelectMany(x => x.ConsumableProducts.Where(y => y.OrderProductId.HasValue))
                        .Select(x => (x.OrderProductId!.Value, x.GuestProduct)));

                var guestProductData = guestProducts.FirstOrDefault(x => x.OrderProductId == orderProduct.Id && x.GuestProduct != null);

                if (allowChange && guestProductData.GuestProduct?.KeepProduct == true && _isCheckReceiveAct > 0)
                {
                    List<OrderDocumentDto> orderDocuments = await WebClient.ExecuteApiRequestAsync(new QueryDocumentsByOrders(new[] { order.Id }, (int)OrderDocumentTypeIds.ActIncomeId));

                    if (orderDocuments?.Any() != true)
                    {
                        MessageFacadeService.ShowMessageBoxWarning(
                            AllGuestProductsHasCharacteristics()
                                ? "Не прикреплен Акт принятия гостевых товаров"
                                : "Не заповнены характеристики у всех гостьовых товаров");

                        return;
                    }
                }

                AdditionalServiceProductClientProductViewModel model =
                    DialogDocumentManagerService.ShowView<AdditionalServiceProductClientProductViewModel>(
                        new AdditionalServiceProductClientProductParameter(
                            guestProductData.GuestProduct?.Product,
                            guestProductData.GuestProduct?.SerialNumber,
                            guestProductData.GuestProduct?.Description,
                            allowChange && orderProduct.Source?.Id == OrderProductSourceType.Guest.Id && order?.Id > 0,
                            SelectedAdditionalServiceWarehouse?.Id,
                            guestProductData.GuestProduct?.KeepProduct ?? false),
                        this);

                if (model.IsOk)
                {
                    firstTime = guestProductData.GuestProduct == null;

                    ReceiveGuestProductDto receiveGuestProductDto = new ReceiveGuestProductDto
                    {
                        Id = guestProductData.GuestProduct?.Id ?? 0,
                        OrderId = order!.Id,
                        OrderProductId = orderProduct.Id,
                        AdditionalServiceWarehouseId = model.AdditionalServiceWarehouseId!.Value,
                        Product = model.Product,
                        SerialNumber = model.SerialNumber,
                        Description = model.Description,
                        KeepProduct = model.KeepGuestProduct,
                        LastGuestProduct = IsLastGuestProduct(orderProduct.Id)
                    };

                    Result<OrderDto> result = await ErrorHandler.HandleErrorsAsync(
                        _ => WebClient.ExecuteApiRequestAsync(new ReceiveGuestProduct(receiveGuestProductDto)),
                        "сохранении характеристик гостевого товара",
                        "Характеристики гостевого товара сохранены",
                        this,
                        true);

                    if (result?.IsSuccess == true)
                    {
                        guestProductData.GuestProduct ??= new GuestProductDto();
                        guestProductData.GuestProduct.SerialNumber = model.SerialNumber;
                        guestProductData.GuestProduct.Description = model.Description;
                        guestProductData.GuestProduct.KeepProduct = model.KeepGuestProduct;
                        guestProductData.GuestProduct.Product = model.Product;

                        SetGuestProductToAdditionalServiceProduct(orderProduct.Id, guestProductData.GuestProduct);

                        OrderProductDto orderProductDto = result.Data.Products.First(x => x.Id == orderProduct.Id);

                        orderProduct.Source = Dictionaries.GetOrderProductSource(
                            orderProductDto.SourceId,
                            orderProductDto.WarehouseId,
                            orderProductDto.SourceText,
                            orderProductDto.SourceDate);
                    }
                }
            }
            finally
            {
                if (!IsLockedByCurrentUserAndEditingAllowed && !info)
                {
                    await TryUnlockAndNotifyAsync(order!.Id);
                }
            }

            if (_isCheckReceiveAct > 0 && allowChange && firstTime && AllGuestProductsHasCharacteristics() && HasKeepGuestProducts())
            {
                await PrintActIncomeCommand.ExecuteAsync(false);

                ShowDocumentsBotQrCommand.Execute(null);
            }
        }

        private void SetGuestProductToAdditionalServiceProduct(int orderProductId, GuestProductDto guestProduct)
        {
            AdditionalServiceProductConsumableDto consumable = AdditionalServiceProducts
                .SelectMany(x => x.ConsumableProducts)
                .FirstOrDefault(x => x.OrderProductId == orderProductId);

            if (consumable is null)
            {
                AdditionalServiceProductDto[] additionalServiceProducts = AdditionalServiceProducts
                    .Where(x => x.ParentOrderProductId.HasValue && x.ParentOrderProductId == orderProductId)
                    .ToArray();

                if (additionalServiceProducts.Any())
                {
                    additionalServiceProducts.ForEach(x => x.GuestProduct = guestProduct);
                }
            }
            else
            {
                consumable.GuestProduct = guestProduct;
            }
        }

        private bool CanReceiveGuestProduct(OrderProductViewModel orderProduct)
        {
            return orderProduct != null
                   && orderProduct.Id > 0
                   && orderProduct.IsGuestProduct
                   && orderProduct.Source.Id == OrderProductSourceType.GuestId
                   && order?.Id > 0
                   && AdditionalServiceProductCreated(orderProduct)
                   && !FillGuestProduct(orderProduct);
        }

        private bool CanReceivedGuestProduct(OrderProductViewModel orderProduct)
        {
            return orderProduct != null
                   && orderProduct.Id > 0
                   && orderProduct.IsGuestProduct &&
                   orderProduct.Source.Id == OrderProductSourceType.GuestId
                   && order?.Id > 0
                   && AdditionalServiceProductCreated(orderProduct)
                   && FillGuestProduct(orderProduct);
        }

        private bool CanShowGuestProductInfo(OrderProductViewModel orderProduct)
        {
            return orderProduct != null
                   && orderProduct.IsGuestProduct
                   && orderProduct.Source.Id != OrderProductSourceType.GuestId
                   && AdditionalServiceProductCreated(orderProduct)
                   && order?.Id > 0;
        }

        private bool AdditionalServiceProductCreated(OrderProductViewModel orderProduct)
        {
            if (AdditionalServiceProducts?.Any() != true)
            {
                return false;
            }

            AdditionalServiceProductConsumableDto consumable = AdditionalServiceProducts
                .SelectMany(x => x.ConsumableProducts)
                .FirstOrDefault(x => x.OrderProductId == orderProduct.Id);

            if (consumable is not null)
            {
                return true;
            }

            AdditionalServiceProductDto[] additionalServiceProducts = AdditionalServiceProducts
                .Where(x => x.ParentOrderProductId.HasValue && x.ParentOrderProductId == orderProduct.Id)
                .ToArray();

            if (additionalServiceProducts.Any())
            {
                return true;
            }

            return false;
        }

        private bool FillGuestProduct(OrderProductViewModel orderProduct)
        {
            List<(int OrderProductId, GuestProductDto GuestProduct)> guestProducts = new();

            guestProducts.AddRange(AdditionalServiceProducts.Where(x => x.ParentOrderProductProductId.HasValue)
                    .Select(x => (x.ParentOrderProductId!.Value, x.GuestProduct)));

            guestProducts.AddRange(AdditionalServiceProducts.SelectMany(x => x.ConsumableProducts.Where(y => y.OrderProductId.HasValue))
                    .Select(x => (x.OrderProductId!.Value, x.GuestProduct)));

            var guestProductData = guestProducts.FirstOrDefault(x => x.OrderProductId == orderProduct.Id);

            return guestProductData.GuestProduct != null && guestProductData.GuestProduct.KeepProduct && _isCheckReceiveAct > 0;
        }

        private async Task CancelCallAsync(CallViewItem callViewItem)
        {
            if (callViewItem.CallState != CallState.New)
            {
                MessageFacadeService.ShowNotificationWarning("Отменять можно только новые звонки");
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
                var newCall = result.Data;

                MessageFacadeService.ShowNotificationInfo($"Звонок №{newCall.Id} успешно отменен");

                Messenger.Send(new CallMessage(result.Data, MessageType.Changed));

                var existingCall = Calls.FirstOrDefault(x => x.Id == newCall.Id);
                Mapper.Map(newCall, existingCall);

                NewCallsCount -= 1;
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
                Logger.LogError(exception, "Error while canceling call");
                MessageFacadeService.ShowNotificationError("Ошибка при отмене звонка");
            }
        }

        private bool CanExpireOrder()
        {
            return !IsInDesignMode
                && !IsAddMode
                && LockerId == null
                && Rt == 1
                && SelectedCarryType != null && SelectedCarryType.Id != CarryType.PickupId
                && State == OrderStatus.Done
                && order.CustomerReceivedOn == null
                && WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.Warehouse, Role.ServiceManager, Role.TechSupport);
        }

        private Task ExpireOrderAsync()
        {
            return LockableOperationProcessor.DoActionAsync(
                Id,
                lockedOrder =>
                {
                    OrderExpireViewModel viewModel = DialogDocumentManagerService.ShowView<OrderExpireViewModel>(lockedOrder, this);

                    if (viewModel.IsOk)
                    {
                        Close();
                    }
                });
        }

        private async Task SetExternalOrderAsync()
        {
            GetTextFromUserParameter parameter = new GetTextFromUserParameter(
                "Номер внешнего заказа",
                "Введите номер внешнего заказа",
                content: order.ExternalOrderId);

            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(parameter, this);

            if (viewModel.IsOk && !string.Equals(viewModel.Content, order.ExternalOrderId, StringComparison.InvariantCulture))
            {
                Result<OrderDto> result = await ErrorHandler.HandleErrorsAsync(
                        _ => WebClient.ExecuteApiRequestAsync(new SetOrderExternalOrder(Id, viewModel.Content)),
                        "изменении номера внешнего заказа",
                        "Номер внешнего заказа изменён",
                        this,
                        true);

                if (result.IsSuccess)
                {
                    Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
                }
            }
        }

        private async Task ClearFiscalRegistarAsync()
        {
            Result<OrderDto> result = await ErrorHandler.HandleErrorsAsync(
                        _ => WebClient.ExecuteApiRequestAsync(new ConfirmOrderOnFiscalRegistrar(Id, null, false)),
                        "очистке параметра наличия фискализации",
                        "Параметр наличия фискализации удалён",
                        this,
                        true);

            if (result.IsSuccess)
            {
                Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
            }
        }

        private async Task EditContractorAsync()
        {
            IsLongOperationInProgress = true;

            const string GeneralErrorMessage = "Ошибки при изменении контрагента заказа";

            await LockableOperationProcessor.DoOperationAsync(Id, UpdateOrderContractorAsync, false);

            async Task UpdateOrderContractorAsync(OrderDto orderObj)
            {
                ChangeItemViewModel viewModel = DialogDocumentManagerService.ShowView<ChangeItemViewModel>(
                    new ChangeItemParameter(
                        Contractors.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                        Contractors.FirstOrDefault(x => x.Id == order.ClientId).Name,
                        "Изменение контрагента"),
                    this);

                if (!viewModel.IsOk)
                {
                    return;
                }

                Result<OrderDto> result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new UpdateOrderContractor(order.Id, viewModel.NewItem!.Value.Id, true)),
                    "изменении контрагента",
                    "Контрагент успешно сохранен",
                    this,
                    true);

                if (result.IsSuccess)
                {
                    Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
                }
            }

            IsLongOperationInProgress = false;
        }

        private async Task EditCreatedByAsync()
        {
            IsLongOperationInProgress = true;

            const string GeneralErrorMessage = "Ошибки при изменении сотрудника создавшего заказ";

            await LockableOperationProcessor.DoOperationAsync(Id, UpdateOrderCreatedByAsync, false);

            async Task UpdateOrderCreatedByAsync(OrderDto orderObj)
            {
                List<EmployeeDto> employeesList = await WebClient.ExecuteApiRequestAsync(new QueryEmployees()).GetPagedResultDataAsync();

                ChangeItemViewModel viewModel = DialogDocumentManagerService.ShowView<ChangeItemViewModel>(
                    new ChangeItemParameter(
                        employeesList.OrderBy(x => x.Name).Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                        employeesList.FirstOrDefault(x => x.Id == order.CreatedBy).Name,
                        "Изменение сотрудника создавшего заказ"),
                    this);

                if (!viewModel.IsOk)
                {
                    return;
                }

                Result<OrderDto> result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new UpdateOrderCreatedBy(order.Id, viewModel.NewItem!.Value.Id)),
                    "изменении сотрудника",
                    "Сотрудник сохранен",
                    this,
                    true);

                if (result.IsSuccess)
                {
                    Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
                }
            }

            IsLongOperationInProgress = false;
        }

        private async Task EditProductAddedByAsync(OrderProductViewModel orderProduct)
        {
            IsLongOperationInProgress = true;

            bool updated = false;

            const string GeneralErrorMessage = "Ошибки при изменении сотрудника дабавившего товар в заказ";

            await LockableOperationProcessor.DoOperationAsync(Id, UpdateOrderProductAddedByAsync, false);

            async Task UpdateOrderProductAddedByAsync(OrderDto orderObj)
            {
                List<EmployeeDto> employeesList = await WebClient.ExecuteApiRequestAsync(new QueryEmployees()).GetPagedResultDataAsync();

                ChangeItemViewModel viewModel = DialogDocumentManagerService.ShowView<ChangeItemViewModel>(
                    new ChangeItemParameter(
                        employeesList.OrderBy(x => x.Name).Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                        employeesList.FirstOrDefault(x => x.Id == orderProduct.CreatedBy).Name,
                        "Изменение сотрудника дабавившего товар"),
                    this);

                if (!viewModel.IsOk)
                {
                    return;
                }

                Result<OrderDto> result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new UpdateOrderProductAddedBy(order.Id, orderProduct.Id, viewModel.NewItem!.Value.Id)),
                    "изменении сотрудника",
                    "Сотрудник сохранен",
                    this,
                    true);

                if (result.IsSuccess)
                {
                    Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
                }
            }

            IsLongOperationInProgress = false;
        }

        private Task EditReceivedDateAsync()
        {
            return LockableOperationProcessor.DoActionAsync(
                Id,
                lockedOrder =>
                {
                    OrderEditReceivedDateViewModel viewModel = DialogDocumentManagerService.ShowView<OrderEditReceivedDateViewModel>(lockedOrder, this);

                    if (viewModel.IsOk)
                    {
                        Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
                    }
                });
        }

        private bool CanPrintTrackNumber()
        {
            return !string.IsNullOrWhiteSpace(order.PackageTtn);
        }

        private Task PrintTrackNumberAsync()
        {
            CarryType carryType = Dictionaries.GetItemById<CarryType>(order.CarryId);

            return carryType
                .GetTrackNumberProvider()
                .PrintAsync(order.PackageTtn, true);
        }

        private async Task PrintOrderAsync(int orderDocumentTypeid)
        {
            try
            {
                await Mediator.Send(new PrintOrderDocumentRequest(order.Id, orderDocumentTypeid));
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

        private async Task PrintAssemblyAsync()
        {
            PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

            OrderAssemblyReportDataDto reportDto = await WebClient.ExecuteApiRequestAsync(new QueryOrderAssemblyReport(order.Id));

            OrderAssemblyReportData reportData = Mapper.Map<OrderAssemblyReportData>(reportDto);

            reportData.SetDateTimeAssemblyPrint(DateTime.Now);

            IReport report = new OrderAssemblyReport { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = printSettings?.Main != null
                   ? new PrintReportRequest(report, true, printSettings.Main.Name, printSettings.Main.PaperSource)
                   : new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }

        private async Task PrintWarrantyCardAsync()
        {
            try
            {
                if (!order.Products.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("В заказе должен быть хотя бы один товар");
                    return;
                }

                ProductsSelectionViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ProductsSelectionViewModel>(
                    new ProductsSelectionParameter(order.Id, SeparateWarrantyCards),
                    this);

                if (viewModel.IsOk)
                {
                    await Mediator.Send(new PrintWarrantyCardRequest(
                        order.Id,
                        viewModel.SelectedItems.Select(x => x.Id).ToArray(),
                        viewModel.SeparateWarrantyCards,
                        true));
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
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private void FillHistoryFilter()
        {
            if (historyFilterItem == null)
            {
                return;
            }

            historyFilterItem.Items.Clear();
            historyFilterItem.Items.Add(OrdersCheckItem);

            if (ExternalPayments?.Any() == true)
            {
                historyFilterItem.Items.Add(new BarItemSeparator());

                foreach (var externalPayment in ExternalPayments)
                {
                    string name = $"{externalPayment.Payment.Name} ({externalPayment.Id})";

                    OrderHistoryFilterItem productHistoryFilterItem = new OrderHistoryFilterItem(externalPayment.Id, name, OrderHistoryType.ExternalPayment);
                    AddProductHistoryFilterItem(productHistoryFilterItem);
                }
            }

            historyFilterItem.Items.Add(new BarItemSeparator());
            historyFilterItem.Items.Add(RemovedProductsCheckItem);

            foreach (OrderProductViewModel orderProductViewModel in OrderProducts.Where(x => !x.IsNew))
            {
                string name = $"{orderProductViewModel.Product.Name} ({orderProductViewModel.Id})";

                OrderHistoryFilterItem productHistoryFilterItem = new OrderHistoryFilterItem(orderProductViewModel.Id, name, OrderHistoryType.Product);
                AddProductHistoryFilterItem(productHistoryFilterItem);
            }

            SelectedHistoryFilterItem ??= OrderFilterItem;
        }

        private void AddProductHistoryFilterItem(OrderHistoryFilterItem filterItem)
        {
            historyFilterItem.Items.Add(GetBarItemForHistoryFilter(filterItem));
            idRecordToFilterItemDictionary[filterItem.Id] = filterItem;
        }

        private IBarItem GetBarItemForHistoryFilter(OrderHistoryFilterItem item)
        {
            return new BarButtonItem
            {
                Content = item.Name,
                BarItemDisplayMode = BarItemDisplayMode.Content,
                GlyphAlignment = System.Windows.Controls.Dock.Right,
                Command = SetHistoryFilterItemCommand,
                CommandParameter = item
            };
        }

        private void HandleHistoryFilterLoaded(RoutedEventArgs args)
        {
            historyFilterItem = args.Source as BarSubItem;
            FillHistoryFilter();
        }

        private void SetHistoryItem(OrderHistoryFilterItem param)
        {
            SelectedHistoryFilterItem = param;
            RefreshAuditEntriesCommand.Execute(SelectedHistoryFilterItem);
        }

        private async Task CreateManyServiceRequestAsync()
        {
            CreateManyServiceRequestModel model = new CreateManyServiceRequestModel(Dictionaries, WebClient, Mapper, MessageFacadeService);
            await model.SetOrderDataAsync(Dictionaries, order);

            WizardDialogViewModel<CreateManyServiceRequestModel> wizardDialogViewModel = new WizardDialogViewModel<CreateManyServiceRequestModel>(
                typeof(CreateManyServiceRequestStep2ViewModel),
                model,
                this);

            CreateManyServiceRequestWizardDialogService.ShowDialog(MessageButton.OKCancel, "Создание заявок", wizardDialogViewModel);
        }

        private void ShowProductHistory(OrderProductViewModel product)
        {
            if (product != null)
            {
                SelectedHistoryFilterItem = idRecordToFilterItemDictionary[product.Id];

                SelectedTabName = null;
                SelectedTabName = "HistoryTab";

                RefreshAuditEntriesCommand.Execute(SelectedHistoryFilterItem);
            }
        }

        private bool CanSetNotInStock(OrderProductViewModel orderProduct)
        {
            return orderProduct != null
                && orderProduct.Id > 0
                && orderProduct.State.AllowSetSource
                && orderProduct.Source.Id == OrderProductSourceType.WarehouseSource.Id
                && State.CanChangeSources
                && !IsLockedFromAnyUser
                && AllowSetNotInStock
                && SelectedProducts.Count() <= 1;
        }

        private async Task SetNotInStockAsync(OrderProductViewModel orderProduct)
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что данного товара нет на складе?"))
            {
                return;
            }

            IsLongOperationInProgress = true;

            LockResponse<OrderDto> lockResult = null;

            try
            {
                lockResult = await TryLockAndNotifyAsync(Id, false);

                if (!lockResult.Success)
                {
                    ShowOrderIsAlreadyLockedMessage(lockResult.Dto?.EmployeeLock?.Name);
                    return;
                }

                Result<OrderProductDto> result = await WebClient.ExecuteApiRequestAsync(new SetOrderProductNotInStock(Id, orderProduct.Id));

                if (lockResult.Success)
                {
                    await TryUnlockAndNotifyAsync(Id);
                    lockResult = null;
                }

                if (result.Warnings.Any())
                {
                    ShowValidationResultView("Предупреждения при задании статуса товару", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray());
                }

                Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Не удалось задать товару статус \"Нет в наличии\"");
                ShowValidationResultView("Ошибки при сохранении товара", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError("Не удалось задать товару статус \"Нет в наличии\"");
                ShowValidationResultView("Ошибки при сохранении товара", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save order product");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении товара");
            }
            finally
            {
                IsLongOperationInProgress = false;

                if (lockResult != null && lockResult.Success)
                {
                    await TryUnlockAndNotifyAsync(Id);
                }
            }
        }

        private bool CanFillSources()
        {
            return Id > 0 && State.CanChangeSources && !IsLockedFromAnyUser && SelectedProducts.Count() <= 1;
        }

        private Task FillSourceAsync()
        {
            Task task = Task.CompletedTask;

            if (MessageFacadeService.Confirm("Вы уверены?"))
            {
                task = LockableOperationProcessor.DoOperationAsync(Id, x => FillSourcesInternalAsync(x, true), false);
            }

            return task;
        }

        private async Task FillSourcesInternalAsync(OrderDto lockedOrder, bool showMessages)
        {
            ShowLoadingIndicator = true;

            try
            {
                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(new FillOrderSources(lockedOrder.Id, null, true, true));

                if (SelectedWarehouse == null && result.Data.WarehouseId != null)
                {
                    SelectedWarehouse = Warehouses.FirstOrDefault(x => x.Id == result.Data.WarehouseId);
                }

                SetProducts(result.Data.Products, result.Data.Folders);

                if (showMessages)
                {
                    if (result.Warnings.Any())
                    {
                        MessageFacadeService.ShowNotificationWarning("Источники установлены c предупреждениями");
                        ShowValidationResultView("Предупреждения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationInfo("Источники успешно установлены");
                    }
                }
            }
            catch (UnexpectedSatusException exception)
            {
                IReadOnlyCollection<ValidationResultItem> validationItems = exception.GetErrorItems();

                if (validationItems.Any(x => x.IsError))
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при установке источников");
                    ShowValidationResultView("Ошибки при установке источников", validationItems);
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning(string.Join(Environment.NewLine, validationItems.Select(x => x.Message)));
                }
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при установке источников");
                ShowValidationResultView("Ошибки при установке источников", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to fill order sources");
                MessageFacadeService.ShowNotificationError("Ошибка при установке источников");
            }
            finally
            {
                ShowLoadingIndicator = false;
            }
        }

        private Task FillSourcesManualAsync()
        {
            return LockableOperationProcessor.DoOperationAsync(Id, FillSourceManualInternalAsync, false);
        }

        private Task FillSourceManualInternalAsync(OrderDto lockedOrder)
        {
            FillSourcesWarehousesListViewModel viewModel = DialogDocumentManagerService.ShowView<FillSourcesWarehousesListViewModel>(
                new FillSourcesWarehousesListParameter(lockedOrder.Id, lockedOrder.WarehouseId),
                this);

            if (viewModel.IsOk)
            {
                Result<OrderDto> result = viewModel.Result;

                if (SelectedWarehouse == null && result.Data.WarehouseId != null)
                {
                    SelectedWarehouse = Warehouses.FirstOrDefault(x => x.Id == result.Data.WarehouseId);
                }

                SetProducts(result.Data.Products, result.Data.Folders);
            }

            return Task.CompletedTask;
        }

        private void SetProducts(IReadOnlyCollection<OrderProductDto> products, IReadOnlyCollection<OrderFolderDto> folders)
        {
            order.Products = products;

            OrderProducts.CollectionChanged -= OrderProductsCollectionChangedEvent;

            OrderProducts.Clear();

            PostProcessOrderProducts(
                products.Select(x => MapOrderProduct(x, new OrderProductViewModel(Dictionaries, MessageFacadeService, OrderRules), folders?.FirstOrDefault(z => z.Id == x.OrderFolderId))).ToReadOnlyCollection(),
                PostProcessProductsMode.AddRange,
                true);

            OrderProductsCollectionChanged();

            OrderProducts.CollectionChanged += OrderProductsCollectionChangedEvent;
        }

        private void SetBonuses(IReadOnlyCollection<OrderProductBonusDto> bonuses)
        {
            Bonuses = bonuses?.ToObservableCollection();

            RefreshBonusSummary();
        }

        private async Task RefreshExternalPaymentsAsync()
        {
            List<ExternalPaymentDto> externalPayments = await WebClient.ExecuteApiRequestAsync(new QueryExternalPayments(Id));

            ExternalPayments = externalPayments
                .Select(x => Mapper.Map<ExternalPaymentViewItem>(x))
                .OrderBy(x => x.CreatedOn)
                .ToObservableCollection();

            ExternalPaymentsCount = ExternalPayments.Count;

            SelectedExternalPayment = ExternalPayments.FirstOrDefault();
        }

        private async Task RefreshOrderPaymentsAsync()
        {
            OrderPayments = null;

            try
            {
                List<OrderPaymentDto> result = await WebClient.ExecuteApiRequestAsync(new QueryOrderPayments(Id));

                OrderPayments = result
                    .OrderBy(x => x.CreatedOn)
                    .Select(x => Mapper.Map<OrderPaymentRecordViewItem>(x))
                    .ToObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to load order payments");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task AddOrderPaymentAsync()
        {
            if (SelectedPayment?.Id is Payment.MonobankId or Payment.CreditId or Payment.PrivatPartialPayId or Payment.PumbId
                && OrderPayments?.Any() != true
                && (!WebClient.IsOperationAllowed(BusinessOperation.OrderCreditPaymentIgnoreError)
                && !WebClient.IsOperationAllowed(BusinessOperation.OrderCreditPaymentIgnoreErrorOnFirstPayment)))
            {
                MessageFacadeService.ShowNotificationWarning("В кредитных заказах, первая оплата не может быть внесена вручную");
                return;
            }

            IsLongOperationInProgress = true;

            await LockableOperationProcessor.DoOperationAsync(Id, x => AddOrderPaymentInternalAsync(x, false), false);

            IsLongOperationInProgress = false;
        }

        private async Task<bool> AddOrderPaymentInternalAsync(OrderDto actualOrderObj, bool fullAmount)
        {
            AddOrderPaymentParameter parameter = new AddOrderPaymentParameter(
                actualOrderObj.CreatedOn.Date,
                actualOrderObj.LegalEntity,
                actualOrderObj.GetPrices().ToPay,
                actualOrderObj.Products
                    .Select(x => x.CurrencyOutId)
                    .Distinct()
                    .Select(Currency.GetById)
                    .ToArray(),
                actualOrderObj.ClientId,
                fullAmount,
                actualOrderObj.PaymentId);

            AddOrderPaymentViewModel vm = DialogDocumentManagerService.ShowView<AddOrderPaymentViewModel>(parameter, this);

            if (vm.IsOk)
            {
                try
                {
                    CreateOrderPayment gatewayRequest = new CreateOrderPayment(
                        actualOrderObj.Id,
                        vm.SelectedCurrency.Id,
                        vm.SelectedPaymentType?.Id,
                        vm.SelectedCashboxId!.Value,
                        vm.Amount,
                        vm.ReceivedOn,
                        vm.Comment,
                        prepayment: true);

                    Result<OrderPaymentResultDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                    MessageFacadeService.ShowNotificationInfo($"Данные об оплате для заказа №{result.Data.Order.Id} успешно сохранены");

                    ProcessNewPayment(result);
                }
                catch (UnexpectedSatusException exception)
                {
                    ShowValidationResultView("Ошибки при внесении оплаты заказа", exception.GetErrorItems());
                }
                catch (UnexpectedErrorException)
                {
                    ShowValidationResultView("Ошибки при внесении оплаты заказа", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Error while creating order payment");
                    MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
                }
            }

            return vm.IsOk;
        }

        private async Task AddOrderPaymentViaCashRegistrarAsync()
        {
            IsLongOperationInProgress = true;

            await LockableOperationProcessor.TryLockAsync(Id, true, false);

            (OrderDto Order, OrderPaymentDto OrderPayment) paymentResult = await OrderGiveHelper.AddOrderPaymentViaCashRegistrarAsync(
                order,
                true,
                false,
                true,
                this);

            await LockableOperationProcessor.UnlockAsync(Id, true);

            if (paymentResult.Order != null)
            {
                order = paymentResult.Order;
                Pko = paymentResult.Order.Pko;
            }

            RefreshSummaryItems();

            IsLongOperationInProgress = false;
        }

        private async Task AddOrderWithdrawAsync()
        {
            bool hasRoleAccountant = WebClient.AuthenticatedEmployee.HasAnyRole(Role.Accountant);

            if (order.BasedOnServiceRequestId.HasValue)
            {
                ServiceRequestDto serviceRequest = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(order.BasedOnServiceRequestId.Value));

                if (serviceRequest.BundleId.HasValue)
                {
                    if (WebClient.IsOperationAllowed(BusinessOperation.RefundMoneyFromBundleChangeOrder))
                    {
                        if (!MessageFacadeService.Confirm("Вы точно уверены что хотите вернуть ДС по обменному заказу на товар из бандла?"))
                        {
                            return;
                        }
                    }
                    else
                    {
                        MessageFacadeService.ShowMessageBoxError("У Вас нет прав на операцию создания Возврата ДС в обменном заказе по бандлу");
                        return;
                    }
                }
            }

            List<OrderPaymentDto> orderPaymentDtos = order.OrderPayments
                .Where(x => x.Sign > 0)
                .ToList();

            List<CashboxDto> cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            if (hasRoleAccountant
                || order.BasedOnServiceRequestId != null
                || PaymentOnNonVirtualCashbox(orderPaymentDtos, cashboxes)
                || (PaymentOnVirtualCashbox(orderPaymentDtos, cashboxes)
                    && order.PaymentId != Payment.LiqPayId
                    && order.PaymentId != Payment.MonoPayId
                    && order.PaymentId != Payment.NovaPayId
                    && order.PaymentId != Payment.PortmoneId))
            {
                await LockableOperationProcessor.DoActionAsync(Id, AddOrderWithdrawInternal, false);
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нет фактической оплаты по данному заказу, просьба обратиться в бухгалтерию");
            }

            void AddOrderWithdrawInternal(OrderDto actualOrderObj)
            {
                IsLongOperationInProgress = true;

                IReadOnlyCollection<OrderPaymentRecordViewItem> orderPayments = Mapper.Map<OrderPaymentRecordViewItem[]>(actualOrderObj.OrderPayments);

                AddOrderWithdrawParameter parameter;

                if (Payment.IsCreditPayment(order.PaymentId) && order.ExternalPayments.Any(x => x.PaymentId == order.PaymentId && PaymentState.IsActual(x.PaymentStateId)))
                {
                    OrderPaymentRecordViewItem orderPayment = orderPayments.FirstOrDefault(x => x.PaymentId == order.PaymentId);

                    if (orderPayment == null)
                    {
                        string paymentName = Dictionaries.GetItemById<Payment>(order.PaymentId).Name;

                        MessageFacadeService.ShowNotificationWarning($"В заказе должна быть оплата со способом оплаты \"{paymentName}\"");

                        return;
                    }

                    parameter = new AddOrderWithdrawParameter(
                        actualOrderObj.Id,
                        orderPayments,
                        actualOrderObj.LegalEntity,
                        orderPayment.PaymentId,
                        orderPayment.CurrencyId,
                        orderPayment.CashboxId,
                        OrderPaymentViewModel.PrepaymentItem.GetByCurrency(orderPayment.CurrencyId),
                        firstName: actualOrderObj.FirstName,
                        lastName: actualOrderObj.LastName,
                        middleName: actualOrderObj.MiddleName);
                }
                else
                {
                    parameter = new AddOrderWithdrawParameter(
                        actualOrderObj.Id,
                        orderPayments,
                        actualOrderObj.LegalEntity,
                        firstName: actualOrderObj.FirstName,
                        lastName: actualOrderObj.LastName,
                        middleName: actualOrderObj.MiddleName);
                }

                AddOrderWithdrawViewModel vm = DialogDocumentManagerService.ShowView<AddOrderWithdrawViewModel>(parameter, this);

                if (vm.IsOk)
                {
                    Pko = vm.Result.Data.Order.Pko;
                    RefreshSummaryItems();
                }

                IsLongOperationInProgress = false;
            }
        }

        private void ResendSms()
        {
            ResendSmsParameter parameter = new ResendSmsParameter(
                SelectedClientContact.Phone,
                SelectedClientContact.Content,
                SelectedClientContact.SmsTemplateId,
                order.Id,
                SelectedClientContact.Type);

            ResendSmsViewModel viewModel = DialogDocumentManagerService.ShowView<ResendSmsViewModel>(parameter, this);

            if (viewModel.IsOk)
            {
                RefreshCrmCommand.Execute(null);
            }
        }

        private async Task RefreshCrmAsync()
        {
            ClientContactsHistoryItems = null;

            try
            {
                List<ClientContactDto> clientContacts = await WebClient.ExecuteApiRequestAsync(new QueryOrderClientContacts(Id));

                ClientContactsHistoryItems = clientContacts
                    .Select(x => Mapper.Map<ClientContactViewItem>(x))
                    .OrderByDescending(x => x.Date)
                    .ToReadOnlyObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get order crm info");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshComplaintsAsync()
        {
            Complaints = null;

            try
            {
                IFilteringItem filteringItem = new ComplaintsFilteringItem { OrderIds = Id.ToString() };

                List<ComplaintDto> complaints = await WebClient.ExecuteApiRequestAsync(new QueryComplaints(filteringItem)).GetPagedResultDataAsync();
                Complaints = complaints
                    .Select(x => Mapper.Map<ComplaintViewItem>(x))
                    .ToObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get order complaints");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void AddComplaint()
        {
            ComplaintCreateParameter parameter = new ComplaintCreateParameter(
                Id,
                null,
                null,
                SelectedContractor.Id,
                GetOrderProducts().Select(x => new ComboBoxItem(x.Product.Id, x.Product.Name)).ToList(),
                order.Fio,
                Phone,
                Phone2,
                Email);

            NonModalDialogDocumentManagerService.ShowView<ComplaintCreateViewModel>(parameter, this);
        }

        private async Task AddAssembledComputerRuleAsync()
        {
            SelectComplectationGeneratorOptionsViewModel complectationGeneratorOptionsViewModel = DialogDocumentManagerService.ShowView<SelectComplectationGeneratorOptionsViewModel>(null, this);

            if (!complectationGeneratorOptionsViewModel.IsOk)
            {
                return;
            }

            ComplactationGeneratorOptionsItem generatorOptions = complectationGeneratorOptionsViewModel.Model;

            SelectAssembledComputerRuleParameter parameter = new SelectAssembledComputerRuleParameter(SelectedContractor.Id);

            SelectAssembledComputerRuleViewModel selectAssembledComputerRuleViewModel = DialogDocumentManagerService.ShowView<SelectAssembledComputerRuleViewModel>(parameter, this);

            if (!selectAssembledComputerRuleViewModel.IsOk)
            {
                return;
            }

            List<ProductDto> products = new List<ProductDto>();

            PreloaderViewModel loaderViewModel = DialogDocumentManagerService.ShowView<PreloaderViewModel>(
                new PreloaderParameter(x => QueryGeneratedAssembledComputerRulesAsync(selectAssembledComputerRuleViewModel.SelectedProducts, generatorOptions, x)),
                this);

            if (!loaderViewModel.IsOk)
            {
                return;
            }

            QueryProductByIdsDto queryProductByIdsDto = new QueryProductByIdsDto(new[] { Constants.AssemblyServiceProductId }, SelectedContractor.Id);

            List<ProductDto> assemblyServiceProduct = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(queryProductByIdsDto));

            NomenclatureViewItem assembledComputerRuleViewItem = Mapper.Map<NomenclatureViewItem>(assemblyServiceProduct.First());

            List<string> errors = new();

            foreach (ProductDto product in products)
            {
                if (product.Complectation?.Any() == true)
                {
                    int quantity = selectAssembledComputerRuleViewModel.SelectedProducts.First(x => x.ProductId == product.Id).Quantity;

                    OrderFolderDto orderFolder = new()
                    {
                        Id = new Random().GetRandomId(),
                        Name = product.Name,
                        Quantity = quantity,
                        TypeId = OrderFolderType.AssembledComputerRuleId,
                        ProductId = product.Id,
                        Price = product.Price,
                        FreeDelivery = product.FreeDelivery
                    };

                    List<NomenclatureViewItem> assembledComputerRuleProducts = product.Complectation
                        .Select(x => Mapper.Map<NomenclatureViewItem>(x))
                        .ToList();

                    assembledComputerRuleViewItem.Quantity = quantity;

                    assembledComputerRuleProducts.Add(assembledComputerRuleViewItem);

                    ProcessSelectedForAddingProducts(assembledComputerRuleProducts, orderFolder);
                }
                else
                {
                    ProcessSelectedForAddingProducts(new[] { Mapper.Map<NomenclatureViewItem>(product) });
                    errors.Add($"Не получилось найти товары под конфигурацию '{product.Name}'");
                }
            }

            if (errors.Any())
            {
                MessageFacadeService.ShowValidationResultView("Ошибки при добавлении конфигураций", errors.Select(x => new ValidationResultItem(x, false)), this);
            }

            MessageFacadeService.ShowNotificationInfo("Состав конфигурации является коммерческой тайной компании");

            async Task<IEnumerable<ValidationResultItem>> QueryGeneratedAssembledComputerRulesAsync(IReadOnlyCollection<SelectAssembledComputerRuleProductViewItem> assembledComputerRuleProducts, ComplactationGeneratorOptionsItem genaretorOptionsItem, IProgress<string> progress)
            {
                progress.Report("Генерация товаров под конфигурацию...");

                int i = 1;

                foreach (SelectAssembledComputerRuleProductViewItem product in assembledComputerRuleProducts)
                {
                    progress.Report($"Генерация товаров под конфигурацию №{i++}...");

                    QueryProductByIdsDto queryProductByIdsDto = new(new[] { product.ProductId }, SelectedContractor.Id)
                    {
                        GenerateComplectation = true,
                        MaxComplectationPrice = product.Price + (product.Price * (decimal)(genaretorOptionsItem.OverPricePercent / 100)),
                        UseWarehouse = genaretorOptionsItem.UseWarehouse,
                        UseTransits = genaretorOptionsItem.UseTransits,
                        UsePurchases = genaretorOptionsItem.UsePurchases
                    };

                    List<ProductDto> productsPart = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(queryProductByIdsDto));

                    if (productsPart.Any())
                    {
                        products.Add(productsPart.First());
                    }
                }

                return Array.Empty<ValidationResultItem>();
            }
        }

        private OrderProductViewModel AddFolder(string name, OrderFolderType type)
        {
            OrderFolderDto orderFolder = new()
            {
                Id = Random.Shared.GetRandomId(),
                Name = name,
                Quantity = 1,
                TypeId = type.Id,
                ProductId = null,
                Price = null,
                FreeDelivery = false
            };

            OrderProductViewModel orderFolderProduct = new OrderProductViewModel(Dictionaries, MessageFacadeService, OrderRules)
            {
                Id = Random.Shared.GetRandomId(),
                CurrencyId = Currency.UahId,
                CurrencyOutId = Currency.UahId,
                OrderFolderId = orderFolder.Id,
                OrderFolder = orderFolder,
                AssemblyId = null,
                Quantity = 1,
                State = OrderProductStatus.None,
                Source = new NoneOrderProductSource(),
                IsVirtualProduct = true,
                AssembliesInAssemblyModule = null,
                Product = new ProductSimpleDto { NameFullRu = orderFolder.Name },
                FreeDelivery = false
            };

            orderFolderProduct.SetParentViewModel(this);

            OrderProducts.Add(orderFolderProduct);

            return orderFolderProduct;
        }

        private async Task AddBundleAsync(IEnumerable<OrderProductViewModel> orderProducts)
        {
            if (SelectedSubdivision != Subdivision.Telemart)
            {
                MessageFacadeService.ShowNotificationWarning("Использование промо-кодов возможно лишь с подразделением «Телемарт»");
                return;
            }

            GetTextFromUserViewModel vm = GetInfoDialog("Введите промо-код или код бандла", "Промо-код / код бандла");

            if (!vm.IsOk)
            {
                return;
            }

            string promoCode = vm.Content;
            string[] promoCodes = null;
            int[] promoCodeIds = null;
            bool alreadyAdded = false;

            if (int.TryParse(promoCode, out int promoCodeId))
            {
                if (PromoCodes.Any(x => x.PromoCodeId == promoCodeId))
                {
                    alreadyAdded = true;
                }

                promoCodeIds = PromoCodes.Select(x => x.PromoCodeId).Concat(promoCodeId).ToArray();
            }
            else
            {
                if (PromoCodes.Any(x => string.Equals(x.Value, promoCode, StringComparison.Ordinal)))
                {
                    alreadyAdded = true;
                }

                promoCodes = PromoCodes.Select(x => x.Value).Concat(promoCode).ToArray();
            }

            var request = promoCodeId > 0
                ? new QueryPromoCode(promoCodeId)
                : new QueryPromoCode(promoCode);

            PromoCodeFullDto promoCodeData = await WebClient.ExecuteApiRequestAsync(request);

            if (promoCodeData.TypeId == PromoCodeType.ProductDiscount.Id)
            {
                MessageFacadeService.ShowNotificationWarning($"Вы ввели промо-код с типом \"{PromoCodeType.ProductDiscount.Name}\"");
                return;
            }

            (bool Success, CheckPromoCodesResponse Result) response = await ProcessPromoCodesAsync(new[] { promoCode }, new[] { promoCodeId }, orderProducts, bundle: true);

            if (response is { Success: true, Result: not null })
            {
                CheckPromoCodeResult promoCodeResult = response.Result.PromoCodes.First(x => x.PromoCodeValue == promoCode || x.PromoCodeId!.Value.ToString() == promoCode);

                OrderPromoCodeDto dto = new OrderPromoCodeDto
                {
                    Id = new Random().GetRandomId(),
                    PromoCodeId = promoCodeData.Id,
                    Value = promoCodeData.Value,
                    PromoCodeMetaTitle = promoCodeData.MetaTitle,
                    PromoCodeTypeId = promoCodeData.TypeId
                };

                if (!alreadyAdded)
                {
                    PromoCodes.Add(dto);
                }

                var bundleProducts = SelectedProducts.Where(x => x.OrderPromoCodeId == promoCodeData.Id).ToArray();

                var bundleFolderProduct = AddFolder($"Бандл {dto.PromoCodeId} \"{dto.Value}\"", OrderFolderType.Bundle);

                bundleProducts.ForEach(x =>
                {
                    x.ParentId = bundleFolderProduct.Id;
                    x.OrderFolder = bundleFolderProduct.OrderFolder;
                    x.OrderFolderId = bundleFolderProduct.OrderFolderId;
                    x.AssemblyQuantity = x.Quantity;
                    x.BonusesToCharge = null;
                });

                CalculateBundleProductPrice(bundleFolderProduct.OrderFolderId, bundleProducts);

                RecalculateCanChangePriceIdForCurrentProduct();

                bundleFolderProduct.RefreshAllowEdit();
            }

            void CalculateBundleProductPrice(int? orderFolderId, IEnumerable<OrderProductViewModel> bundleProducts)
            {
                OrderProductViewModel bundleProductOrderProduct = OrderProducts
                    .FirstOrDefault(x => x.OrderFolderId == orderFolderId
                        && !bundleProducts.Select(y => y.Id).Contains(x.Id));

                if (bundleProductOrderProduct != null)
                {
                    decimal bundlePrice = bundleProducts
                        .Where(x => x.OrderFolderId == orderFolderId && x.Id != bundleProductOrderProduct.Id)
                        .Sum(x => x.PriceOut * x.AssemblyQuantity!.Value);

                    bundleProductOrderProduct.PriceOut = bundlePrice;
                }
            }
        }

        private async Task DeleteBundleAsync(OrderProductViewModel bundleFolderProduct)
        {
            if (!IsLockedByCurrentUserAndEditingAllowed)
            {
                return;
            }

            if (!MessageFacadeService.Confirm("Вы уверены, что хотите удалить бандл?"))
            {
                return;
            }

            var bundleProducts = OrderProducts.Where(x => x.ParentId == bundleFolderProduct.Id).ToArray();

            var otherBundleProducts = OrderProducts.Where(x => x.OrderPromoCodeId == bundleProducts.First().OrderPromoCodeId).ToArray();

            var promoCode = PromoCodes.First(x => x.PromoCodeId == bundleProducts.First().OrderPromoCodeId);

            (bool Success, CheckPromoCodesResponse Result) response = await ProcessPromoCodesAsync(Array.Empty<string>(), null, bundleProducts, true);

            if (response.Success && response.Result != null)
            {
                if (bundleProducts.Count() == otherBundleProducts.Count())
                {
                    PromoCodes.Remove(promoCode);
                }

                await DeleteOrderProductsAsync(new[] { bundleFolderProduct });
            }
        }

        private async Task AddAssemblyAsync()
        {
            OrderFolderDto orderFolder = new OrderFolderDto()
            {
                Id = new Random().GetRandomId(),
                TypeId = OrderFolderType.AssemblyServiceId,
                Name = "Сборка",
                Quantity = 1,
                ProductId = null,
                FreeDelivery = false
            };

            AssemblyViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<AssemblyViewModel>(new AssemblyParameter(SelectedContractor.Id, OrderRules.NeedCheckGifts(SelectedSubdivision), true, orderFolder), this);

            if (viewModel.IsOk)
            {
                var products = await WebClient.ExecuteCatalogApiRequestAsync(
                    new QueryProductByIds(
                        SelectedContractor.Id,
                        viewModel.GetProducts().Select(x => x.NomenclatureItem.Id).ToArray(),
                        OrderRules.NeedCheckGifts(SelectedSubdivision),
                        true));

                var nomenclatureItems = Mapper.Map<NomenclatureViewItem[]>(products);

                var viewModelProducts = viewModel.GetProducts().ToArray();

                foreach (AssemblyViewModelResult viewModelResult in viewModelProducts)
                {
                    var nomenclatureItem = nomenclatureItems.FirstOrDefault(x => x.Id == viewModelResult.NomenclatureItem?.Id);

                    nomenclatureItem.Quantity = viewModelResult.NomenclatureItem.Quantity;
                    nomenclatureItem.AssemblyIncluded = viewModelResult.NomenclatureItem.AssemblyIncluded;
                    nomenclatureItem.Price = viewModelResult.NomenclatureItem.Price;

                    viewModelResult.NomenclatureItem = nomenclatureItem;
                }

                var assemblyServiceProduct =
                    viewModelProducts.FirstOrDefault(x => x.NomenclatureItem?.Id == Constants.AssemblyServiceProductId)?.NomenclatureItem;

                if (assemblyServiceProduct != null)
                {
                    decimal assemblyPrice = viewModelProducts
                        .Where(
                            x => x.NomenclatureItem != null &&
                                 x.NomenclatureItem.Id != Constants.AssemblyServiceProductId)
                        .Sum(x => x.NomenclatureItem.Price * x.NomenclatureItem.Quantity);

                    var calculatePriceResult = await WebClient.ExecuteApiRequestAsync(new CalculateAssemblyServiceProductPrice(assemblyPrice));

                    if (calculatePriceResult.IsSuccess)
                    {
                        assemblyServiceProduct.Price = calculatePriceResult.Data.Price;
                    }
                    else
                    {
                        Logger.LogError("Failed to calculate assembly service product price. Errors: {Errors}", calculatePriceResult.ErrorObj.GetErrorMessage());
                    }
                }

                ProcessSelectedForAddingProducts(viewModelProducts, orderFolder);
            }
        }

        private async Task EditAssemblyAsync(OrderProductViewModel orderProduct)
        {
            bool anyAssemblyServices = false;

            if (orderProduct.IsAssembly() && (orderProduct.Id > 0 || (orderProduct.IsVirtualProduct && OrderProducts.Any(x => x.ParentId == orderProduct.Id && x.Id > 0))))
            {
                OrderCanEditAssemblyResponse response = await CanEditAssemblyAsync(orderProduct.OrderFolderId!.Value);

                if (response == null)
                {
                    return;
                }

                anyAssemblyServices = response.AnyAssemblyServices;
            }

            List<OrderProductViewModel> orderProducts = OrderProducts
                .Where(x => (!x.IsAdditionalService || x.TypeId == ProductType.AccessoryId) && !x.IsVirtualProduct && x.OrderFolderId == orderProduct.OrderFolderId)
                .ToList();

            AssemblyViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<AssemblyViewModel>(
                new AssemblyParameter(
                    SelectedContractor.Id,
                    OrderRules.NeedCheckGifts(SelectedSubdivision),
                    true,
                    orderProduct.OrderFolder,
                    orderProducts.Select(x => new AssemblyParameterProduct(
                        x.Product.Id,
                        x.AssemblyQuantity!.Value,
                        (x.Source != null && x.Source.Id != OrderProductSourceType.NoneId) || x.IsAdditionalServiceConsumable,
                        x.AssemblyIncluded,
                        x.IsGift,
                        x.PriceOut)).ToArray(),
                    anyAssemblyServices),
                this);

            if (viewModel.IsOk)
            {
                int assemblyQuantity = OrderProducts.First(x => x.IsVirtualProduct && x.OrderFolderId == orderProduct.OrderFolderId).Quantity;

                List<OrderProductViewModel> orderProductsToDelete = new List<OrderProductViewModel>();

                List<AssemblyViewModelResult> products = viewModel.GetProducts().ToList();

                foreach (OrderProductViewModel orderProductInternal in orderProducts)
                {
                    AssemblyViewModelResult product = products.FirstOrDefault(x => x.NomenclatureItem.Id == orderProductInternal.Product.Id);

                    if (product == null)
                    {
                        orderProductsToDelete.Add(orderProductInternal);
                    }
                    else
                    {
                        if (orderProductInternal.AssemblyQuantity < product.NomenclatureItem.Quantity)
                        {
                            orderProductInternal.Quantity += (product.NomenclatureItem.Quantity - orderProductInternal.AssemblyQuantity.Value) * assemblyQuantity;
                        }
                        else if (orderProductInternal.AssemblyQuantity > product.NomenclatureItem.Quantity)
                        {
                            int removeQuantity = (orderProductInternal.AssemblyQuantity.Value - product.NomenclatureItem.Quantity) * assemblyQuantity;

                            foreach (OrderProductViewModel orderProductViewModel in OrderProducts.Where(x => x.Product.Id == product.NomenclatureItem.Id && x.OrderFolderId == orderProduct.OrderFolderId))
                            {
                                if (orderProductViewModel.Quantity <= removeQuantity)
                                {
                                    removeQuantity -= orderProductViewModel.Quantity;
                                    orderProductsToDelete.Add(orderProductViewModel);
                                }
                                else
                                {
                                    orderProductViewModel.Quantity -= removeQuantity;

                                    break;
                                }
                            }
                        }

                        foreach (OrderProductViewModel orderProductViewModel in OrderProducts.Where(x => x.Product.Id == product.NomenclatureItem.Id && x.OrderFolderId == orderProduct.OrderFolderId))
                        {
                            orderProductViewModel.AssemblyQuantity = product.NomenclatureItem.Quantity;
                            orderProductViewModel.AssemblyIncluded = product.AssemblyIncluded;
                        }
                    }
                }

                foreach (OrderProductViewModel orderProductViewModel in orderProductsToDelete)
                {
                    foreach (OrderProductViewModel childOrderProduct in OrderProducts.Where(x => x.ParentRecordId == orderProductViewModel.Id).ToArray())
                    {
                        if (!(childOrderProduct.IsAdditionalService && IsAdditionalServiceProductCompleted(childOrderProduct.Id)))
                        {
                            childOrderProduct.ValidationStarted -= OrderProductValidationStarted;
                            OrderProducts.Remove(childOrderProduct);
                        }
                    }

                    orderProductViewModel.ValidationStarted -= OrderProductValidationStarted;

                    OrderProducts.Remove(orderProductViewModel);
                }

                int[] existsProductIds = OrderProducts.Where(y => y.OrderFolderId == orderProduct.OrderFolderId)
                    .Select(x => x.ProductId)
                    .ToArray();

                List<AssemblyViewModelResult> assemblyResults = products
                    .Where(x => !existsProductIds.Contains(x.NomenclatureItem?.Id ?? 0) && !x.IsGift)
                    .ToList();

                if (assemblyResults.Any())
                {
                    var productsFromCatalog = await WebClient.ExecuteCatalogApiRequestAsync(
                        new QueryProductByIds(
                            SelectedContractor.Id,
                            assemblyResults.Select(x => x.NomenclatureItem.Id).ToArray(),
                            OrderRules.NeedCheckGifts(SelectedSubdivision),
                            true,
                            priceIn: true,
                            cartProductIds: existsProductIds,
                            conditionGifts: orderProduct.OrderFolder?.TypeId != OrderFolderType.AssembledComputerRuleId));

                    NomenclatureViewItem[] nomenclatureItems = Mapper.Map<NomenclatureViewItem[]>(productsFromCatalog);

                    foreach (AssemblyViewModelResult assemblyResult in assemblyResults)
                    {
                        NomenclatureViewItem nomenclatureItem = nomenclatureItems.First(x => x.Id == assemblyResult.NomenclatureItem?.Id);

                        nomenclatureItem.Quantity = assemblyResult.NomenclatureItem.Quantity;
                        nomenclatureItem.AssemblyIncluded = assemblyResult.NomenclatureItem.AssemblyIncluded;

                        assemblyResult.NomenclatureItem = nomenclatureItem;
                    }

                    ProcessSelectedForAddingProducts(assemblyResults, orderProduct.OrderFolder, assemblyQuantity);
                }
                else
                {
                    PostProcessOrderProducts(OrderProducts.ToList(), PostProcessProductsMode.ReplaceRange);
                }

                if (OrderProducts.Any(x => x.OrderFolderId == orderProduct.OrderFolderId && x.ProductId == Constants.AssemblyServiceProductId)
                    && orderProduct.OrderFolder?.TypeId != OrderFolderType.AssembledComputerRuleId)
                {
                    await CalculateAssemblyServiceProductPriceAsync(orderProduct.OrderFolderId);
                }
            }
        }

        private async Task CreateAssemblyWithProductsAsync()
        {
            OrderFolderDto orderFolder = new()
            {
                Id = Random.Shared.GetRandomId(),
                Name = "Сборка",
                Quantity = 1,
                TypeId = OrderFolderType.AssemblyServiceId,
                ProductId = null,
                Price = null,
                FreeDelivery = false
            };

            var viewModel = SizeableDialogDocumentManagerService
                .ShowView<AssemblyViewModel>(
                    new AssemblyParameter(
                        SelectedContractor.Id,
                        OrderRules.NeedCheckGifts(SelectedSubdivision),
                        true,
                        orderFolder,
                        SelectedProducts.Select(x => new AssemblyParameterProduct(x.ProductId, x.Quantity, false, true, x.IsGift, x.Price)).ToReadOnlyCollection()),
                    this);

            if (viewModel.IsOk)
            {
                OrderProductViewModel assemblyFolder = new OrderProductViewModel(Dictionaries, MessageFacadeService, OrderRules)
                {
                    Id = Random.Shared.GetRandomId(),
                    CurrencyId = Currency.UahId,
                    CurrencyOutId = Currency.UahId,
                    OrderFolderId = orderFolder.Id,
                    OrderFolder = orderFolder,
                    AssemblyId = null,
                    Quantity = 1,
                    State = OrderProductStatus.None,
                    Source = new NoneOrderProductSource(),
                    IsVirtualProduct = true,
                    AssembliesInAssemblyModule = null,
                    Product = new ProductSimpleDto { NameFullRu = orderFolder.Name },
                    FreeDelivery = false
                };

                assemblyFolder.SetParentViewModel(this);

                OrderProducts.Add(assemblyFolder);

                var assemblyProducts = SelectedProducts.ToArray();

                assemblyProducts.ForEach(x =>
                {
                    x.ParentId = assemblyFolder.Id;
                    x.OrderFolder = orderFolder;
                    x.OrderFolderId = orderFolder.Id;
                    x.AssemblyQuantity = x.Quantity;
                });

                OrderProducts = OrderProducts.ToObservableRangeCollection();

                List<OrderProductViewModel> orderProductsToDelete = new List<OrderProductViewModel>();

                List<AssemblyViewModelResult> products = viewModel.GetProducts().ToList();

                foreach (OrderProductViewModel orderProductInternal in assemblyProducts)
                {
                    AssemblyViewModelResult product = products.FirstOrDefault(x => x.NomenclatureItem.Id == orderProductInternal.Product.Id);

                    if (product is null)
                    {
                        orderProductsToDelete.Add(orderProductInternal);
                    }
                    else
                    {
                        if (orderProductInternal.AssemblyQuantity < product.NomenclatureItem.Quantity)
                        {
                            orderProductInternal.Quantity += product.NomenclatureItem.Quantity - orderProductInternal.AssemblyQuantity.Value;
                        }
                        else if (orderProductInternal.AssemblyQuantity > product.NomenclatureItem.Quantity)
                        {
                            int removeQuantity = orderProductInternal.AssemblyQuantity.Value - product.NomenclatureItem.Quantity;

                            foreach (OrderProductViewModel orderProductViewModel in OrderProducts.Where(x => x.Product.Id == product.NomenclatureItem.Id && x.OrderFolderId == assemblyFolder.OrderFolderId))
                            {
                                if (orderProductViewModel.Quantity <= removeQuantity)
                                {
                                    removeQuantity -= orderProductViewModel.Quantity;
                                    orderProductsToDelete.Add(orderProductViewModel);
                                }
                                else
                                {
                                    orderProductViewModel.Quantity -= removeQuantity;

                                    break;
                                }
                            }
                        }

                        foreach (OrderProductViewModel orderProductViewModel in OrderProducts.Where(x => x.Product.Id == product.NomenclatureItem.Id && x.OrderFolderId == assemblyFolder.OrderFolderId))
                        {
                            orderProductViewModel.AssemblyQuantity = product.NomenclatureItem.Quantity;
                            orderProductViewModel.AssemblyIncluded = product.AssemblyIncluded;
                        }
                    }
                }

                foreach (OrderProductViewModel orderProductViewModel in orderProductsToDelete)
                {
                    foreach (OrderProductViewModel childOrderProduct in OrderProducts.Where(x => x.ParentRecordId == orderProductViewModel.Id).ToArray())
                    {
                        if (!(childOrderProduct.IsAdditionalService && IsAdditionalServiceProductCompleted(childOrderProduct.Id)))
                        {
                            childOrderProduct.ValidationStarted -= OrderProductValidationStarted;
                            OrderProducts.Remove(childOrderProduct);
                        }
                    }

                    orderProductViewModel.ValidationStarted -= OrderProductValidationStarted;

                    OrderProducts.Remove(orderProductViewModel);
                }

                int[] existsProductIds = assemblyProducts
                    .Select(x => x.ProductId)
                    .ToArray();

                List<AssemblyViewModelResult> assemblyResults = products
                    .Where(x => !existsProductIds.Contains(x.NomenclatureItem?.Id ?? 0) && !x.IsGift)
                    .ToList();

                if (assemblyResults.Any())
                {
                    var productsFromCatalog = await WebClient.ExecuteCatalogApiRequestAsync(
                        new QueryProductByIds(
                            SelectedContractor.Id,
                            assemblyResults.Select(x => x.NomenclatureItem.Id).ToArray(),
                            OrderRules.NeedCheckGifts(SelectedSubdivision),
                            true,
                            priceIn: true,
                            cartProductIds: existsProductIds,
                            conditionGifts: assemblyFolder.OrderFolder?.TypeId != OrderFolderType.AssembledComputerRuleId));

                    NomenclatureViewItem[] nomenclatureItems = Mapper.Map<NomenclatureViewItem[]>(productsFromCatalog);

                    foreach (AssemblyViewModelResult assemblyResult in assemblyResults)
                    {
                        NomenclatureViewItem nomenclatureItem = nomenclatureItems.First(x => x.Id == assemblyResult.NomenclatureItem?.Id);

                        nomenclatureItem.Quantity = assemblyResult.NomenclatureItem.Quantity;
                        nomenclatureItem.AssemblyIncluded = assemblyResult.NomenclatureItem.AssemblyIncluded;

                        assemblyResult.NomenclatureItem = nomenclatureItem;
                    }

                    ProcessSelectedForAddingProducts(assemblyResults, assemblyFolder.OrderFolder, 1);
                }
                else
                {
                    PostProcessOrderProducts(OrderProducts.ToList(), PostProcessProductsMode.ReplaceRange);
                }

                if (OrderProducts.Any(x => x.OrderFolderId == assemblyFolder.OrderFolder.Id && x.ProductId == Constants.AssemblyServiceProductId))
                {
                    await CalculateAssemblyServiceProductPriceAsync(assemblyFolder.Id);
                }
            }
        }

        private async Task CalculateAssemblyServiceProductPriceAsync(int? orderFolderId)
        {
            OrderProductViewModel assemblyServiceProductOrderProduct = OrderProducts
                .FirstOrDefault(x => x.ProductId == Constants.AssemblyServiceProductId
                                     && x.OrderFolderId == orderFolderId);

            if (assemblyServiceProductOrderProduct != null)
            {
                decimal assemblyPrice = OrderProducts
                    .Where(x =>
                        x.AssemblyIncluded
                        && x.ProductId != Constants.AssemblyServiceProductId
                        && x.OrderFolderId == orderFolderId)
                    .Sum(x => x.PriceOut * x.AssemblyQuantity ?? 1);

                Result<CalculateAssemblyServiceProductPriceResponse> calculatePriceResult = await WebClient.ExecuteApiRequestAsync(new CalculateAssemblyServiceProductPrice(assemblyPrice));

                if (calculatePriceResult.IsSuccess)
                {
                    assemblyServiceProductOrderProduct.PriceOut = calculatePriceResult.Data.Price;
                }
                else
                {
                    Logger.LogError("Failed to calculate assembly service product price. Errors: {Errors}", calculatePriceResult.ErrorObj.GetErrorMessage());
                }
            }
        }

        private async Task PrintAssemblyReportAsync()
        {
            try
            {
                List<AssemblyReportData> reportData = OrderProducts
                    .Where(x => x.IsAssemblyOrAssembledComputerRule())
                    .GroupBy(x => x.OrderFolderId!.Value)
                    .Where(x => x.Any(y => y.Product.Id == Constants.AssemblyServiceProductId))
                    .Select(x => new { x.Key, x.First(y => y.IsVirtualProduct).Quantity, orderProducts = x })
                    .SelectMany(x => Enumerable.Range(1, x.Quantity)
                        .Select(y => new AssemblyReportData(
                            WebClient.AuthenticatedEmployee.Name,
                            order.Id,
                            AssemblyServices.FirstOrDefault(p => p.Products.Any(k => k.OrderFolderId == x.Key))?.Id,
                            x.Quantity,
                            y,
                            x.orderProducts
                                .Where(z => !z.IsVirtualProduct && !z.IsGift)
                                .GroupBy(z => z.Product.Id)
                                .Select(z => z.Select(q => new BarcodeReportData(q.Product.NameFullRu, q.Product.Id, q.AssemblyQuantity!.Value)).First())
                                .ToArray())))
                    .ToList();

                IReport report = new AssemblyReport { DataSource = reportData };

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

        private async Task GenerateAssembledComputerRuleAsync()
        {
            if (CurrentOrderProduct.Quantity > 1)
            {
                MessageFacadeService.ShowNotificationError("Генерировать товары можно только под 1 конфигурацию");
                return;
            }

            if (Bonuses?.Any(x => x.OrderProductId == CurrentOrderProduct.Id) == true)
            {
                MessageFacadeService.ShowNotificationError("Запрещено генерировать товары при наличии бонусов в конфигурации");
                return;
            }

            List<AssembledComputerRuleDto> assembledComputerRules = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputerRules());

            if (assembledComputerRules.First(x => x.ProductId == CurrentOrderProduct.ProductId).Active == false)
            {
                MessageFacadeService.ShowNotificationError("Конфигурация не активна");
                return;
            }

            if (OrderBillsVisible)
            {
                if (OrderBills == null)
                {
                    await RefreshOrderBillsCommand.ExecuteAsync(null);
                }

                if (OrderBills?.Any() != true)
                {
                    MessageFacadeService.ShowNotificationError("Запрещено генерировать конфигурации до выставления счета");
                    return;
                }
            }

            SelectComplectationGeneratorOptionsViewModel complectationGeneratorOptionsViewModel = DialogDocumentManagerService.ShowView<SelectComplectationGeneratorOptionsViewModel>(null, this);

            if (!complectationGeneratorOptionsViewModel.IsOk)
            {
                return;
            }

            ComplactationGeneratorOptionsItem generatorOptions = complectationGeneratorOptionsViewModel.Model;

            List<ProductDto> products = new List<ProductDto>();

            QueryProductByIdsDto queryProductByIdsDto =
                new QueryProductByIdsDto(new[] { CurrentOrderProduct.ProductId }, SelectedContractor.Id)
            {
                GenerateComplectation = true,
                MaxComplectationPrice = CurrentOrderProduct.PriceOut + (CurrentOrderProduct.PriceOut * (generatorOptions.OverPricePercent / 100)),
                UseWarehouse = generatorOptions.UseWarehouse,
                UsePurchases = generatorOptions.UsePurchases,
                UseTransits = generatorOptions.UseTransits
            };

            PreloaderViewModel loaderViewModel = DialogDocumentManagerService.ShowView<PreloaderViewModel>(new PreloaderParameter(QueryGeneratedAssembledComputerRulesAsync), this);

            if (!loaderViewModel.IsOk)
            {
                return;
            }

            queryProductByIdsDto = new QueryProductByIdsDto(new[] { Constants.AssemblyServiceProductId }, SelectedContractor.Id);

            List<ProductDto> assemblyServiceProduct = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(queryProductByIdsDto));

            NomenclatureViewItem assemblyServiceProductViewItem = Mapper.Map<NomenclatureViewItem>(assemblyServiceProduct.First());

            assemblyServiceProductViewItem.Quantity = CurrentOrderProduct.Quantity;

            ProductDto product = products.First();

            if (product.Complectation?.Any() == true)
            {
                OrderFolderDto orderFolder = new()
                {
                    Id = new Random().GetRandomId(),
                    Name = product.Name,
                    Quantity = CurrentOrderProduct.Quantity,
                    TypeId = OrderFolderType.AssembledComputerRuleId,
                    ProductId = product.Id,
                    Price = CurrentOrderProduct.PriceOut,
                    PriceId = CurrentOrderProduct.PriceId,
                    BonusesToCharge = CurrentOrderProduct.BonusesToCharge,
                    FreeDelivery = product.FreeDelivery
                };

                List<NomenclatureViewItem> assembledComputerRulesProducts = product.Complectation.Select(x => Mapper.Map<NomenclatureViewItem>(x)).ToList();

                assembledComputerRulesProducts.Add(assemblyServiceProductViewItem);

                ProcessSelectedForAddingProducts(assembledComputerRulesProducts, orderFolder);

                OrderProductViewModel assemblyServiceOrderProduct = GetOrderProducts().First(x => x.OrderFolderId == orderFolder.Id && x.Product.TypeId == ProductType.AssemblyServiceId);

                int index = OrderProducts.FindIndex(x => x.Id == assemblyServiceOrderProduct.Id);

                foreach (OrderProductViewModel orderProduct in GetOrderProducts().Where(x => x.IsAdditionalService && x.ParentRecordId == CurrentOrderProduct.Id).ToArray())
                {
                    orderProduct.ParentRecordId = assemblyServiceOrderProduct.Id;
                    orderProduct.OrderFolderId = orderFolder.Id;
                    orderProduct.AssemblyQuantity = 1;

                    int currentIndex = OrderProducts.FindIndex(x => x.Id == orderProduct.Id);

                    OrderProducts.Move(currentIndex, index);

                    IEnumerable<OrderProductViewModel> consumableOrderProducts = GetOrderProducts()
                        .Where(x => x.IsAdditionalServiceConsumable && x.ParentRecordId == orderProduct.Id)
                        .ToArray();

                    foreach (OrderProductViewModel consumableOrderProduct in consumableOrderProducts)
                    {
                        consumableOrderProduct.OrderFolderId = orderFolder.Id;
                        consumableOrderProduct.AssemblyQuantity = 1;

                        int currentConsumableIndex = OrderProducts.FindIndex(x => x.Id == consumableOrderProduct.Id);

                        OrderProducts.Move(currentConsumableIndex, OrderProducts.FindIndex(x => x.Id == orderProduct.Id));
                    }
                }

                OrderProducts.Remove(CurrentOrderProduct);

                MessageFacadeService.ShowNotificationInfo("Состав конфигурации является коммерческой тайной компании");
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Не получилось найти товары под конфигурацию");
            }

            async Task<IEnumerable<ValidationResultItem>> QueryGeneratedAssembledComputerRulesAsync(IProgress<string> progress)
            {
                progress.Report("Генерация товаров под конфигурацию...");

                products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(queryProductByIdsDto));

                return Array.Empty<ValidationResultItem>();
            }
        }

        private async Task<bool> CheckCompatibilityAsync(OrderProductViewModel orderProduct)
        {
            return await CheckCompatibilityMultipleAsync(orderProduct);
        }

        private async Task<bool> CheckCompatibilityMultipleAsync(params OrderProductViewModel[] orderProducts)
        {
            try
            {
                IReadOnlyCollection<ProductQuantityDto> productQuantities = OrderProducts
                    .Where(x => x.OrderFolderId == orderProducts.First().OrderFolderId
                        && x.Product.TypeId != ProductType.AssemblyServiceId
                        && !x.IsVirtualProduct
                        && !x.IsGift
                        && !x.IsAdditionalService
                        && x.AssemblyIncluded)
                    .GroupBy(x => x.Product.Id)
                    .Select(x => new ProductQuantityDto(x.Key, x.Sum(y => y.Quantity)))
                    .ToArray();

                CheckCompatibilityResponse response = await WebClient.ExecuteCatalogApiRequestAsync(new CheckProductCompatibility(productQuantities, orderProducts.First().OrderFolder?.ProductId));

                var validationResults = response.GetValidationResults().ToArray();

                if (validationResults.Any())
                {
                    ShowValidationResultView("Валидация при проверке сборки", validationResults.Select(x => new ValidationResultItem(x.Message, x.NotificationImageId)));
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Комплектующие совместимы");

                    return true;
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при проверке сборки");
                ShowValidationResultView("Ошибки при проверке сборки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to check compatibility");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при проверке сборки");
                Logger.LogError(exception, "Error while checking compatibility");
            }

            return false;
        }

        private async Task PaymentControlAsync()
        {
            bool isOk = false;

            const string waitingText = "Операция все еще выполяется на стороне банка. Попробуйте открыть заказ позже";

            ExternalPaymentViewItem externalPayment = ExternalPayments.First(x => x.PaymentStateId == PaymentState.Confirmed.Id);

            await LockableOperationProcessor.DoActionAsync(Id, x =>
            {
                OrderPaymentControlViewModel viewModel = DialogDocumentManagerService.ShowView<OrderPaymentControlViewModel>(
                    new OrderPaymentControlParameter(
                        x.Id,
                        externalPayment,
                        x.SubdivisionId,
                        x.GetPrices().ToPay.Uah,
                        externalPayment.HoldedAmount!.Value,
                        ExternalPayments.Where(z => z.ParentId == externalPayment.Id).Sum(z => z.HoldedAmount ?? 0)),
                    this);

                isOk = viewModel.IsOk;
            });

            if (isOk)
            {
                if (externalPayment.Payment.Id == Payment.NovaPayId)
                {
                    DialogDocumentManagerService.ShowView<PreloaderViewModel>(
                        new PreloaderParameter(
                            async x =>
                            {
                                x.Report("Ожидание коллбека от NovaPay...");
                                await Task.Delay(NovaPayOptions.CallBackWaitSeconds * 1000);

                                OrderDto orderDto = await WebClient.ExecuteApiRequestAsync(new QueryOrder(Id));

                                x.Report("Проверка статуса оплаты...");

                                if (orderDto.ExternalPayments.Any(z => z.PaymentStateId == PaymentState.Processing.Id && z.PaymentId == Payment.NovaPayId))
                                {
                                    Application.Current.Dispatcher.BeginInvoke(() => MessageFacadeService.ShowMessageBoxWarning(waitingText));
                                }

                                return default;
                            },
                            null,
                            null),
                        this);
                }
                else if (externalPayment.Payment.Id == Payment.MonoPayId)
                {
                    MessageFacadeService.ShowMessageBoxWarning(waitingText);
                }

                Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
            }
        }

        private async Task PrintChequeAsync()
        {
            int cashboxId;

            if (order.CarryId == CarryType.PickupId)
            {
                Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                    ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                    "Определении настроек РРО",
                    null,
                    this,
                    true);

                if (!clientResult.IsSuccess)
                {
                    return;
                }

                CashboxDto cashbox =
                    await WebClient.ExecuteApiRequestAsync(new QueryCashbox(clientResult.Data.Settings.CashboxId));

                if (cashbox.Session?.Closed != false)
                {
                    MessageFacadeService.ShowNotificationWarning("Смена закрыта");
                    return;
                }

                cashboxId = clientResult.Data.Settings.CashboxId;
            }
            else
            {
                if (order.LegalEntityId == null)
                {
                    MessageFacadeService.ShowNotificationError("В заказе не заполнено юр. лицо");
                    return;
                }

                var legalEntity = await WebClient.ExecuteApiRequestAsync(new QueryLegalEntity(order.LegalEntityId.Value));

                if (legalEntity.FiscalCashboxId == null)
                {
                    MessageFacadeService.ShowNotificationError("В юр. лице заказа не заполнена фискальная каса");
                    return;
                }

                cashboxId = legalEntity.FiscalCashboxId.Value;
            }

            await PrintOrderOnFiscalRegistrarAsync(order.Id, cashboxId, order.FiscalId);
        }

        private bool CanPrintAct()
        {
            return HasKeepGuestProducts() && AllGuestProductsHasCharacteristics();
        }

        private void ShowUklonOrder()
        {
            DialogDocumentManagerService.ShowView<UklonOrderViewModel>(new UklonOrderParameter() { OrderId = order.Id }, this);
        }

        private bool CanShowUklonOrder()
        {
            return order != null &&
                   ((order.StateId == OrderStatus.Packed.Id && order.GetLeftToPay().Uah == 0) || order.StateId == OrderStatus.Done.Id)
                   && order.CarryId == CarryType.UklonId;
        }

        private async Task PrintActIncomeAsync(bool fromForms)
        {
            if (!AllGuestProductsHasCharacteristics())
            {
                if (fromForms)
                {
                    MessageFacadeService.ShowMessageBoxWarning("Не всем гостевым товарам заполнены характеристики");
                }

                return;
            }

            AdditionalServiceProductClientProductReportDataDto data = await WebClient.ExecuteApiRequestAsync(new QueryActAdditionalServiceProductReportData(order.Id));

            if (data.GuestProducts?.Any() != true)
            {
                MessageFacadeService.ShowMessageBoxWarning("В заказе отсутствуют гостевые товары,\nкоторые останутся у нас");
                return;
            }

            AdditionalServiceProductClientProductsReportData reportData = new(DateTime.Now, data.FullNameClient, string.Empty, data.PlaceName, WebClient.AuthenticatedEmployee.Name, true);

            reportData.SetGuestProducs(data.GuestProducts?.Select(x => Mapper.Map<GuestProductReportData>(x)).ToArray());
            reportData.SetNumber(order.Id);

            IReport report = new ActIncomeClientProductReport { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }

        private async Task PrintActOutcomeAsync()
        {
            if (AdditionalServiceProducts.Any(x => (x.ProductTypeId == ProductType.GuestProductId || x.ConsumableProducts?.Any(y => y.ProductTypeId == ProductType.GuestProductId) == true)
                                                                        && x.StateId != AdditionalServiceProductState.CompletedId))
            {
                MessageFacadeService.ShowMessageBoxWarning("Услуги с гостевыми товарами не завершены");
                return;
            }

            AdditionalServiceProductClientProductReportDataDto data = await WebClient.ExecuteApiRequestAsync(new QueryActAdditionalServiceProductReportData(order.Id));

            if (data.GuestProducts?.Any() != true)
            {
                MessageFacadeService.ShowMessageBoxWarning("В заказе отсутствуют гостевые товары,\nкоторые оставались у нас");
                return;
            }

            AdditionalServiceProductClientProductsReportData reportData = new(DateTime.Now, data.FullNameClient, string.Empty, data.PlaceName, WebClient.AuthenticatedEmployee.Name);

            reportData.SetGuestProducs(data.GuestProducts?.Select(x => Mapper.Map<GuestProductReportData>(x)).ToArray());
            reportData.SetNumber(order.Id);

            IReport report = new ActOutcomeClientProductReport { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }

        private async Task PrintOrderPaymentFiscalChequeAsync(int id, decimal amount, int cashboxId, int paymentId, string fiscalId)
        {
            if (!(paymentId is Payment.TerminalId or Payment.CashId))
            {
                MessageFacadeService.ShowNotificationWarning("Недопустимый способ оплаты");
                return;
            }

            CashboxDto cashbox = await WebClient.ExecuteApiRequestAsync(new QueryCashbox(cashboxId));

            if (!cashbox.LegalEntity.White)
            {
                MessageFacadeService.ShowNotificationWarning("Нет необходимости печати фискального чека для используемого юр. лица в заказе");
                return;
            }

            if (cashbox.Session?.Closed != false)
            {
                MessageFacadeService.ShowNotificationWarning("Смена закрыта");
                return;
            }

            Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                "Определении настроек РРО",
                null,
                this,
                true);

            if (!clientResult.IsSuccess)
            {
                return;
            }

            if (paymentId == Payment.CashId && clientResult.Data.Settings.CashboxId != cashboxId)
            {
                MessageFacadeService.ShowNotificationWarning("Касса оплаты не равна кассе в настройках РРО");
                return;
            }

            if (string.IsNullOrEmpty(fiscalId))
            {
                IsLongOperationInProgress = true;

                await CreateOrderPaymentOnFiscalRegistrarAsync(id, amount, cashbox, paymentId, clientResult.Data);

                IsLongOperationInProgress = false;
            }
            else
            {
                await PrintOrderPaymentOnFiscalRegistrarAsync(id, cashboxId, fiscalId);
            }
        }

        private async Task PrintOrderPaymentOnFiscalRegistrarAsync(int id, int cashboxId, string fiscalId)
        {
            Result resultSentRroCheck = await RroPrintHelper.SentCheckAsync(fiscalId, Phone, Email, cashboxId, this);

            if (resultSentRroCheck.IsSuccess)
            {
                Result<OrderPaymentDto> result = await WebClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(id, fiscalId, true, cashboxId));

                Messenger.Send(new OrderPaymentMessage(result.Data, MessageType.Changed));
            }
        }

        private async Task PrintOrderOnFiscalRegistrarAsync(int id, int cashboxId, string fiscalId)
        {
            Result resultSentRroCheck = await RroPrintHelper.SentCheckAsync(fiscalId, order.Phone, order.Email, cashboxId, this, true);

            if (resultSentRroCheck.IsSuccess)
            {
                await WebClient.ExecuteApiRequestAsync(new ConfirmOrderOnFiscalRegistrar(id, fiscalId, true));
            }
        }

        private async Task CreateOrderPaymentOnFiscalRegistrarAsync(int id, decimal amount, CashboxDto cashbox, int paymentId, IFiscalRegistrarClient fiscalRegistrarClient)
        {
            Payment payment = Dictionaries.GetItemById<Payment>(order.PaymentId);

            if (!payment.Fiscal)
            {
                await WebClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(id, string.Empty, true, cashbox.Id));

                return;
            }

            SellRequest sellRequest = new SellRequest(
                Guid.NewGuid().ToString(),
                cashbox.Id,
                $"Касир: {WebClient.AuthenticatedEmployee.ShortName}",
                $"Замовлення №{order.Id}",
                order.Id,
                new[] { new SellProductRequest(FiscalRegistrarConstants.PrepaymentProductId, "Передплата", null, amount, 1, Telemart.Common.Dictionaries.ProductType.Product) },
                new[]
                {
                    new SellPaymentRequest(FiscalPaymentType.GetByPaymentId(paymentId), amount)
                    {
                        CheckNumber = null,
                        Rrn = null,
                        TerminalId = null,
                        MerchantId = null,
                        AuthCode = null,
                        Pan = null,
                        IssuerName = null
                    }
                },
                Array.Empty<string>(),
                null);

            await WebClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(id, sellRequest.OperationId, false, cashbox.Id));

            try
            {
                await fiscalRegistrarClient.SellAsync(sellRequest, default);
            }
            catch { }

            Result<QueryReceiptStatusResponse> statusResult = await fiscalRegistrarClient.GetReceiptStatusAsync(sellRequest.CashboxId, sellRequest.OperationId, default);

            if (!statusResult.IsSuccess || statusResult.Data.Status == "ERROR")
            {
                MessageFacadeService.ShowNotificationInfo("Ошибка при проверке статуса чека");

                await WebClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(id, string.Empty, payment.Fiscal, cashbox.Id));

                return;
            }

            Result<OrderPaymentDto> result = await WebClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(id, sellRequest.OperationId, true, cashbox.Id));

            Messenger.Send(new OrderPaymentMessage(result.Data, MessageType.Changed));

            await WebClient.ExecuteApiRequestAsync(new CreateFiscalDocument(sellRequest.ToDocument(cashbox.Id, sellRequest.OperationId, order.Id, Entity.OrderId)));

            await PrintOrderPaymentOnFiscalRegistrarAsync(id, cashbox.Id, sellRequest.OperationId);
        }

        private void CurrentProductChanged()
        {
            LoadValues(CurrentOrderProduct);

            if (CurrentOrderProduct != null)
            {
                SetCurrentProductAssemblyServices();
                SetCurrentProductAdditionalServiceProducts();
                SetCurrentProductPromos();
                RecalculateCanChangePriceIdForCurrentProduct();
            }
        }

        private void SetCurrentProductAssemblyServices()
        {
            ProductAssemblyServices = AssemblyServices
                 .Where(x => x.Products.Any(z => z.OrderProductId == CurrentOrderProduct.Id))
                 .ToReadOnlyObservableCollection();
        }

        private void SetCurrentProductPromos()
        {
            ProductPromos = ProductsPromoCodes?.TryGetValue(CurrentOrderProduct.ProductId, out var promoCodes) == true
                ? promoCodes.ToReadOnlyObservableCollection()
                : null;
        }

        private void SetCurrentProductAdditionalServiceProducts()
        {
            IEnumerable<int> childOrderProductIds = OrderProducts
                .Where(z => z.ParentRecordId == CurrentOrderProduct?.Id)
                .Select(q => q.Id);

            ProductAdditionalServiceProducts = AdditionalServiceProducts?
                   .Where(x => x.OrderProductId.HasValue && childOrderProductIds.Contains(x.OrderProductId.Value))
                   .ToReadOnlyObservableCollection();
        }

        private async Task AddBonusAsync()
        {
            if (order.CustomerId == null)
            {
                return;
            }

            List<CustomerBonusDto> customerBonuses = await WebClient.ExecuteApiRequestAsync(new QueryCustomerBonuses(order.CustomerId));

            List<BonusType> bonusTypes = Dictionaries.GetItems<BonusType>()
                .Where(x => customerBonuses.Any(y => y.BonusTypeId == x.Id) && !Bonuses?.Any(y => y.BonusTypeId == x.Id) == true)
                .ToList();

            if (!bonusTypes.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нет доступных бонусов");
                return;
            }

            await LockableOperationProcessor.DoOperationAsync(
                Id,
                async lockedOrder =>
                {
                    SelectBonusTypeParameter parameter = new SelectBonusTypeParameter(bonusTypes);

                    SelectBonusTypeViewModel bonusTypeViewModel = DialogDocumentManagerService.ShowView<SelectBonusTypeViewModel>(parameter, this);

                    if (!bonusTypeViewModel.IsOk)
                    {
                        return;
                    }

                    int bonusTypeId = bonusTypeViewModel.SelectedBonus?.Id ?? 0;

                    CustomerBonusDto selectCustomerBonus = customerBonuses.First(x => x.BonusTypeId == bonusTypeId);

                    if (bonusTypeViewModel.SelectedBonus!.Id == BonusType.TradeInPointsId)
                    {
                        BonusType tradeInBonusType = bonusTypes.First(x => x.Id == bonusTypeId);

                        if (OrderPayments?
                            .Where(
                                x => x.PaymentId == tradeInBonusType.PaymentId
                                     && x.CashboxId != tradeInBonusType.CashboxId)
                            .Sum(x => x.Sign * x.Amount) > 0)
                        {
                            MessageFacadeService.ShowNotificationWarning("Запрещено добавлять бонусы на разные кассы");
                        }

                        AddBonusesParameter addBonusesParameter = new AddBonusesParameter(
                            order.Id,
                            (int)OrderPaymentViewModel.ToPayItem.Uah,
                            selectCustomerBonus.Quantity,
                            tradeInBonusType.Name);

                        AddBonusesViewModel addBonusesViewModel = DialogDocumentManagerService.ShowView<AddBonusesViewModel>(addBonusesParameter, this);

                        if (!addBonusesViewModel.IsOk)
                        {
                            return;
                        }

                        CreateOrderPayment tradeInPaymentRequest = new CreateOrderPayment(
                            order.Id,
                            Currency.UahId,
                            tradeInBonusType.PaymentId!.Value,
                            tradeInBonusType.CashboxId,
                            addBonusesViewModel.SelectedQuantity,
                            DateTime.Today,
                            null,
                            prepayment: true);

                        Result<OrderPaymentResultDto> tradeInPaymentResult = await ErrorHandler.HandleErrorsAsync(
                            _ => WebClient.ExecuteApiRequestAsync(tradeInPaymentRequest),
                            "внесении Trade-In оплаты",
                            null,
                            this,
                            true);

                        if (tradeInPaymentResult?.IsSuccess == true)
                        {
                            ProcessNewPayment(tradeInPaymentResult);
                        }
                    }
                    else
                    {
                        List<BonusConfirmationViewItem> bonusConfirmationItems = lockedOrder.Products
                            .Where(x => x.BonusTypeId == bonusTypeId && x.OrderPromoCodeId == null)
                            .SelectMany(x => Enumerable.Range(1, x.Quantity)
                                .Select(_ => new BonusConfirmationViewItem
                                {
                                    MaxQuantity = Math.Min((int)(x.PriceOut - 1) <= 0 ? 0 : (int)(x.PriceOut - 1), x.MaxBonusesToUse ?? 0),
                                    OrderProductId = x.Id,
                                    ProductId = x.Product.Id,
                                    Price = x.Price,
                                    PriceCurrent = x.PriceOut,
                                    ProductName = x.Product.GetLocalName(LocalizableNameType.Ukr),
                                }))
                            .ToList();

                        if (!bonusConfirmationItems.Any())
                        {
                            MessageFacadeService.ShowNotificationWarning("Отсутствуют товары, к которым можно применить бонусы");
                            return;
                        }

                        BonusConfirmationParameter bonusConfirmationParameter = new BonusConfirmationParameter(selectCustomerBonus.Quantity, bonusConfirmationItems);

                        BonusConfirmationViewModel bonusConfirmationViewModel = SizeableDialogDocumentManagerService.ShowView<BonusConfirmationViewModel>(bonusConfirmationParameter, this);

                        if (!bonusConfirmationViewModel.IsOk)
                        {
                            return;
                        }

                        await ProcessBonusesAsync(Id, bonusTypeId, bonusConfirmationViewModel.Items);
                    }
                });
        }

        private Task EditBonusAsync(OrderBonusSummaryViewItem item)
        {
            if (order.CustomerId == null)
            {
                return Task.CompletedTask;
            }

            if (item.BonusTypeId == BonusType.TradeInPointsId)
            {
                MessageFacadeService.ShowNotificationError("Запрещено редактировать Trade-In бонусы");
                return Task.CompletedTask;
            }

            return LockableOperationProcessor.DoOperationAsync(
               Id,
               async lockedOrder =>
               {
                   List<CustomerBonusDto> customerBonuses = await WebClient.ExecuteApiRequestAsync(new QueryCustomerBonuses(order.CustomerId));

                   Dictionary<int, Stack<OrderProductBonusDto>> bonusesDictionary = Bonuses
                       .Where(x => x.BonusTypeId == item.BonusTypeId)
                       .GroupBy(x => x.OrderProductId)
                       .ToDictionary(x => x.Key, x => new Stack<OrderProductBonusDto>(x));

                   List<BonusConfirmationViewItem> bonusConfirmationItems = lockedOrder.Products
                       .Where(x => x.BonusTypeId == item.BonusTypeId && x.OrderPromoCodeId == null)
                       .SelectMany(x => Enumerable.Range(1, x.Quantity)
                           .Select(_ => MapBonuses(x, bonusesDictionary)))
                       .ToList();

                   CustomerBonusDto selectCustomerBonus = customerBonuses.FirstOrDefault(x => x.BonusTypeId == item.BonusTypeId);

                   BonusConfirmationParameter bonusConfirmationParameter = new BonusConfirmationParameter(selectCustomerBonus?.Quantity ?? 0, bonusConfirmationItems);

                   BonusConfirmationViewModel bonusConfirmationViewModel = SizeableDialogDocumentManagerService.ShowView<BonusConfirmationViewModel>(bonusConfirmationParameter, this);

                   if (!bonusConfirmationViewModel.IsOk)
                   {
                       return;
                   }

                   await ProcessBonusesAsync(Id, item.BonusTypeId, bonusConfirmationViewModel.Items);
               });
        }

        private Task RemoveBonusAsync(OrderBonusSummaryViewItem item)
        {
            BonusType bonusType = Dictionaries.GetItemById<BonusType>(item.BonusTypeId);

            if (!MessageFacadeService.Confirm($"Вы действительно хотите удалить бонусы \"{bonusType.Name}\" из заказа и вернуть их клиенту?"))
            {
                return Task.CompletedTask;
            }

            return LockableOperationProcessor.DoOperationAsync(
                Id,
                _ => item.BonusTypeId == BonusType.TradeInPointsId
                    ? ProcessTradeInBonusesAsync(bonusType, item.Quantity)
                    : ProcessBonusesAsync(Id, item.BonusTypeId, Array.Empty<BonusConfirmationViewItem>()));
        }

        private async Task ProcessTradeInBonusesAsync(BonusType bonusType, int quantity)
        {
            HashSet<int> cashboxIds = OrderPayments.Where(x => x.PaymentId == bonusType.PaymentId)
                .Select(x => x.CashboxId)
                .ToHashSet();

            if (cashboxIds.Count == 0)
            {
                MessageFacadeService.ShowNotificationWarning("Не найдены оплаты бонусами");
                return;
            }

            if (cashboxIds.Count > 1)
            {
                MessageFacadeService.ShowNotificationWarning("Оплаты бонусами производились на разные кассы");
                return;
            }

            CreateOrderWithdraw gatewayRequest = new CreateOrderWithdraw(
                Id,
                cashboxIds.First(),
                bonusType.PaymentId,
                CurrencyTypeIds.UahId,
                quantity,
                "[Система] Возврат бонусов по виртуальной кассе",
                true);

            Result<OrderWithdrawResultDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(gatewayRequest),
                "удалении бонусов",
                "Бонусы удалены",
                this,
                true);

            MessageFacadeService.ShowNotificationInfo($"Бонусы из заказа №{Id} успешно удалены");

            Messenger.Send(new OrderPaymentMessage(result.Data.OrderPayment, MessageType.Added));
            Messenger.Send(new RefundMessage(result.Data.MoneyRefund, MessageType.Added));
        }

        private async Task AddCustomAssemblyAsync()
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Код",
                "Укажите код товара сборки",
                "^[0-9]{1,9}$",
                "Код товара сборки состоит из цифр длиною до 9 символов");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (!fromUserViewModel.IsOk)
            {
                return;
            }

            if (fromUserViewModel.Content.Length > 12 || !int.TryParse(fromUserViewModel.Content, out int assemblyProductId) || assemblyProductId <= 0)
            {
                return;
            }

            QueryProductByIdsDto queryProductByIdsDto = new QueryProductByIdsDto(new[] { assemblyProductId }, SelectedContractor.Id)
            {
                Complectation = true,
                Gifts = true
            };

            List<ProductDto> products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(queryProductByIdsDto));

            if (products?.Any() != true || products.All(x => x.Complectation?.Any() != true))
            {
                MessageFacadeService.ShowNotificationWarning($"Сборка с кодом товара '{assemblyProductId}' не найдена");
                return;
            }

            ProductDto product = products.First();

            OrderFolderDto orderFolder = new OrderFolderDto()
            {
                Id = new Random().GetRandomId(),
                Name = EntityLocalіzerExtensions.GetLacalString(product.Name, product.NameUkr, product.NameEn, LocalizableNameType.Ukr),
                Quantity = 1,
                TypeId = OrderFolderType.AssemblyServiceId,
                ProductId = assemblyProductId,
                FreeDelivery = product.FreeDelivery
            };

            List<NomenclatureViewItem> assemblyProducts = product.Complectation
                .Select(x => Mapper.Map<NomenclatureViewItem>(x))
                .ToList();

            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            foreach (NomenclatureViewItem assemblyProduct in assemblyProducts)
            {
                CategoryDto category = categories.First(x => x.Id == assemblyProduct.ParentCategoryId);

                assemblyProduct.AssemblyIncluded = category.TypeId == CategoryType.PcComponents.Id || assemblyProduct.TypeId == ProductType.AssemblyServiceId;
            }

            ProcessSelectedForAddingProducts(assemblyProducts, orderFolder);

            MessageFacadeService.ShowNotificationInfo("Сборка добавлена успешно");
        }

        private async Task HandleRowDoubleClickAsync(RowDoubleClickInfo e)
        {
            OrderProductViewModel orderProduct = (OrderProductViewModel)e.Data;

            if (e.FieldName.Equals(nameof(OrderProductViewModel.AnyAdditionalServices)))
            {
                var canEdit = await CanEditAdditionalServiceAsync(orderProduct.Id);

                if (canEdit is not null)
                {
                    if (orderProduct.AnyAdditionalServiceProvideProducts)
                    {
                        await SelectProductsAdditionalServiceConsumableAsync(orderProduct);
                    }
                    else if (orderProduct.AnyAdditionalServices)
                    {
                        SelectAdditionalServices(orderProduct);
                    }
                }
            }
            else if ((e.FieldName.Equals(nameof(OrderProductViewModel.Source)) || e.FieldName.Equals(nameof(OrderProductViewModel.IsGuestProduct)))
                     && orderProduct.Source.Id == OrderProductSourceType.GuestId)
            {
                ReceiveGuestProductCommand.Execute(orderProduct);
            }
        }

        private void HandlePromoRowDoubleClick(RowDoubleClickInfo e)
        {
            PromoCodeDto promo = (PromoCodeDto)e.Data;
            SizeableDialogDocumentManagerService.ShowView<PromoCodeViewModel>(new PromoCodeParameter(promo.Id), this);
        }

        private async Task ProcessBonusesAsync(int orderId, int bonusTypeId, IEnumerable<BonusConfirmationViewItem> items)
        {
            try
            {
                IReadOnlyCollection<OrderBonusSaveDto> bonuses = items.Select(x => new OrderBonusSaveDto
                {
                    BonusTypeId = bonusTypeId,
                    OrderProductId = x.OrderProductId,
                    Id = x.Id,
                    Quantity = x.Quantity
                }).ToArray();

                Prices toPay = OrderPaymentInfoViewModel.CalcToPayAfterUseBonuses(bonuses.Sum(x => x.Quantity), this);

                if (toPay.Uah < 0 || toPay.Usd < 0)
                {
                    MessageFacadeService.ShowNotificationError("Сумма 'К оплате' не может стать отрицательной");
                    return;
                }

                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(new ProcessOrderBonuses(orderId, bonusTypeId, bonuses));

                SetProducts(result.Data.Products, result.Data.Folders);
                SetBonuses(result.Data.Bonuses);

                RefreshPaymentInfo();

                MessageFacadeService.ShowNotificationInfo("Бонусы успешно обработаны");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при применении бонусов к заказу");
                ShowValidationResultView("Ошибки при применении бонусов", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to process order bonuses");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при применении бонусов к заказу");
                Logger.LogError(exception, "Error while processing order bonuses");
            }
        }

        private void OnComplaintMessage(ComplaintMessage message)
        {
            ComplaintDto dto = message.Entity;

            if (dto.OrderId == Id)
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

            ComplaintsCount = Complaints?.Count;
            NewComplaintsCount = Complaints?.Count(x => x.State == ComplaintState.New);
        }

        private void OnServiceRequestMessage(ServiceRequestMessage message)
        {
            ServiceRequestDto dto = message.Entity;

            if (dto.OrderId == Id)
            {
                switch (message.MessageType)
                {
                    case MessageType.Added:
                        ServiceRequests?.Add(Mapper.Map<ServiceRequestViewItem>(dto));
                        break;
                    case MessageType.Changed:
                        ServiceRequests?.DoActionWithItem(x => x.Id == dto.Id, x => Mapper.Map(dto, x));
                        break;
                }
            }

            ServiceRequestsCount = ServiceRequests?.Count;
        }

        private void OnOrderBillMessage(OrderBillMessage message)
        {
            OrderBillDto dto = message.Entity;

            if (dto.OrderId == Id)
            {
                switch (message.MessageType)
                {
                    case MessageType.Added:
                        OrderBills?.Add(Mapper.Map<OrderBillViewItem>(dto));
                        break;
                    case MessageType.Changed:
                        OrderBills?.DoActionWithItem(x => x.Id == dto.Id, x => Mapper.Map(dto, x));
                        break;
                }
            }
        }

        private void OnOrderPaymentMessage(OrderPaymentMessage msg)
        {
            if (msg.Entity.OrderId == Id)
            {
                switch (msg.MessageType)
                {
                    case MessageType.Added:
                        OrderPayments.Add(Mapper.Map<OrderPaymentRecordViewItem>(msg.Entity));
                        break;
                    case MessageType.Changed:
                        OrderPayments.DoActionWithItem(x => x.Id == msg.Entity.Id, x => Mapper.Map(msg.Entity, x));
                        break;
                }

                OrderPaymentViewModel.CalcPaymentInfo(this);
                RefreshBonusSummary();
            }
        }

        private async Task UpdatePurchaseSourceAsync(OrderProductViewModel orderProduct, PurchaseSourceSaveDto source)
        {
            IsLongOperationInProgress = true;

            try
            {
                PurchaseDto purchase = await WebClient.ExecuteApiRequestAsync(new UpdatePurchaseSource(orderProduct.Id, source));

                orderProduct.Source = Dictionaries.GetOrderProductSource(
                    purchase.ProductSourceId,
                    purchase.ProductWarehouseId,
                    purchase.ProductSourceText,
                    purchase.ProductSourceDate);

                MessageFacadeService.ShowNotificationInfo("Источник успешно установлен");

                Messenger.Send(new OrderEditReopenViewMessage(order.Id, IsAddMode));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(exception.Args.Error.GetErrorMessage());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to set purchase source");
                MessageFacadeService.ShowNotificationError("Ошибка установки источника");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private async Task RemovePurchaseSourceAsync(OrderProductViewModel orderProduct)
        {
            IsLongOperationInProgress = true;

            try
            {
                List<ValidationResultItem> validationResults = new List<ValidationResultItem>();

                if (orderProduct.IsVirtualProduct)
                {
                    foreach (OrderProductViewModel orderProductViewModel in OrderProducts.Where(x => !x.IsVirtualProduct && x.OrderFolderId == orderProduct.OrderFolderId))
                    {
                        validationResults.AddRange((await RemovePurchaseSourceInternalAsync(orderProductViewModel)).Select(x => new ValidationResultItem($"{orderProductViewModel.Product.NameFullRu}: {x.Message}", x.IsError)));
                    }
                }
                else
                {
                    validationResults.AddRange(await RemovePurchaseSourceInternalAsync(orderProduct));
                }

                if (validationResults.Any())
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при удалении источника");

                    ShowValidationResultView("Ошибки при удалении источника", validationResults);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Источник успешно удaлен");
                }
            }
            finally
            {
                IsLongOperationInProgress = false;
            }

            async Task<IEnumerable<ValidationResultItem>> RemovePurchaseSourceInternalAsync(OrderProductViewModel orderProductInternal)
            {
                try
                {
                    Result<PurchaseDto> result = await WebClient.ExecuteApiRequestAsync(new RemovePurchaseSource(orderProductInternal.Id));

                    PurchaseDto purchase = result.Data;

                    orderProductInternal.Source = Dictionaries.GetOrderProductSource(
                        purchase.ProductSourceId,
                        purchase.ProductWarehouseId,
                        purchase.ProductSourceText,
                        purchase.ProductSourceDate);
                }
                catch (UnexpectedSatusException exception)
                {
                    return exception.GetErrorItems().Select(x => new ValidationResultItem(x.Message, x.IsError));
                }
                catch (UnexpectedErrorException exception)
                {
                    Logger.LogError(exception, "Failed to remove purchase source");

                    return new[] { new ValidationResultItem(Resources.ServerUnavailable, true) };
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Error while removing purchase source");
                }

                return Array.Empty<ValidationResultItem>();
            }
        }

        private void EditOrderPayment(OrderPaymentRecordViewItem orderPayment)
        {
            if (orderPayment.Sign > 0)
            {
                DialogDocumentManagerService.ShowView<EditOrderPaymentViewModel>(new object[] { orderPayment, order.CreatedOn }, this);
            }
            else if (orderPayment.RefundId.HasValue)
            {
                Messenger.Send(new RefundViewMessage(orderPayment.RefundId.Value));
            }
        }

        private Task EditInfoAsync()
        {
            return LockableOperationProcessor.DoActionAsync(
                Id,
                lockedOrder =>
                {
                    OrderEditInfoViewModel viewModel = DialogDocumentManagerService.ShowView<OrderEditInfoViewModel>(lockedOrder, this);

                    if (viewModel.IsOk)
                    {
                        order.EmployeeComment = viewModel.Comment;
                        order.ReceiveTime = viewModel.ReceiveTime;

                        ReceiveTime = viewModel.ReceiveTime;
                        EmployeeComment = viewModel.Comment;
                    }
                });
        }

        private async IAsyncEnumerable<ValidationResultItem> CanSaveAsync(OrderViewModel x)
        {
            if (SelectedCarryType != null && string.IsNullOrWhiteSpace(x.DeliveryData.PlaceId))
            {
                yield return new ValidationResultItem("Адрес должен быть заполнен", x.State != OrderStatus.Received);
            }

            if (x.OrderPaymentViewModel.ToPayItem.Uah < 0 || x.OrderPaymentViewModel.ToPayItem.Usd < 0)
            {
                yield return new ValidationResultItem("Сумма к оплате не может быть отрицательной", true);
            }

            if (x.OrderProducts.Any(y => IDataErrorInfoHelper.HasErrors(y)))
            {
                yield return new ValidationResultItem("Найдены ошибки валидации товаров", true);
            }

            if ((order.StateId == OrderStatus.Packed.Id || order.StateId == OrderStatus.Confirmed.Id)
                && SelectedAdditionalServiceWarehouse != null
                && order.AdditionalServiceWarehouseId != SelectedAdditionalServiceWarehouse.Id
                && SelectedAdditionalServiceWarehouse.Id != SelectedWarehouse.Id)
            {
                yield return new ValidationResultItem("Выбранный склад услуг не равен складу выдачи заказа", true);
            }

            int[] requireProductsToProvideAdditionalServiceProductId = AdditionalServiceProducts
                .Where(y => y.RequireProductsToProvide)
                .Select(z => z.AdditionalServiceProductId)
                .ToArray();

            if (x.OrderProducts.Any(y => requireProductsToProvideAdditionalServiceProductId.Contains(y.ProductId)))
            {
                foreach (OrderProductViewModel orderProduct in x.OrderProducts.Where(y => requireProductsToProvideAdditionalServiceProductId.Contains(y.ProductId)))
                {
                    if (x.OrderProducts.Any(y => y.ParentRecordId == orderProduct.Id) != true)
                    {
                        string nameProduct = orderProduct.Product.NameFullRu;

                        yield return new ValidationResultItem($"Для услуги {nameProduct} обязательно добавление товара для оказания услуги", false);
                    }
                }
            }
        }

        private int? GetQuantityAssembliesInAssemblyModule(ICollection<int> orderProductIds, int? orderFolderId)
        {
            int? quantity = AssemblyServices?.Count(x => orderProductIds.Contains(x.OrderProductId ?? 0) || x.Products.Any(y => orderProductIds.Contains(y.OrderProductId ?? 0) && y.OrderFolderId == orderFolderId));
            return quantity;
        }

        private int? GetCompletedAdditionalServiceProducts(int orderProductId)
        {
            int? quantity = AdditionalServiceProducts.Count(m => m.OrderProductId == orderProductId && m.StateId == AdditionalServiceProductState.CompletedId);
            return quantity;
        }

        private void SelectAdditionalServices(OrderProductViewModel orderProduct)
        {
            int[] additionalServiceProductTypeIds = null;

            if (orderProduct.ParentRecordId.HasValue)
            {
                var parent = OrderProducts.FirstOrDefault(x => x.Id == orderProduct.ParentRecordId.Value);

                if (parent.AdditionalServiceId.HasValue && AdditionalServiceProducts.FirstOrDefault(x => x.OrderProductId == parent.Id && (x.StateId == AdditionalServiceProductState.CompletedId || x.StateId == AdditionalServiceProductState.DoingId)) != null)
                {
                    additionalServiceProductTypeIds = new[] { ProductType.CertificateId };
                }
            }

            SelectAdditionalServicesParameter selectAdditionalServicesParameter = new SelectAdditionalServicesParameter(
                orderProduct.ProductId,
                orderProduct.Product.GetLocalName(LocalizableNameType.Ukr),
                orderProduct.PriceOut,
                SelectedContractor.Id,
                order.PaymentId,
                order.ExternalPayments,
                IsLockedByCurrentUserAndEditingAllowed,
                additionalServiceProductTypeIds);

            SelectAdditionalServicesViewModel viewModel = DialogDocumentManagerService.ShowView<SelectAdditionalServicesViewModel>(selectAdditionalServicesParameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            ProcessSelectedForAddingProducts(viewModel.Products, orderProduct.OrderFolder, orderProduct.AssemblyQuantity ?? 1, orderProduct.Id);
        }

        private async Task SelectProductsAdditionalServiceConsumableAsync(OrderProductViewModel orderProduct)
        {
            if (orderProduct.AdditionalServiceId == null || !IsLockedByCurrentUserAndEditingAllowed)
            {
                return;
            }

            AdditionalServiceDto additionalServiceDto =
                await WebClient.ExecuteApiRequestAsync(new QueryAdditionalService(orderProduct.AdditionalServiceId.Value));

            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                SelectedContractor.Id,
                NomenclatureViewSelectionMode.Single,
                queryGifts: OrderRules.NeedCheckGifts(SelectedSubdivision),
                queryAdditionalServices: true,
                includePriceJson: true,
                additionalServiceProvideProductsByProductId: additionalServiceDto.ProductId,
                cartProductIds: GetOrderProducts().Select(x => x.ProductId).ToArray());

            NomenclatureViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            ProcessSelectedForAddingProducts(
                viewModel.GetSelectedItems().ToArray(),
                orderProduct.OrderFolder,
                orderProduct.AssemblyQuantity ?? 1,
                orderProduct.Id,
                true);
        }

        private bool IsAdditionalServiceProductCompleted(int orderProductId)
        {
            return GetAdditionalServiceProduct(orderProductId)?.StateId == AdditionalServiceProductState.CompletedId;
        }

        private bool IsAdditionalServiceProductDoing(int orderProductId)
        {
            return GetAdditionalServiceProduct(orderProductId)?.StateId == AdditionalServiceProductState.DoingId;
        }

        private AdditionalServiceProductDto GetAdditionalServiceProduct(int orderProductId)
        {
            return AdditionalServiceProducts.FirstOrDefault(x => x.OrderProductId == orderProductId);
        }

        private bool CanChargeBonuses()
        {
            return SelectedContractor?.PriceTypeId == ProductPriceKind.Telemart1;
        }

        private int GetCancelReasonId(JToken jToken)
        {
            int changeReasonId = 0;

            if (jToken != null)
            {
                string tokenValue = jToken.Value<string>();

                if (!string.IsNullOrWhiteSpace(tokenValue))
                {
                    changeReasonId = Convert.ToInt32(tokenValue);
                }
            }

            return changeReasonId;
        }

        private void RecalculateCanChangePriceIdForCurrentProduct()
        {
            CanChangePriceIdForCurrentProduct = IsLockedByCurrentUserAndEditingAllowed
                                                && CurrentOrderProduct != null
                                                && !CurrentOrderProduct.IsGift
                                                && !CurrentOrderProduct.IsAdditionalService
                                                && (CurrentOrderProduct.OrderFolder is null || CurrentOrderProduct.OrderFolder.TypeId != OrderFolderType.AssembledComputerRuleId)
                                                && WebClient.IsOperationAllowed(BusinessOperation.OrderSetProductPriceType)
                                                && CurrentOrderProduct.PriceCanBeChanged
                                                && (PromoCodes is null || !PromoCodes.Any());
        }

        private void RecalculateJoinedComment()
        {
            JoinedComment = OrderCommentHelper.GetJoinedComment(CustomerComment, EmployeeComment, SystemComment);
        }

        private void ProcessNewPayment(Result<OrderPaymentResultDto> orderPaymentResult)
        {
            Messenger.Send(new OrderPaymentMessage(orderPaymentResult.Data.OrderPayment, MessageType.Added));

            Pko = orderPaymentResult.Data.Order.Pko;
            order = orderPaymentResult.Data.Order;
            RefreshSummaryItems();
            RefreshBonusSummary();
        }

        private async Task SendChequeToEmailAsync(string fiscalId)
        {
            if (string.IsNullOrEmpty(fiscalId))
            {
                return;
            }

            GetTextFromUserParameter parameter = new GetTextFromUserParameter(
                "Email",
                "Почта клиента",
                @"^$|^.+@.+\..+$",
                "Не валидное значение. ",
                content: Email);

            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(parameter, this);

            int cashboxId;
            IFiscalRegistrarClient fiscalClient;

            if (viewModel.IsOk)
            {
                if (order.CarryId == CarryType.PickupId)
                {
                    Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                        ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                        "Определении настроек РРО",
                        null,
                        this,
                        true);

                    if (clientResult?.IsSuccess != true)
                    {
                        return;
                    }
                    else
                    {
                        fiscalClient = clientResult.Data;
                        cashboxId = clientResult.Data.Settings.CashboxId;
                    }
                }
                else
                {
                    if (order.LegalEntityId == null)
                    {
                        MessageFacadeService.ShowNotificationError("В заказе не заполнено юр. лицо");
                        return;
                    }

                    var legalEntity = await WebClient.ExecuteApiRequestAsync(new QueryLegalEntity(order.LegalEntityId.Value));

                    if (legalEntity.FiscalCashboxId == null)
                    {
                        MessageFacadeService.ShowNotificationError("В юр. лице заказа не заполнена фискальная каса");
                        return;
                    }

                    cashboxId = legalEntity.FiscalCashboxId.Value;
                    fiscalClient = await FiscalRegistrarClientFactory.CreateAsync(FiscalRegistrarType.Software.Type, null, CancellationToken.None);
                }

                await ErrorHandler.HandleErrorsAsync(
                    ct => fiscalClient.SendToEmailsAsync(
                        fiscalId,
                        new[] { viewModel.Content },
                        cashboxId,
                        ct),
                    "отправке чека на почту",
                    "Чек на почту отправлен",
                    this,
                    true);
            }
        }

        private async Task SendChequeAsync(string fiscalId)
        {
            if (string.IsNullOrEmpty(fiscalId))
            {
                return;
            }

            int cashboxId;

            if (order.CarryId == CarryType.PickupId)
            {
                Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                    ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                    "Определении настроек РРО",
                    null,
                    this,
                    true);

                if (!clientResult.IsSuccess)
                {
                    return;
                }

                CashboxDto cashbox =
                    await WebClient.ExecuteApiRequestAsync(new QueryCashbox(clientResult.Data.Settings.CashboxId));

                if (cashbox.Session?.Closed != false)
                {
                    MessageFacadeService.ShowNotificationWarning("Смена закрыта");
                    return;
                }

                cashboxId = clientResult.Data.Settings.CashboxId;
            }
            else
            {
                if (order.LegalEntityId == null)
                {
                    MessageFacadeService.ShowNotificationError("В заказе не заполнено юр. лицо");
                    return;
                }

                var legalEntity = await WebClient.ExecuteApiRequestAsync(new QueryLegalEntity(order.LegalEntityId.Value));

                if (legalEntity.FiscalCashboxId == null)
                {
                    MessageFacadeService.ShowNotificationError("В юр. лице заказа не заполнена фискальная каса");
                    return;
                }

                cashboxId = legalEntity.FiscalCashboxId.Value;
            }

            await RroPrintHelper.SentCheckAsync(fiscalId, Phone, Email,  cashboxId, this, true);
        }

        private async Task<(bool Success, int[] CellIds)> PrepareUnpackAsync()
        {
            int[] cellIds = null;

            List<OrderCellDto> orderCells = await WebClient.ExecuteApiRequestAsync(new QueryOrderCells(order.Id));

            if (orderCells.Any() && SelectedWarehouse.UseCells && order.CarryId == CarryType.PickupId)
            {
                await RefreshOrderBillsAsync();

                int? billId = OrderBills
                    .Where(x => x.StateId == OrderBillState.Paid.Id)
                    .OrderByDescending(x => x.CreatedOn)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefault();

                OrderGiveCellViewModel orderGiveCellViewModel = SizeableDialogDocumentManagerService.ShowView<OrderGiveCellViewModel>(
                    new OrderGiveCellParameter(order, orderCells, SelectedCity.Name, SelectedContractor.Name, "Распаковка заказа", false, billId, false),
                    this);

                if (!orderGiveCellViewModel.IsOk)
                {
                    return (false, null);
                }

                cellIds = orderGiveCellViewModel.OrderCells.Where(x => x.Completed).Select(x => x.CellId).ToArray();
            }
            else if (!MessageFacadeService.Confirm("Вы уже нашли заказ на паллете и распаковали его?"))
            {
                MessageFacadeService.ShowNotificationWarning("Сначала найдите заказ");
                return (false, null);
            }

            return (true, cellIds);
        }

        private void OnPhone1Changed()
        {
            OnPhoneChangedInternal(Phone);
        }

        private void OnPhone2Changed()
        {
            OnPhoneChangedInternal(Phone2);
        }

        private void OnOrderMessage(OrderMessage message)
        {
            if (message.Entity.Id != Id)
            {
                return;
            }

            switch (message.MessageType)
            {
                case MessageType.Changed:
                    SelectLegalEntityId = message.Entity.LegalEntityId;
                    break;
            }
        }

        private void ShowDocumentsBotQr()
        {
            QrCodeParameter parameter = new QrCodeParameter(DocumentsBotHelper.GetUrl(order.Id, Entity.OrderId, Dictionaries, _telegramBotOptions), "Telegram бот");

            DialogDocumentManagerService.ShowView<QrCodeViewModel>(parameter, this);
        }

        private async void OnPhoneChangedInternal(string phone)
        {
            try
            {
                if (phone is null
                    || phone.Length != 10
                    || !Loaded
                    || !WebClient.IsOperationAllowed(BusinessOperation.CheckCreatedOrdersByPhone))
                {
                    return;
                }

                OrderFilteringItem queryOrdersFilter = new OrderFilteringItem()
                {
                    Cellphone = phone,
                    OrderStatuses = new List<int>()
                    {
                        OrderStatus.Received.Id,
                        OrderStatus.Confirmed.Id,
                        OrderStatus.Packed.Id
                    }
                };

                PagedResult<OrderDto> orders = await WebClient.ExecuteApiRequestAsync(new QueryOrders(queryOrdersFilter));

                if (!orders.Data.Any())
                {
                    return;
                }

                DocumentCommands.ShowPhoneHistoryCommand.Execute(new[] { Phone, Phone2 });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while changing phone");
            }
        }

        // Current logic also placed in TelemartEditorViewModelBase
        private async Task ShowModuleAnalyticsViewAsync()
        {
            if (Id < 1)
            {
                Logger.LogInformation("OrderViewModel.ShowModuleAnalyticsViewAsync: Order ID is less than 1, skipping analytics view.");
                return;
            }

            List<ModuleAnalyticUrlDto> moduleAnalyticsUrls = await WebClient.ExecuteApiRequestAsync(new QueryModuleAnalyticUrls(), true);

            ModuleAnalyticUrlDto analyticUrlDto = moduleAnalyticsUrls.FirstOrDefault(x => string.Equals(x.ModuleView, GetType().Name, StringComparison.OrdinalIgnoreCase));

            if (analyticUrlDto is not null
                && (WebClient.AuthenticatedEmployeeFullData?.Accounts?.Any(x => x.AccountId == WorkAccountIds.MetabaseId) == true
                    || WebClient.AuthenticatedEmployeeFullData?.GenericAccounts?.Metabase?.Login != null))
            {
                IModuleAnalyticsSettingsStore moduleAnalyticsSettingsStore = ContainerExtension.ServiceProvider.GetRequiredService<IModuleAnalyticsSettingsStore>();

                ModuleAnalyticsSettings moduleAnalyticsSettings = await moduleAnalyticsSettingsStore.LoadAsync();

                if (!moduleAnalyticsSettings.Settings.TryGetValue(GetType().Name, out ModuleAnalyticsSetting setting))
                {
                    setting = new()
                    {
                        PositionId = ModuleAnalyticsPosition.Maximized.Id,
                        LocationId = ModuleAnalyticsLocation.Horizontal.Id
                    };
                }

                if (setting.PositionId == ModuleAnalyticsPosition.Hidden.Id)
                {
                    return;
                }

                string analyticUrl = Smart.Format(analyticUrlDto.Url, this);

                // To support this feature - add <dxmvvm:CurrentWindowService /> behavior in XAML
                CurrentWindowService currentWindowService = (CurrentWindowService)GetService<ICurrentWindowService>();

                const int desiredHeight = 350;
                const int desiredWidth = 700;

                double? newLeft = null;
                double? newTop = null;
                int analyticsWindowWidth = desiredWidth;
                int analyticsWindowHeight = desiredHeight;

                if (setting.LocationId == ModuleAnalyticsLocation.Horizontal.Id)
                {
                    if (currentWindowService?.ActualWindow is not null)
                    {
                        double currentTop = currentWindowService.ActualWindow.Top;
                        double currentLeft = currentWindowService.ActualWindow.Left;
                        double currentWidth = currentWindowService.ActualWindow.Width;

                        double screenHeight = SystemParameters.PrimaryScreenHeight;
                        double screenWidth = SystemParameters.PrimaryScreenWidth;

                        newLeft = currentLeft;
                        newTop = currentTop + currentWindowService.ActualWindow.Height;

                        if (newTop + desiredHeight > screenHeight)
                        {
                            newLeft = (screenWidth - currentWidth) / 2;
                            newTop = (screenHeight - desiredHeight) / 2;
                        }

                        analyticsWindowHeight = desiredHeight;
                        analyticsWindowWidth = (int)currentWindowService.ActualWindow.Width - Constants.AnalyticsFormWidthMargin;
                    }
                }
                else
                {
                    if (setting.Left.HasValue && setting.Top.HasValue)
                    {
                        newLeft = setting.Left.Value;
                        newTop = setting.Top.Value;
                        analyticsWindowWidth = setting.Width ?? analyticsWindowWidth;
                        analyticsWindowHeight = setting.Height ?? analyticsWindowHeight;
                    }
                    else
                    {
                        newLeft = 100;
                        newTop = 100;
                    }
                }

                const int defaultWindowMargin = 25;

                ModuleAnalyticsParameter browserParameter = new ModuleAnalyticsParameter(
                    analyticsWindowWidth,
                    analyticsWindowHeight,
                    (int)(newTop ?? defaultWindowMargin),
                    (int)(newLeft ?? defaultWindowMargin),
                    analyticUrl,
                    "Аналитика",
                    setting.PositionId,
                    this,
                    Id,
                    GetType().Name);

                Messenger.Send(browserParameter);
            }
        }

        #region OrderDocuments

        private async Task RefreshDocumentsAsync()
        {
            await LoadDocumentsAsync(Id);
        }

        private async Task LoadDocumentsAsync(int id)
        {
            List<OrderDocumentSimpleDto> documentDtos = await WebClient.ExecuteApiRequestAsync(new QueryOrderDocuments(id));

            documents = documentDtos.ToObservableCollection();

            OrderDocumentViewItems = documentDtos?
                                         .Select(x => Mapper.Map<OrderDocumentViewItem>(x)).ToObservableCollection()
                                     ?? Array.Empty<OrderDocumentViewItem>().ToObservableCollection();

            List<OrderDocumentTypeDto> types = await WebClient.ExecuteApiRequestAsync(new QueryOrderDocumentTypes());

            OrderDocumentTypes = types.ToReadOnlyObservableCollection();
        }

        private void AddDocument()
        {
            const int MaxDocumentsCount = 25;
            const int MaxFileLengthMb = 10;

            if (OpenFileDialogService.ShowDialog())
            {
                if (OpenFileDialogService.Files.Count() + OrderDocumentViewItems.Count > MaxDocumentsCount)
                {
                    MessageFacadeService.ShowNotificationWarning($"Можно добавить не больше чем {MaxDocumentsCount} файлов");
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

                    return;
                }

                AddDocumentsParameter parameter = new AddDocumentsParameter(
                    Id,
                    OpenFileDialogService.Files.Select(x => x.GetFullName()).ToList());

                NonModalSizeableDialogDocumentManagerService.ShowView<OrderAddDocumentViewModel>(parameter, this);
            }
        }

        private async Task RemoveDocumentAsync(OrderDocumentViewItem item)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteOrderDocument(item.Id));

                OrderDocumentSimpleDto removingDocument = documents.FirstOrDefault(x => x.Id == item.Id);
                documents.Remove(removingDocument);

                OrderDocumentViewItems.Remove(item);

                MessageFacadeService.ShowNotificationInfo("Документ удален успешно");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to delete Trade-In documents");

                MessageFacadeService.ShowNotificationError("Ошибка при удалении документа");
            }
        }

        private async Task PrintDocumentAsync(OrderDocumentViewItem item)
        {
            OrderDocumentDto document = await WebClient.ExecuteApiRequestAsync(new QueryOrderDocument(item.Id));
            await FileHelper.OpenAsFileAsync(document.Data, document.Ext);
        }

        private void OnDocumentMessage(OrderCreateDocumentMessage message)
        {
            if (message.Entity.OrderId == Id)
            {
                switch (message.MessageType)
                {
                    case MessageType.Added:
                        documents.Add(message.Entity);
                        OrderDocumentViewItems ??= Array.Empty<OrderDocumentViewItem>().ToObservableCollection();
                        OrderDocumentViewItems.Add(Mapper.Map<OrderDocumentViewItem>(message.Entity));

                        SetSourcesKeepGuestProducts(message.Order);

                        break;
                }
            }
        }

        private bool HasKeepGuestProducts()
        {
            IReadOnlyCollection<(int OrderProductId, GuestProductDto GuestProduct)> guestProducts = GetOrderProductGuestProducts();

            return guestProducts.Any(x => x.GuestProduct?.KeepProduct == true);

        }

        private bool AllGuestProductsHasCharacteristics()
        {
            IReadOnlyCollection<(int OrderProductId, GuestProductDto GuestProduct)> guestProducts = GetOrderProductGuestProducts();

            return guestProducts.All(x => x.GuestProduct != null);
        }

        private bool IsLastGuestProduct(int orderProductId)
        {
            IReadOnlyCollection<(int OrderProductId, GuestProductDto GuestProduct)> guestProducts = GetOrderProductGuestProducts();

            return guestProducts.Where(x => x.OrderProductId != orderProductId).All(x => x.GuestProduct != null)
                && guestProducts.Where(x => x.OrderProductId == orderProductId).All(x => x.GuestProduct == null);
        }

        private IReadOnlyCollection<(int OrderProductId, GuestProductDto GuestProduct)> GetOrderProductGuestProducts()
        {
            int[] guestOrderProductIds = OrderProducts?.Where(x => x.Product.TypeId == ProductType.GuestProductId)
                .Select(x => x.Id).ToArray();

            List<(int OrderProductId, GuestProductDto GuestProduct)> guestProducts = new();

            guestProducts.AddRange(
                AdditionalServiceProducts.Where(x => x.ParentOrderProductId.HasValue
                                                     && guestOrderProductIds?.Contains(x.ParentOrderProductId.Value) == true)
                    .Select(x => (x.ParentOrderProductId!.Value, x.GuestProduct)));

            guestProducts.AddRange(
                AdditionalServiceProducts.SelectMany(x => x.ConsumableProducts
                        .Where(y => y.OrderProductId.HasValue && guestOrderProductIds?.Contains(y.OrderProductId!.Value) == true))
                    .Select(x => (x.OrderProductId!.Value, x.GuestProduct)));

            return guestProducts;
        }

        private void SetSourcesKeepGuestProducts(OrderDto orderDto)
        {
            OrderProducts?.ForEach(
                x =>
                {
                    if (x.IsGuestProduct)
                    {
                        OrderProductDto orderProductDto = orderDto?.Products?.FirstOrDefault(y => y.Id == x.Id);

                        if (orderProductDto != null
                            && (orderProductDto.SourceId != x.Source.Id
                                || orderProductDto!.SourceText != x.Source.SourceText
                                || orderProductDto!.SourceDate != x.Source.SourceDate))
                        {
                            x.Source = Dictionaries.GetOrderProductSource(
                                orderProductDto.SourceId,
                                orderProductDto.WarehouseId,
                                orderProductDto.SourceText,
                                orderProductDto.SourceDate);
                        }
                    }
                });

        }

        private void OnAdditionalServiceProductUpdateMessage(AdditionalServiceProductEntityMessage message)
        {
            if (message.Entity != null)
            {
                switch (message.MessageType)
                {
                    case MessageType.Changed:

                        AdditionalServiceProductDto additionalServiceProductDto = AdditionalServiceProducts.FirstOrDefault(x => x.Id == message.Entity.Id);

                        AdditionalServiceProducts.Remove(additionalServiceProductDto);

                        AdditionalServiceProducts.Add(message.Entity);

                        break;
                }
            }
        }

        private void OnAdditionalServiceProductMessage(EntityMessage<AdditionalServiceProductDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:

                    AdditionalServiceProductDto additionalServiceProductDto = AdditionalServiceProducts.FirstOrDefault(x => x.Id == message.Entity.Id);

                    AdditionalServiceProducts.Remove(additionalServiceProductDto);

                    AdditionalServiceProducts.Add(message.Entity);

                    SetCurrentProductAdditionalServiceProducts();
                    break;
            }

            RaisePropertyChanged(nameof(ProductAdditionalServiceProducts));
        }

        #endregion

        public class OrderHistoryFilterItem
        {
            public OrderHistoryFilterItem(int id, string name, OrderHistoryType filterType)
            {
                Id = id;
                Name = name;
                FilterType = filterType;
            }

            public int Id { get; }

            public string Name { get; }

            public OrderHistoryType FilterType { get; }
        }
    }
}