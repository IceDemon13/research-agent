using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Business.Order;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Data.Requests.Features.AdditionalService;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Contractor.Template;
using Telemart.Client.Data.Requests.Features.Customer;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Locations;
using Telemart.Client.Data.Requests.Features.Movement;
using Telemart.Client.Data.Requests.Features.Movement.Actions;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.PromoCode;
using Telemart.Client.Data.Requests.Features.Purchase;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.Requests.Features.Warehouse.Delivery;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AutoSource;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.PromoCode;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.Warehouse.Delivery;
using Telemart.Client.ViewModels.AdditionalService;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Customer;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.TreeStructure;

namespace Telemart.Client.ViewModels.Store.Order.CreateCompleted
{
    public class CreateCompletedOrderViewModel : TelemartDialogViewModelBase
    {
        private const bool UseCache = true;

        private readonly IMessenger _messenger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly IIdGenerator _idGenerator;
        private readonly IPrintingSettingsStore _printingSettings;
        private readonly IOrderGiveHelper _orderGiveHelper;
        private readonly IErrorHandler _errorHandler;
        private readonly int[] _additionalServiceProductTypeIds = { ProductType.AccessoryId, ProductType.CertificateId };

        private IReadOnlyCollection<DeliveryDto> _warehouseDeliveries;
        private IReadOnlyCollection<WarehouseDto> _warehouses;
        private IReadOnlyCollection<LocationEntityDto> _locations;

        private ScanSerialMode _scanSerialMode = ScanSerialMode.Single;

        private bool _closed;

        public CreateCompletedOrderViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IIdGenerator idGenerator,
            IMapper mapper,
            IPrintingSettingsStore printingSettings,
            IMediator mediator,
            IOrderGiveHelper orderGiveHelper,
            ProductInformationViewModel productInformationViewModel,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _messenger = messenger;
            _idGenerator = idGenerator;
            _mapper = mapper;
            _printingSettings = printingSettings;
            _mediator = mediator;
            _orderGiveHelper = orderGiveHelper;
            _errorHandler = errorHandler;

            AllowEditContractor = !WebClient.AuthenticatedEmployee.HasAnyRole(Role.OutsourceSeller);

            OrderPaymentViewModel = new OrderPaymentInfoViewModel();
            ProductInformation = productInformationViewModel;
            RecognizeBarcodeViewModel = new RecognizeBarcodeViewModel(webClient, dictionaries, messageFacadeService);

            RecognizeBarcodeViewModel.Init(new RecognizeBarcodeSettings(true, true), Array.Empty<ProductAttributesDto>());

            DeleteProductCommand = new DelegateCommand(() => DeleteProduct(SelectedOrderProduct), () => SelectedOrderProduct != null);
            AddPromoCodeCommand = new AsyncCommand(AddPromoCodeAsync);
            RemovePromoCodeCommand = new AsyncCommand<OrderPromoCodeDto>(RemovePromoCodeAsync, x => x != null);
            EditProductSerialsCommand = new DelegateCommand<CreateCompletedOrderProductViewItem>(EditProductSerials, x => x != null);
            RefreshPromoCodesCommand = new AsyncCommand(RefreshPromoCodesAsync, () => Model.PromoCodes?.Any() == true);
            AutoSourceCommand = new AsyncCommand(AutoSourceAsync, CanAutoSource);
            GiveCommand = new AsyncCommand(GiveAsync, CanExecuteGiveCommand);
            GiveRroCommand = new AsyncCommand(GiveRroAsync);
            FindCustomerCommand = new AsyncCommand(FindCustomerAsync, () => Model != null && !string.IsNullOrWhiteSpace(Model.Phone));
            AddProductCommand = new AsyncCommand<RecognizeBarcodeResultEventArgs>(RecognizeBarcodeViewModelOnFinishedAsync);
            HandleRowDoubleClickCommand = new AsyncCommand<RowDoubleClickInfo>(HandleRowDoubleClickAsync, x => x != null);

            AddBonusCommand = new DelegateCommand(AddBonus);
            EditBonusCommand = new DelegateCommand<OrderBonusSummaryViewItem>(EditBonus, x => x != null);
            RemoveBonusCommand = new DelegateCommand<OrderBonusSummaryViewItem>(RemoveBonus, x => x != null);

            Contractors = new ObservableRangeValidatableCollection<OrderContractorViewItem>(
                ContractorIsValid,
                y => y.OrderByDescending(x => x.Valid)
                    .ThenBy(x => x.Subdivision.Id)
                    .ThenBy(x => x.Name));
            Cities = new ObservableRangeValidatableCollection<OrderCityViewItem>(
                _ => true,
                y => y.OrderByDescending(x => x.Valid)
                    .ThenBy(x => x.Position)
                    .ThenBy(x => x.Name));
            CarryTypes = new ObservableRangeValidatableCollection<OrderCarryTypeViewItem>(CarryTypeIsValid, y => y.OrderByDescending(x => x.Valid).ThenBy(x => x.Position));
            Warehouses = new ObservableRangeValidatableCollection<OrderWarehouseViewItem>(WarehouseIsValid, y => y.OrderByDescending(x => x.Valid).ThenByDescending(x => x.Position));
            Payments = new ObservableRangeValidatableCollection<OrderPaymentViewItem>(PaymentIsValid, y => y.OrderByDescending(x => x.Valid).ThenBy(x => x.Id));

            AllowChangePhone = true;

            AppliedBonuses = new ObservableCollection<OrderBonusSummaryViewItem>();

            RecognizeBarcodeViewModel.OnStarted += RecognizeBarcodeViewModelOnStarted;
            RecognizeBarcodeViewModel.OnFinished += (_, e) => AddProductCommand.Execute(e);
        }

        public CreateCompletedOrderViewModel()
        {
        }

        public IDelegateCommand EditProductSerialsCommand { get; }

        public IDelegateCommand DeleteProductCommand { get; }

        public IAsyncCommand AddPromoCodeCommand { get; }

        public IAsyncCommand FindCustomerCommand { get; }

        public IAsyncCommand RemovePromoCodeCommand { get; }

        public IAsyncCommand RefreshPromoCodesCommand { get; }

        public IAsyncCommand AddProductCommand { get; }

        public IAsyncCommand HandleRowDoubleClickCommand { get; }

        public IAsyncCommand GiveCommand { get; }

        public IAsyncCommand GiveRroCommand { get; }

        public IAsyncCommand AutoSourceCommand { get; }

        public IDelegateCommand AddBonusCommand { get; }

        public IDelegateCommand EditBonusCommand { get; }

        public IDelegateCommand RemoveBonusCommand { get; }

        public OrderPaymentInfoViewModel OrderPaymentViewModel { get; }

        public ProductInformationViewModel ProductInformation { get; }

        public RecognizeBarcodeViewModel RecognizeBarcodeViewModel { get; }

        public ObservableRangeValidatableCollection<OrderContractorViewItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private init { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ProductPriceKind> PriceKinds
        {
            get { return GetProperty(() => PriceKinds); }
            private set { SetProperty(() => PriceKinds, value); }
        }

        public ObservableRangeValidatableCollection<OrderCityViewItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private init { SetProperty(() => Cities, value); }
        }

        public ObservableRangeValidatableCollection<OrderCarryTypeViewItem> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private init { SetProperty(() => CarryTypes, value); }
        }

        public ObservableRangeValidatableCollection<OrderWarehouseViewItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private init { SetProperty(() => Warehouses, value); }
        }

        public ObservableRangeValidatableCollection<OrderPaymentViewItem> Payments
        {
            get { return GetProperty(() => Payments); }
            private init { SetProperty(() => Payments, value); }
        }

        public ObservableCollection<OrderBonusSummaryViewItem> AppliedBonuses
        {
            get { return GetProperty(() => AppliedBonuses); }
            private set { SetProperty(() => AppliedBonuses, value, () => RaisePropertyChanged(nameof(AppliedBonusesQuantity))); }
        }

        public int AppliedBonusesQuantity => AppliedBonuses?.Count ?? 0;

        public CreateCompletedOrderProductViewItem SelectedOrderProduct
        {
            get { return GetProperty(() => SelectedOrderProduct); }
            set { SetProperty(() => SelectedOrderProduct, value, SelectedOrderProductChanged); }
        }

        public CreateCompletedOrderViewItem Model
        {
            get { return GetProperty(() => Model); }
            private set { SetProperty(() => Model, value); }
        }

        public bool AutoPrintAcceptanceProtocol
        {
            get { return GetProperty(() => AutoPrintAcceptanceProtocol); }
            set { SetProperty(() => AutoPrintAcceptanceProtocol, value); }
        }

        public bool AutoPrintWarrantyCard
        {
            get { return GetProperty(() => AutoPrintWarrantyCard); }
            set { SetProperty(() => AutoPrintWarrantyCard, value); }
        }

        public ReadOnlyObservableCollection<BonusType> BonusTypes
        {
            get { return GetProperty(() => BonusTypes); }
            private set { SetProperty(() => BonusTypes, value); }
        }

        public bool IsRecognitionInProgress
        {
            get { return GetProperty(() => IsRecognitionInProgress); }
            private set { SetProperty(() => IsRecognitionInProgress, value); }
        }

        public bool AllowEditContractor { get; }

        public bool CanExecuteGiveCommand
        {
            get { return GetProperty(() => CanExecuteGiveCommand); }
            private set { SetProperty(() => CanExecuteGiveCommand, value); }
        }

        public string PhoneToolTip
        {
            get { return GetProperty(() => PhoneToolTip); }
            private set { SetProperty(() => PhoneToolTip, value); }
        }

        public bool AllowChangePhone
        {
            get { return GetProperty(() => AllowChangePhone); }
            private set { SetProperty(() => AllowChangePhone, value); }
        }

        public override void OnClose(CancelEventArgs e)
        {
            if (_closed || MessageFacadeService.Confirm("Закрыть диалог без сохранения изменений?"))
            {
                base.OnClose(e);
            }
            else
            {
                e.Cancel = true;
            }
        }

        protected override async Task HandleLoadedAsync()
        {
            await base.HandleLoadedAsync();

            CreateCompletedOrderParameter parameter = (CreateCompletedOrderParameter)Parameter;

            ContractorTemplateDto contractorTemplate;

            Model = new CreateCompletedOrderViewItem();
            Model.PropertyChanged += OnModelPropertyChanged;

            AutoPrintWarrantyCard = true;

            if (parameter.ContractorTemplateId.HasValue)
            {
                contractorTemplate = await WebClient.ExecuteApiRequestAsync(new QueryContractorTemplate(parameter.ContractorTemplateId.Value));
            }
            else
            {
                contractorTemplate = new ContractorTemplateDto();
            }

            int selectedContractorId = contractorTemplate.ClientId > 0 ? contractorTemplate.ClientId : Constants.TelemartContractorId;

            Task<IEnumerable<OrderContractorViewItem>> contractorsTask = FetchContractorsAsync(selectedContractorId);
            Task<IEnumerable<OrderCityViewItem>> citiesTask = FetchCitiesAsync(contractorTemplate.CityId);
            Task<IEnumerable<OrderWarehouseViewItem>> warehousesTask = FetchWarehousesAsync(contractorTemplate.WarehouseId);

            await Task.WhenAll(FetchWarehouseDeliveriesAsync(), FetchEmployeesAsync(), FetchShopsAsync(), contractorsTask, citiesTask, warehousesTask);

            Contractors.ReplaceRange(contractorsTask.Result);
            Cities.ReplaceRange(citiesTask.Result);
            Warehouses.ReplaceRange(warehousesTask.Result);

            FetchCarryTypes(CarryType.PickupId);
            FetchPayments(Payment.CashId);

            Model.SelectedContractor = Contractors.FirstOrDefault(x => x.Id == selectedContractorId);
            Model.SelectedCity = Cities.FirstOrDefault(x => x.Id == contractorTemplate.CityId);
            Model.SelectedWarehouse = Warehouses.FirstOrDefault(x => x.Id == contractorTemplate.WarehouseId);
            Model.SelectedCarryType = CarryTypes.FirstOrDefault(x => x.Id == CarryType.PickupId);
            Model.SelectedPayment = Payments.FirstOrDefault(x => x.Id == Payment.CashId);

            Contractors.Validate();
            Cities.Validate();
            Warehouses.Validate();
            CarryTypes.Validate();
            Payments.Validate();

            Model.RaiseProperties();

            Model.LastName = contractorTemplate.LastName;
            Model.FirstName = contractorTemplate.FirstName;
            Model.MiddleName = contractorTemplate.MiddleName;
            Model.Phone = contractorTemplate.Phone;
            Model.Phone2 = contractorTemplate.Phone2;
            Model.Email = contractorTemplate.Email;
            Model.DeliveryData = contractorTemplate.DeliveryData;
            Model.Address = contractorTemplate.Address;

            PriceKinds = Dictionaries
                .GetItems<ProductPriceKind>()
                .Where(x => x.PriceColumn != null)
                .OrderBy(x => x.PriceColumn)
                .ToReadOnlyObservableCollection();

            BonusTypes = Dictionaries.GetItems<BonusType>().ToReadOnlyObservableCollection();

            Title = "Быстрая продажа";
        }

        protected override Task HandleOkAsync()
        {
            throw new NotSupportedException();
        }

        private static Task<bool> IsPrintingSettingsValidAsync(PrintingSettingsInfo settings, CancellationToken cancellationToken)
        {
            if (settings?.Main?.Name == null ||
                settings.WarrantyCard?.Name == null ||
                (settings.ChequeFormat == PrintingSettingsChequeFormat.CheckTape.Id && settings.Cheque?.Name == null))
            {
                return Task.FromResult(false);
            }

            return Task<bool>.Factory.StartNew(IsPrintingSettingsValid, cancellationToken);

            bool IsPrintingSettingsValid()
            {
                List<string> printers = new List<string> { settings.Main.Name, settings.WarrantyCard.Name };

                if (settings.ChequeFormat == PrintingSettingsChequeFormat.CheckTape.Id)
                {
                    printers.Add(settings.Cheque.Name);
                }

                return printers
                    .Select(printerName => new PrinterSettings { PrinterName = printerName })
                    .All(printerSettings => printerSettings.IsValid);
            }
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Model == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(CreateCompletedOrderViewItem.SelectedContractor):
                    Warehouses.Validate();
                    Payments.Validate();

                    CanExecuteGiveCommand = Model.SelectedContractor?.OldClient == true;

                    Model.RaiseProperties(nameof(Model.SelectedWarehouse), nameof(Model.SelectedPayment));

                    break;
                case nameof(CreateCompletedOrderViewItem.SelectedCity):
                    CarryTypes.Validate();
                    Warehouses.Validate();

                    Model.RaiseProperties(nameof(Model.SelectedWarehouse), nameof(Model.SelectedCarryType));

                    ClearAddressFields();
                    CleanProductSources();
                    break;
                case nameof(CreateCompletedOrderViewItem.SelectedCarryType):
                    Warehouses.Validate();

                    AutoPrintAcceptanceProtocol = Model.SelectedCarryType?.Id != CarryType.PickupId;

                    Model.RaiseProperties(nameof(Model.SelectedWarehouse));

                    RefreshPaymentInfo();
                    break;

                case nameof(CreateCompletedOrderViewItem.Address):
                    Warehouses.Validate();

                    Model.RaiseProperties(nameof(Model.SelectedWarehouse));
                    break;
            }
        }

        private async Task GiveAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this) || Model?.OrderProducts?.Any(x => IDataErrorInfoHelper.HasErrors(x)) != false)
            {
                return;
            }

            if (Model.OrderProducts?.Any() != true)
            {
                MessageFacadeService.ShowNotificationWarning("Добавьте хотя бы один товар в заказ");
                return;
            }

            if (Model.SelectedContractor.OldClient == false)
            {
                MessageFacadeService.ShowNotificationWarning("Контрагент не поддерживает выбранный способ оплаты");
                return;
            }

            if (Model.OrderProducts.Any(x => x.Source == null))
            {
                MessageFacadeService.ShowNotificationWarning("Не всем товарам установлен источник");
                return;
            }

            OrderDto order = await GiveInternalAsync(false);

            if (order != null)
            {
                IsOk = true;
                ForceClose();
            }
        }

        private async Task FindCustomerAsync()
        {
            CustomerFilteringItem item = new CustomerFilteringItem(Model.Phone);

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

            Fio fio = new Fio(customer.Fio);

            Model.FirstName = fio.FirstName;
            Model.LastName = fio.LastName;
            Model.MiddleName = fio.MiddleName;
            Model.Email = customer.Email;
            Model.ValidatedPhone = Model.Phone;
            Model.CustomerBonuses = customer.Bonuses;
        }

        private async Task GiveRroAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this) || Model?.OrderProducts?.Any(x => IDataErrorInfoHelper.HasErrors(x)) != false)
            {
                MessageFacadeService.ShowNotificationWarning("Найдены ошибки на форме");
                return;
            }

            if (Model.OrderProducts?.Any() != true)
            {
                MessageFacadeService.ShowNotificationWarning("Добавьте хотя бы один товар в заказ");
                return;
            }

            if (Model.OrderProducts.Any(x => x.Source == null))
            {
                MessageFacadeService.ShowNotificationWarning("Не всем товарам установлен источник");
                return;
            }

            try
            {
                OrderCreatePackedDto createDto = GetCreateDto();

                await WebClient.ExecuteApiRequestAsync(new CanCreatePackedOrder(createDto));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании заказа");
                ShowValidationResultView("Ошибки при создании заказа", exception.GetErrorItems());
                return;
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create order");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                return;
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании заказа");
                Logger.LogError(exception, "Error while creating order");
                return;
            }

            OrderDto order = await GiveInternalAsync(true);

            if (order == null)
            {
                return;
            }

            IsOk = true;
            ForceClose();
        }

        private async Task<OrderDto> GiveInternalAsync(bool white)
        {
            if (AutoPrintAcceptanceProtocol || AutoPrintWarrantyCard)
            {
                PrintingSettingsInfo settings = await _printingSettings.LoadAsync();

                bool isPrintingSettingsValid;

                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    isPrintingSettingsValid = await IsPrintingSettingsValidAsync(settings, cancellationTokenSource.Token);
                }

                if (!isPrintingSettingsValid)
                {
                    MessageFacadeService.ShowNotificationError("Задайте принтеры в настройках");
                    return null;
                }
            }

            IReadOnlyCollection<CreateCompletedOrderProductViewItem> orderProductsForMovenemts = Model.OrderProducts.Where(x => x.Source.WarehouseId != Model.SelectedWarehouse.Id).ToArray();

            if (orderProductsForMovenemts.Any())
            {
                IReadOnlyCollection<int> movementIds = await MovementProcessingAsync(orderProductsForMovenemts);

                if (movementIds.Count <= 0 || Model.OrderProducts.Any(x => x.Source == null))
                {
                    return null;
                }

                MessageFacadeService.ShowNotification($"Создано перемещение №{string.Join(", ", movementIds)}", MessageBoxImage.Information);
            }

            OrderCreatePackedDto createDto = GetCreateDto();

            try
            {
                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(new CreatePackedOrder(createDto));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    const string Message = "Заказ создан с предупреждениями";

                    ShowValidationResultView(Message, validationResultItems);

                    MessageFacadeService.ShowNotificationWarning(Message);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Заказ №{result.Data.Id} успешно создан");
                }

                _messenger.Send(new OrderMessage(result.Data, MessageType.Added));

                (bool success, OrderDto order) = await _orderGiveHelper.GiveAsync(
                    result.Data.Id,
                    Model.SelectedContractor.Name,
                    Model.SelectedCity.Name,
                    false,
                    this,
                    white,
                    AutoPrintWarrantyCard);

                if (success)
                {
                    await PrintDocumentsAsync(order);

                    return order;
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning("При выдаче заказа возникли ошибки. Найдите этот заказ в списке и выдайте повторно");

                    return result.Data;
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании заказа");
                ShowValidationResultView("Ошибки при создании заказа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create order");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании заказа");
                Logger.LogError(exception, "Error while creating order");
            }

            return null;
        }

        private void ForceClose()
        {
            _closed = true;
            Close();
        }

        private async Task PrintDocumentsAsync(OrderDto order)
        {
            if (AutoPrintAcceptanceProtocol)
            {
                await _mediator.Send(new PrintOrderDocumentRequest(order.Id, OrderDocumentType.AcceptanceProtocolId, preview: false));
            }
        }

        private void DeleteProduct(CreateCompletedOrderProductViewItem item)
        {
            if (!MessageFacadeService.Confirm("Вы уверены что ходите удалить товар?"))
            {
                return;
            }

            if (item.BonusTypeId.HasValue && item.AppliedBonusesQuantity > 0)
            {
                OrderBonusSummaryViewItem bonusItem = AppliedBonuses.First(x => x.BonusTypeId == item.BonusTypeId.Value);
                CustomerBonusDto customerBonus = Model.CustomerBonuses.First(x => x.BonusTypeId == item.BonusTypeId.Value);

                bonusItem.Quantity -= item.AppliedBonusesQuantity;
                customerBonus.Quantity += item.Quantity;

                if (bonusItem.Quantity == 0)
                {
                    AppliedBonuses.Remove(bonusItem);
                }

                RaisePropertyChanged(nameof(AppliedBonusesQuantity));
            }

            int selectedOrderProductId = item.Id;

            Model.OrderProducts.RemoveAll(x => x.Id == selectedOrderProductId || x.ParentRecordId == selectedOrderProductId);

            RefreshPaymentInfo();
        }

        private async Task RefreshPromoCodesAsync()
        {
            if (Model.PromoCodes.Count == 0)
            {
                return;
            }

            if (Model.SelectedContractor.Subdivision != Subdivision.Telemart)
            {
                MessageFacadeService.ShowNotificationWarning("Использование промо-кодов возможно лишь с подразделением «Телемарт»");
                return;
            }

            CheckPromoCodesRequest request = new CheckPromoCodesRequest(
                Model.PromoCodes.Select(x => x.Value).ToArray(),
                Model.OrderProducts.Select(x => new CheckPromoCodeProductRequest(x.Id, x.ProductId,  x.Quantity, x.Price)).ToArray(),
                DateTime.Now);

            CheckPromoCodesResponse response = await CheckPromoCodesAsync(request);

            if (response == null)
            {
                return;
            }

            await ProcessPromoCodesAsync(response);
        }

        private async Task RemovePromoCodeAsync(OrderPromoCodeDto promoCode)
        {
            CheckPromoCodesRequest request = new CheckPromoCodesRequest(
                Model.PromoCodes.Where(x => x.Value != promoCode.Value).Select(x => x.Value).ToArray(),
                Model.OrderProducts.Select(x => new CheckPromoCodeProductRequest(x.Id, x.ProductId, x.Quantity, x.Price)).ToArray(),
                DateTime.Now);

            CheckPromoCodesResponse response = await CheckPromoCodesAsync(request);

            if (response == null)
            {
                return;
            }

            bool accepted = await ProcessPromoCodesAsync(response);

            if (accepted)
            {
                Model.PromoCodes.Remove(promoCode);
            }

            RefreshPaymentInfo();
        }

        private void EditProductSerials(CreateCompletedOrderProductViewItem packViewItem)
        {
            ProductEditSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
              new ProductEditSerialsParameter(packViewItem.Serials.ToList()), this);

            if (viewModel.IsOk)
            {
                packViewItem.Serials = new ObservableRangeCollection<string>(viewModel.SerialNumbers);
                packViewItem.ScannedQuantity = packViewItem.Serials.Count;
            }
        }

        private async Task AddPromoCodeAsync()
        {
            if (Model.SelectedContractor.Subdivision != Subdivision.Telemart)
            {
                MessageFacadeService.ShowNotificationWarning("Использование промо-кодов возможно лишь с подразделением «Телемарт»");
                return;
            }

            GetTextFromUserViewModel vm = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                new GetTextFromUserParameter("Введите промо-код или код акции", "Промо-код / код акции"),
                this);

            if (!vm.IsOk)
            {
                return;
            }

            string promoCode = vm.Content;
            string[] promoCodes = null;
            int[] promoCodeIds = null;

            if (int.TryParse(promoCode, out int promoCodeId))
            {
                if (Model.PromoCodes.Any(x => x.PromoCodeId == promoCodeId))
                {
                    MessageFacadeService.ShowNotificationWarning($"Промо-код «{promoCode}» уже добавлен в заказ");
                    return;
                }

                promoCodeIds = Model.PromoCodes.Select(x => x.PromoCodeId).Union(new[] { promoCodeId }).ToArray();
            }
            else
            {
                if (Model.PromoCodes.Any(x => string.Equals(x.Value, promoCode, StringComparison.Ordinal)))
                {
                    MessageFacadeService.ShowNotificationWarning($"Промо-код «{promoCode}» уже добавлен в заказ");
                    return;
                }

                promoCodes = Model.PromoCodes.Select(x => x.Value).Union(new[] { promoCode }).ToArray();
            }

            if (Model.PromoCodes.Any(x => string.Equals(x.Value, promoCode, StringComparison.Ordinal)))
            {
                MessageFacadeService.ShowNotificationWarning($"Промо-код «{promoCode}» уже добавлен в заказ");
                return;
            }

            CheckPromoCodeProductRequest[] promoProducts = Model.OrderProducts.Select(x => new CheckPromoCodeProductRequest(x.Id, x.ProductId, x.Quantity, x.Price)).ToArray();

            CheckPromoCodesRequest request = promoCodeIds?.Any() == true
                ? new CheckPromoCodesRequest(
                    promoCodeIds,
                    promoProducts,
                    DateTime.Now)
                : new CheckPromoCodesRequest(
                    promoCodes,
                    promoProducts,
                    DateTime.Now);

            CheckPromoCodesResponse response = await CheckPromoCodesAsync(request);

            if (response == null)
            {
                return;
            }

            bool accepted = await ProcessPromoCodesAsync(response);

            if (accepted)
            {
                CheckPromoCodeResult promoCodeResult = response.PromoCodes.First(x => x.PromoCodeValue == promoCode || x.PromoCodeId!.Value.ToString() == promoCode);

                PromoCodeFullDto promoCodeData = await WebClient.ExecuteApiRequestAsync(new QueryPromoCode(promoCodeResult.PromoCodeId!.Value));

                OrderPromoCodeDto dto = new OrderPromoCodeDto
                {
                    Id = new Random().GetRandomId(),
                    PromoCodeId = promoCodeData.Id,
                    Value = promoCodeData.Value,
                    PromoCodeMetaTitle = promoCodeData.MetaTitle,
                    PromoCodeTypeId = promoCodeData.TypeId
                };

                Model.PromoCodes.Add(dto);
            }

            RefreshPaymentInfo();
        }

        private async Task<CheckPromoCodesResponse> CheckPromoCodesAsync(CheckPromoCodesRequest request)
        {
            CheckPromoCodesResponse response = null;

            try
            {
                response = await WebClient.ExecuteApiRequestAsync(new CheckPromoCodes(request));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при добавлении промо-кода");
                ShowValidationResultView("Ошибки при добавлении промо-кода", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при добавлении промо-кода");
                ShowValidationResultView("Ошибки при добавлении промо-кода", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to add promo code");
                MessageFacadeService.ShowNotificationError("Ошибка при добавлении промо-кода");
            }

            return response;
        }

        private async Task<bool> ProcessPromoCodesAsync(CheckPromoCodesResponse response)
        {
            bool result = false;

            List<ValidationResultItem> errors = response.PromoCodes
                .Where(x => !string.IsNullOrWhiteSpace(x.Error))
                .Select(x => new ValidationResultItem(x.Error, true))
                .ToList();

            if (errors.Any())
            {
                ShowValidationResultView("Ошибки при добавлении промо-кода", errors);
            }
            else
            {
                List<(CreateCompletedOrderProductViewItem OrderProduct, int Position)> giftOrderProducts = new List<(CreateCompletedOrderProductViewItem OrderProduct, int Position)>();

                Dictionary<int, (int ParentProductId, int ParentRecordId)> giftProductsWithParentDatas = response.Products
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
                                        ParentRecordId = x.First(product => !product.IsGift).Id,
                                        ParentProductId = x.First(product => !product.IsGift).ProductId
                                    }))
                    .DistinctBy(x => x.GiftProductId)
                    .ToDictionary(x => x.GiftProductId, x => (x.ParentProductId, x.ParentRecordId));

                int[] giftProductIds = response.Products
                    .Where(x => x.IsGift)
                    .Select(x => x.ProductId)
                    .ToArray();

                if (giftProductIds.Any())
                {
                    List<ProductDto> giftProducts = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(Constants.TelemartContractorId, giftProductIds));

                    foreach (CheckPromoCodeProductResponse giftProduct in response.Products.Where(x => x.IsGift))
                    {
                        if (!giftProductsWithParentDatas.TryGetValue(
                                giftProduct.ProductId,
                                out (int ParentProductId, int ParentRecordId) parentData))
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

                        CreateCompletedOrderProductViewItem parentOrderProduct = Model.OrderProducts.First(x => x.Id == parentData.ParentRecordId);

                        CreateCompletedOrderProductViewItem orderGiftProductViewModel =
                            new CreateCompletedOrderProductViewItem()
                            {
                                Id = _idGenerator.GetNext(),
                                ProductId = giftProductDto.Id,
                                ParentRecordId = parentOrderProduct.Id,
                                Quantity = giftProduct.Quantity,
                                PriceOut = 1,
                                CurrencyOutId = Currency.UahId,
                                Price = 1,
                                CurrencyId = Currency.UahId,
                                IsGift = true,
                                ProductName = giftProductDto.Name,
                                ProductTypeId = giftProductDto.TypeId,
                                BonusesToCharge = 0,
                                ProductFullNameUa = giftProductDto.NameFullUa,
                                Source = null,
                                ParentProductId = parentData.ParentProductId,
                                OrderPromoCodeId = giftProduct.PromoCodeId,
                                WarrantyId = giftProductDto.WarrantyId,
                                MaxBonusesToUse = giftProductDto.MaxBonusesToUse ?? 0,
                                ShowAdditionalServiceIcon = additionalService != null && ProductType.IsAdditionalServiceProductType(additionalService.ProductTypeId),
                                ShowAccessoryAdditionalServiceIcon = additionalService != null && ProductType.IsAccessoryAdditionalServiceProductType(additionalService.ProductTypeId)
                            };

                        int parentOrderPosition = Model.OrderProducts.IndexOf(parentOrderProduct);

                        giftOrderProducts.Add((orderGiftProductViewModel, parentOrderPosition));
                    }
                }

                List<PromoCodeProductViewItem> productChanges = (from p in response.Products
                    let op = p.IsGift ? giftOrderProducts.First(x => x.OrderProduct.ProductId == p.ProductId).OrderProduct : Model.OrderProducts.First(x => x.Id == p.Id)
                    select new PromoCodeProductViewItem
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        Name = op.ProductName,
                        NameUkr = op.ProductFullNameUa,
                        PromoCodeId = p.PromoCodeId,
                        PromoCode = response.PromoCodes.FirstOrDefault(x => x.PromoCodeId == p.PromoCodeId)?.PromoCodeValue,
                        Price = op.Price,
                        IsGift = p.IsGift,
                        CurrencyId = op.CurrencyId,
                        PriceCurrent = op.PriceOut,
                        Quantity = op.Quantity,
                        CurrencyCurrentId = op.CurrencyOutId,
                        BonusesToChargeCurrent = op.BonusesToCharge,
                        BonusesToChargeNew = ((p.BonusesToCharge ?? 0) >= op.BonusesToCharge ? (int?)p.BonusesToCharge : op.BonusesToCharge) ?? 0,
                        PriceNew = op.OrderPromoCodeId == null && p.PromoCodeId == null ? op.PriceOut : p.PriceNew ?? op.PriceOut,
                        CurrencyNewId = op.OrderPromoCodeId == null && p.PromoCodeId == null ? op.CurrencyOutId : op.CurrencyId
                    }).ToList();

                if (productChanges.Any())
                {
                    PromoCodeConfirmationViewModel confirmationViewModel = SizeableDialogDocumentManagerService.ShowView<PromoCodeConfirmationViewModel>(productChanges, this);

                    if (confirmationViewModel.IsOk)
                    {
                        foreach (PromoCodeProductViewItem item in confirmationViewModel.Items.Where(x => !x.IsGift))
                        {
                            CreateCompletedOrderProductViewItem orderProduct = Model.OrderProducts.First(x => x.ProductId == item.ProductId);

                            decimal priceDelta = orderProduct.PriceOut - item.PriceNew;

                            orderProduct.PromoDiscount = priceDelta > 0 ? priceDelta : 0;

                            orderProduct.PriceOut = item.PriceNew;
                            orderProduct.CurrencyOutId = item.CurrencyNewId;
                            orderProduct.OrderPromoCodeId = item.PromoCodeId;
                            orderProduct.Quantity = item.Quantity;
                            orderProduct.BonusesToCharge = item.BonusesToChargeNew ?? 0;
                        }

                        // Processing for new gifts

                        foreach ((CreateCompletedOrderProductViewItem OrderProduct, int Position) giftOrderProduct in giftOrderProducts)
                        {
                            Model.OrderProducts.Insert(giftOrderProduct.Position + 1, giftOrderProduct.OrderProduct);
                        }

                        // delete gifts from deleted promocode

                        int[] removedPromoCodeIds = Model.PromoCodes
                            .Select(x => x.PromoCodeId)
                            .Except(response.PromoCodes.Where(x => x.PromoCodeId.HasValue)
                                .Select(x => x.PromoCodeId.Value))
                            .ToArray();

                        CreateCompletedOrderProductViewItem[] giftsToDelete = Model.OrderProducts
                            .Where(x => x.OrderPromoCodeId.HasValue && removedPromoCodeIds.Contains(x.OrderPromoCodeId.Value) && x.IsGift)
                            .ToArray();

                        foreach (CreateCompletedOrderProductViewItem giftToDelete in giftsToDelete)
                        {
                            Model.OrderProducts.Remove(giftToDelete);
                        }

                        result = true;
                    }
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Нет изменений в ценах");
                }
            }

            return result;
        }

        private async Task<IEnumerable<OrderContractorViewItem>> FetchContractorsAsync(int? selectedContractorId)
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), UseCache).GetPagedResultDataAsync();

            return contractors
                .Where(x => (x.IsFolder == false && x.IsClient && x.Active) || x.Id == selectedContractorId)
                .Select(x => _mapper.Map<OrderContractorViewItem>(x));
        }

        private async Task<IEnumerable<OrderCityViewItem>> FetchCitiesAsync(int? selectedCityId)
        {
            List<CityDto> citiesList = await WebClient.ExecuteApiRequestAsync(new QueryCities(), UseCache).GetPagedResultDataAsync().ConfigureAwait(false);

            return citiesList
                .Where(x => x.Active || selectedCityId == x.Id)
                .Select(_mapper.Map<OrderCityViewItem>);
        }

        private async Task<IEnumerable<OrderWarehouseViewItem>> FetchWarehousesAsync(int? selectedWarehouseId)
        {
            _warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), UseCache).GetPagedResultDataAsync();

            return _warehouses
                .Where(x => x.Active == 1
                             && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id)
                             && (x.LocationId.HasValue || selectedWarehouseId == x.Id))
                .Select(x => _mapper.Map<OrderWarehouseViewItem>(x));
        }

        private void FetchCarryTypes(int? selectedCarryTypeId)
        {
            CarryTypes.ReplaceRange(Dictionaries.GetItems<CarryType>()
                .Where(x => x.Active || x.Id == selectedCarryTypeId)
                .OrderBy(x => x.Position)
                .Select(x => _mapper.Map(x, new OrderCarryTypeViewItem())));
        }

        private void FetchPayments(int? selectedPaymentId)
        {
            Payments.ReplaceRange(Dictionaries.GetItems<Payment>()
                .Where(x => (x.Active
                             && !Payment.IsCreditPayment(x.Id)
                             && x.Id != Payment.LiqPayId
                             && x.Id != Payment.MonoPayId
                             && x.Id != Payment.NovaPayId
                             && x.Id != Payment.PortmoneId
                             && x.Id != Payment.TerminalId) || selectedPaymentId == x.Id)
                .Select(x => _mapper.Map(x, new OrderPaymentViewItem())));
        }

        private async Task FetchWarehouseDeliveriesAsync()
        {
            _warehouseDeliveries = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseDeliveries(), UseCache);

            CarryTypes.Validate();
            Warehouses.Validate();

            Model.SelectedWarehouse = Warehouses.FirstOrDefault(x => x.Id == Model.SelectedWarehouse?.Id);
            Model.SelectedCarryType = CarryTypes.FirstOrDefault(x => x.Id == Model.SelectedCarryType?.Id);
        }

        private async Task FetchShopsAsync()
        {
            _locations = await WebClient.ExecuteApiRequestAsync(new QueryLocations(), true);
        }

        private async Task FetchEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            Employees = employees.ToReadOnlyObservableCollection();
        }

        private bool ContractorIsValid(OrderContractorViewItem item)
        {
            return WebClient.AuthenticatedEmployee.AllowSubdivisions.Contains(item.Subdivision.Id);
        }

        private void RemoveBonus(OrderBonusSummaryViewItem item)
        {
            BonusType bonusType = Dictionaries.GetItemById<BonusType>(item.BonusTypeId);

            if (!MessageFacadeService.Confirm($"Вы действительно хотите удалить бонусы \"{bonusType.Name}\" из заказа и вернуть их клиенту?"))
            {
                return;
            }

            CustomerBonusDto customerBonuses = Model.CustomerBonuses.First(x => x.BonusTypeId == item.BonusTypeId);

            foreach (CreateCompletedOrderProductViewItem orderProduct in Model.OrderProducts)
            {
                customerBonuses.Quantity += orderProduct.AppliedBonusesQuantity;
                orderProduct.PriceOut += orderProduct.AppliedBonusesQuantity;
                orderProduct.AppliedBonusesQuantity = 0;
            }

            AppliedBonuses.Remove(item);

            AppliedBonusesChanged();

            RefreshPaymentInfo();

            MessageFacadeService.ShowNotificationInfo("Бонусы удалены успешно");
        }

        private void EditBonus(OrderBonusSummaryViewItem item)
        {
            BonusConfirmationViewItem[] orderProducts = Model.OrderProducts
                .Where(x => x.BonusTypeId == item.BonusTypeId && x.AppliedBonusesQuantity > 0)
                .Select(x => new BonusConfirmationViewItem()
                {
                    OrderProductId = x.Id,
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    Price = x.Price,
                    PriceCurrent = x.PriceOut + x.AppliedBonusesQuantity,
                    Quantity = x.AppliedBonusesQuantity,
                    MaxQuantity = x.MaxBonusesToUse
                }).ToArray();

            CustomerBonusDto customerBonuses = Model.CustomerBonuses.First(x => x.BonusTypeId == item.BonusTypeId);

            BonusConfirmationParameter bonusConfirmationParameter = new BonusConfirmationParameter(customerBonuses.Quantity, orderProducts);

            BonusConfirmationViewModel bonusConfirmationViewModel = SizeableDialogDocumentManagerService.ShowView<BonusConfirmationViewModel>(bonusConfirmationParameter, this);

            if (!bonusConfirmationViewModel.IsOk)
            {
                return;
            }

            Prices toPay = OrderPaymentInfoViewModel.CalcToPayAfterUseBonuses(bonusConfirmationViewModel.Items.Sum(x => x.Quantity), Model);

            if (toPay.Uah < 0 || toPay.Usd < 0)
            {
                MessageFacadeService.ShowNotificationError("Сумма 'К оплате' не может стать отрицательной");
                return;
            }

            ProcessBonuses(bonusConfirmationViewModel.Items, customerBonuses);

            OrderBonusSummaryViewItem bonusViewItem = AppliedBonuses.First(x => x.BonusTypeId == item.BonusTypeId);

            bonusViewItem.ModifiedOn = DateTime.Now;
            bonusViewItem.Quantity = bonusConfirmationViewModel.Items.Sum(x => x.Quantity);

            AppliedBonusesChanged();

            RefreshPaymentInfo();

            MessageFacadeService.ShowNotificationInfo("Бонусы изменены успешно");
        }

        private void AddBonus()
        {
            List<BonusType> bonusTypes = BonusTypes
                .Where(x => Model.CustomerBonuses.Any(y => y.BonusTypeId == x.Id) && !AppliedBonuses?.Any(y => y.BonusTypeId == x.Id) == true)
                .ToList();

            if (!bonusTypes.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нет доступных бонусов");
                return;
            }

            SelectBonusTypeParameter parameter = new SelectBonusTypeParameter(bonusTypes);

            SelectBonusTypeViewModel bonusTypeViewModel = DialogDocumentManagerService.ShowView<SelectBonusTypeViewModel>(parameter, this);

            if (!bonusTypeViewModel.IsOk)
            {
                return;
            }

            int bonusTypeId = bonusTypeViewModel.SelectedBonus?.Id ?? 0;

            List<BonusConfirmationViewItem> bonusConfirmationItems = Model.OrderProducts
                .Where(x => x.BonusTypeId == bonusTypeId && x.OrderPromoCodeId == null)
                .SelectMany(x => Enumerable.Range(1, x.Quantity)
                    .Select(_ => new BonusConfirmationViewItem
                    {
                        MaxQuantity = x.MaxBonusesToUse,
                        OrderProductId = x.Id,
                        ProductId = x.ProductId,
                        Price = x.Price,
                        PriceCurrent = x.PriceOut,
                        ProductName = x.ProductName
                    }))
                .ToList();

            if (!bonusConfirmationItems.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Отсутствуют товары, к которым можно применить бонусы");
                return;
            }

            CustomerBonusDto customerBonuses = Model.CustomerBonuses.First(x => x.BonusTypeId == bonusTypeId);

            BonusConfirmationParameter bonusConfirmationParameter = new BonusConfirmationParameter(customerBonuses.Quantity, bonusConfirmationItems);

            BonusConfirmationViewModel bonusConfirmationViewModel = SizeableDialogDocumentManagerService.ShowView<BonusConfirmationViewModel>(bonusConfirmationParameter, this);

            if (!bonusConfirmationViewModel.IsOk)
            {
                return;
            }

            Prices toPay = OrderPaymentInfoViewModel.CalcToPayAfterUseBonuses(bonusConfirmationViewModel.Items.Sum(x => x.Quantity), Model);

            if (toPay.Uah < 0 || toPay.Usd < 0)
            {
                MessageFacadeService.ShowNotificationError("Сумма 'К оплате' не может стать отрицательной");
                return;
            }

            ProcessBonuses(bonusConfirmationViewModel.Items, customerBonuses);

            AppliedBonuses.Add(new OrderBonusSummaryViewItem
            {
                BonusTypeId = bonusTypeId,
                ModifiedBy = WebClient.AuthenticatedEmployee.Id,
                ModifiedOn = DateTime.Now,
                Quantity = bonusConfirmationViewModel.Items.Sum(x => x.Quantity)
            });

            AppliedBonuses = AppliedBonuses?
                .GroupBy(x => x.BonusTypeId)
                .Select(x => new OrderBonusSummaryViewItem
                {
                    BonusTypeId = x.Key,
                    Quantity = x.Sum(y => y.Quantity),
                    ModifiedBy = x.First().ModifiedBy,
                    ModifiedOn = x.First().ModifiedOn
                }).ToObservableCollection();

            AppliedBonusesChanged();

            MessageFacadeService.ShowNotificationInfo("Бонусы применены успешно");
        }

        private bool WarehouseIsValid(OrderWarehouseViewItem item)
        {
            LocationEntityDto[] location = _locations.Where(x => x.CityId == Model.SelectedCity?.Id).ToArray();

            return Model?.SelectedCarryType?.Valid == true
                   && item.Active
                   && (!Model.SelectedCarryType.IsLocal || location.Any(x => x.CityId == item.CityId && x.LocationTypeId == LocationType.ShopId))
                   && _warehouseDeliveries.Any(x =>
                       x.WarehouseId == item.Id
                       && x.CarryIds.Contains(Model.SelectedCarryType.Id)
                       && (x.SubdivisionId == null || x.SubdivisionId == Model.SelectedContractor?.Subdivision.Id));
        }

        private bool CarryTypeIsValid(OrderCarryTypeViewItem item)
        {
            return true;
        }

        private bool PaymentIsValid(OrderPaymentViewItem item)
        {
            return true;
        }

        private void ClearAddressFields()
        {
            Model.DeliveryData = new DeliveryDataDto();
            Model.Address = null;
        }

        private void RefreshPaymentInfo()
        {
            OrderPaymentViewModel.CalcPaymentInfo(Model);
        }

        private void SelectedOrderProductChanged()
        {
            ProductInformation.ClearProduct();

            if (SelectedOrderProduct != null)
            {
                ProductInformation.ProductId = new ProductInfoId(SelectedOrderProduct.ProductId, SelectedOrderProduct.CurrencyOutId, Model?.SelectedContractor?.Id);
            }
        }

        private void RecognizeBarcodeViewModelOnStarted(object sender, EventArgs e)
        {
            IsRecognitionInProgress = true;
        }

        private async Task HandleRowDoubleClickAsync(RowDoubleClickInfo e)
        {
            CreateCompletedOrderProductViewItem orderProduct = (CreateCompletedOrderProductViewItem)e.Data;

            if (e.FieldName.Equals(nameof(OrderProductViewModel.AnyAdditionalServices)))
            {
                if (orderProduct.AnyAdditionalServices)
                {
                    await SelectAdditionalServicesAsync(orderProduct);
                }
            }
        }

        private async Task SelectAdditionalServicesAsync(CreateCompletedOrderProductViewItem orderProduct)
        {
            if (Model.SelectedContractor is null)
            {
                MessageFacadeService.ShowNotificationWarning("Контрагент не выбран");
                return;
            }

            if (Model.SelectedPayment is null)
            {
                MessageFacadeService.ShowNotificationWarning("Способ оплаты не выбран");
                return;
            }

            SelectAdditionalServicesParameter selectAdditionalServicesParameter = new SelectAdditionalServicesParameter(
                orderProduct.ProductId,
                orderProduct.ProductName,
                orderProduct.PriceOut,
                Model.SelectedContractor.Id,
                Model.SelectedPayment.Id,
                null,
                true,
                _additionalServiceProductTypeIds);

            SelectAdditionalServicesViewModel viewModel = DialogDocumentManagerService.ShowView<SelectAdditionalServicesViewModel>(selectAdditionalServicesParameter, this);

            if (!viewModel.IsOk || viewModel.SelectedAdditionalServices?.Any() != true)
            {
                return;
            }

            PagedResult<ProductAttributesDto> productAttributes = await WebClient
                .ExecuteApiRequestAsync(new QueryProductsAttributesByIds(viewModel.SelectedAdditionalServices.Select(x => x.ProductId).ToArray(), true));

            foreach (ProductAttributesDto productAttribute in productAttributes.Data)
            {
                AdditionalServiceCatalogViewItem additionalService = viewModel.SelectedAdditionalServices.First(x => x.ProductId == productAttribute.ProductId);

                await AddProductAsync(
                    productAttribute,
                    orderProduct.Quantity,
                    additionalService.Price,
                    orderProduct.Id,
                    additionalService.ProductTypeId,
                    orderProduct.ProductId);
            }
        }

        private async Task RecognizeBarcodeViewModelOnFinishedAsync(RecognizeBarcodeResultEventArgs e)
        {
            IsRecognitionInProgress = false;

            switch (e.Result)
            {
                case RecognizeBarcodeResult.Found:
                case RecognizeBarcodeResult.FoundInSupplier:
                    e.Message = await AddProductAsync(e.Product, e.Quantity);
                    break;
                case RecognizeBarcodeResult.NotFound:
                    e.Message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "ШК не найден в БД");
                    break;
            }
        }

        private async Task<RecognizeBarcodeMessage> AddProductAsync(
            ProductAttributesDto product,
            int quantity,
            decimal? price = null,
            int? parentRecordId = null,
            int? additionalServiceProductTypeId = null,
            int? parentProductId = null)
        {
            int[] productIdArray = { product.ProductId };

            CreateCompletedOrderProductViewItem productViewItem = Model.OrderProducts
                .FirstOrDefault(x => x.ProductId == product.ProductId && ((x.Quantity - x.ScannedQuantity) > 0 || x.ParentRecordId == null));

            ProductDto catalogProduct;

            if (productViewItem == null)
            {
                if (Model.SelectedContractor == null)
                {
                    return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "Контрагент не выбран");
                }

                QueryProductByIdsDto dto = new QueryProductByIdsDto(productIdArray, Model.SelectedContractor.Id)
                {
                    Gifts = true,
                    IncludePrices = true,
                    CartProductIds = Model.OrderProducts.Select(x => x.ProductId).ToArray()
                };

                Task<List<ProductDto>> catalogProductsTask = WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(dto));
                Task<PagedResult<AdditionalServiceDto>> additionalServicesTask = WebClient.ExecuteApiRequestAsync(
                    new QueryAdditionalServices(
                        new AdditionalServicesFilteringItem(productIdArray, true)), true);

                await Task.WhenAll(catalogProductsTask, additionalServicesTask);

                catalogProduct = catalogProductsTask.Result.FirstOrDefault(x => x.Id == product.ProductId);

                if (catalogProduct == null)
                {
                    return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "Товар не найден");
                }

                productViewItem = new CreateCompletedOrderProductViewItem
                {
                    Id = _idGenerator.GetNext(),
                    ProductId = catalogProduct.Id,
                    ProductName = catalogProduct.GetLocalName(LocalizableNameType.Ukr),
                    BonusTypeId = catalogProduct.BonusTypeId,
                    MaxBonusesToUse = catalogProduct.MaxBonusesToUse ?? 0,
                    ProductFullNameUa = catalogProduct.NameFullUa,
                    Prices = catalogProduct.Prices,
                    Quantity = 0,
                    ScannedQuantity = 0,
                    ShowAdditionalServiceIcon = additionalServiceProductTypeId == ProductType.ServiceId
                                                || additionalServiceProductTypeId == ProductType.CertificateId
                                                || additionalServiceProductTypeId == ProductType.ServiceCertificateId,
                    ShowAccessoryAdditionalServiceIcon = additionalServiceProductTypeId == ProductType.AccessoryId,
                    WarrantyId = catalogProduct.WarrantyId,
                    PriceOut = price ?? catalogProduct.Price,
                    CurrencyOutId = catalogProduct.CurrencyId,
                    Price = catalogProduct.Price,
                    CurrencyId = catalogProduct.CurrencyId,
                    ParentRecordId = parentRecordId,
                    ParentProductId = parentProductId,
                    LinkRewrite = catalogProduct.LinkRewrite,
                    AnyAdditionalServices = additionalServicesTask.Result.Data?
                        .Where(x => _additionalServiceProductTypeIds.Contains(x.ProductTypeId))
                        .Any(x => x.AppliedToProductIds.Contains(catalogProduct.Id)) == true,
                    ProductTypeId = catalogProduct.TypeId
                };

                productViewItem.PropertyChanged += ProductOnPropertyChanged;

                Model.OrderProducts.Add(productViewItem);

                foreach (ProductDto gift in catalogProduct.Gifts)
                {
                    CreateCompletedOrderProductViewItem productGiftViewItem = new CreateCompletedOrderProductViewItem
                    {
                        Id = _idGenerator.GetNext(),
                        ProductId = gift.Id,
                        ProductName = gift.GetLocalName(LocalizableNameType.Ukr),
                        BonusTypeId = null,
                        MaxBonusesToUse = gift.MaxBonusesToUse ?? 0,
                        ProductFullNameUa = gift.NameFullUa,
                        Quantity = 0,
                        WarrantyId = gift.WarrantyId,
                        ParentRecordId = productViewItem.Id,
                        ParentProductId = productViewItem.ProductId,
                        PriceOut = gift.Price,
                        CurrencyOutId = gift.CurrencyId,
                        Price = gift.Price,
                        CurrencyId = gift.CurrencyId,
                        LinkRewrite = gift.LinkRewrite,
                        IsGift = true,
                        ProductTypeId = gift.TypeId
                    };

                    productGiftViewItem.PropertyChanged += ProductOnPropertyChanged;

                    Model.OrderProducts.Add(productGiftViewItem);
                }
            }

            if (productViewItem.ScannedQuantity == 0)
            {
                productViewItem.PrintWarrantyCard = product.PrintWarrantyCard;
                productViewItem.KeepSerial = product.KeepSerial;
                productViewItem.KeepSerialOverridden = product.KeepSerial;
            }

            if (productViewItem.KeepSerialOverridden)
            {
                string[] barcodes = RecognizeBarcodeViewModel.GetBarcodesById(product.ProductId).ToArray();

                ProductSerialsViewModelParameter parameter = new ProductSerialsViewModelParameter(
                    productViewItem.ProductId,
                    productViewItem.Serials.ToList(),
                    barcodes,
                    product.SerialNumberLength,
                    _scanSerialMode,
                    product.Serials);

                ProductScanSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductScanSerialsViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    productViewItem.Serials.AddRange(viewModel.SerialNumbers);
                    productViewItem.ScannedQuantity = productViewItem.Serials.Count;

                    if (!productViewItem.IsGift)
                    {
                        productViewItem.Quantity = productViewItem.Serials.Count;
                        CreateCompletedOrderProductViewItem item = productViewItem;
                        CreateCompletedOrderProductViewItem viewItem = productViewItem;
                        Model.OrderProducts.Where(x => x.ParentRecordId == viewItem.Id).ForEach(x => x.Quantity = item.Quantity);
                    }

                    _scanSerialMode = viewModel.ScanMode;
                }

                if (productViewItem.Quantity == 0)
                {
                    DeleteProduct(productViewItem);

                    productViewItem = null;
                }
            }
            else if (productViewItem.IsGift)
            {
                int needQuantity = quantity - productViewItem.Quantity;

                productViewItem.ScannedQuantity = Math.Min(productViewItem.ScannedQuantity + quantity, productViewItem.Quantity);

                if (needQuantity > 0)
                {
                    await AddProductAsync(product, needQuantity);
                }
            }
            else
            {
                productViewItem.Quantity += quantity;
                productViewItem.ScannedQuantity += quantity;

                Model.OrderProducts.Where(x => x.ParentRecordId == productViewItem.Id).ForEach(x => x.Quantity += quantity);
            }

            SelectedOrderProduct = productViewItem;

            RefreshPaymentInfo();

            return null;
        }

        private void ProcessBonuses(ObservableCollection<BonusConfirmationViewItem> items, CustomerBonusDto customerBonuses)
        {
            foreach (BonusConfirmationViewItem bonusItem in items)
            {
                CreateCompletedOrderProductViewItem orderProduct = Model.OrderProducts.First(x => x.Id == bonusItem.OrderProductId);

                customerBonuses.Quantity -= bonusItem.Quantity;
                orderProduct.AppliedBonusesQuantity = bonusItem.Quantity;
                orderProduct.PriceOut = bonusItem.PriceNew;
            }
        }

        private void ProductOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshPaymentInfo();
        }

        private void AppliedBonusesChanged()
        {
            if (AppliedBonuses?.Any() == true)
            {
                AllowChangePhone = false;
                PhoneToolTip = "Запрещено изменять телефон, когда применены бонусы";
            }
            else
            {
                PhoneToolTip = null;
                AllowChangePhone = true;
            }

            RaisePropertyChanged(nameof(AppliedBonusesQuantity));
        }

        private OrderCreatePackedDto GetCreateDto()
        {
            OrderCreatePackedDto createDto = new OrderCreatePackedDto
            {
                WorkPlaceId = WebClient.WorkPlaceId,
                IgnoreRecalculatingPriceOutAfterBonusApply = true,
                SubdivisionId = Model.SelectedContractor.Subdivision.Id,
                ClientId = Model.SelectedContractor.Id,
                PaymentId = Model.SelectedPayment.Id,
                FillSources = false,
                LastName = Model.LastName,
                FirstName = Model.FirstName,
                MiddleName = Model.MiddleName,
                Address = Model.Address,
                DeliveryData = Model.DeliveryData,
                CarryId = Model.SelectedCarryType.Id,
                Email = Model.Email,
                EmployeeComment = Model.Comment,
                Phone = Model.Phone,
                Phone2 = Model.Phone2,
                WarehouseId = Model.SelectedWarehouse.Id,
                LocationId = Model.SelectedWarehouse.LocationId,
                CityId = Model.SelectedCity.Id,
                OrderSourceId = OrderSourceType.StoreId,
                Options = new OrderOptionsDto
                {
                    SeparateWarrantyCards = Model.SeparateWarrantyCards,
                    FreeDelivery = false,
                    DontCall = true
                },
                OrderProducts = Model.OrderProducts.Select(x => new OrderProductSaveDto()
                {
                    Id = x.Id,
                    CurrencyOutId = x.CurrencyOutId,
                    CurrencyId = x.CurrencyId,
                    PriceOut = x.PriceOut,
                    Price1C = 0,
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    PriceId = CalculatePriceId(x.Prices, x.PriceOut, x.ProductId, x.CurrencyOutId),
                    ParentRecordId = x.ParentRecordId,
                    OrderPromoCodeId = x.OrderPromoCodeId,
                    BonusesToCharge = x.BonusesToCharge,
                    PromoDiscount = x.PromoDiscount,
                    IsGift = x.IsGift,
                    IsAdditionalService = x.ShowAdditionalServiceIcon || x.ShowAccessoryAdditionalServiceIcon
                }).ToList(),
                PromoCodes = Model.PromoCodes.Select(x => new OrderProductPromoCodeSaveDto
                {
                    PromoCodeId = x.PromoCodeId
                }).ToList(),
                SerialNumbers = Model.OrderProducts
                    .GroupBy(x => x.ProductId)
                    .Select(x => new OrderCreatePackedSerialNumberDto(x.Key, x.SelectMany(z => z.Serials).ToArray()))
                    .ToList(),
                Bonuses = Model.OrderProducts.Where(x => x.BonusTypeId.HasValue && x.AppliedBonusesQuantity > 0).Select(x => new OrderBonusSaveDto()
                {
                    BonusTypeId = x.BonusTypeId.Value,
                    OrderProductId = x.Id,
                    Quantity = x.AppliedBonusesQuantity
                }).ToList()
            };

            return createDto;
        }

        private int? CalculatePriceId(IReadOnlyCollection<ProductPriceSimpleDto> prices, decimal productPrice, int productId, int currencyId)
        {
            if (prices is null)
            {
                Logger.LogError("ParsedPriceJson is null. Failed to calculate priceId for createCompletedOrderProduct. ProductId: {ProductId}", productId);
                return null;
            }

            foreach (ProductPriceKind priceKind in PriceKinds)
            {
                ProductPriceSimpleDto productPriceDto = prices.FirstOrDefault(x => x.PriceTypeId == priceKind.Id);

                if (productPriceDto != null && productPriceDto.CurrencyId == currencyId && productPrice == productPriceDto.Price)
                {
                    return priceKind.Id;
                }
            }

            return null;
        }

        private bool CanAutoSource()
        {
            return Model?.OrderProducts.Any() == true &&
                   Model.SelectedContractor != null &&
                   Model.SelectedPayment != null &&
                   Model.SelectedCity != null &&
                   Model.SelectedCarryType != null &&
                   Model.SelectedWarehouse != null;
        }

        private async Task AutoSourceAsync()
        {
            CleanProductSources();

            IReadOnlyCollection<ProductSourceResultDto> productSourceResult = await FetchProductSourcesAsync();

            foreach (ProductSourceResultDto productSource in productSourceResult.OrderByDescending(x => x.Id))
            {
                if (productSource.Source == null)
                {
                    continue;
                }

                if (productSource.Source.Type == ProductSourceType.Warehouse)
                {
                    int? parentProductId = productSourceResult.FirstOrDefault(x => x.Children?.Any(y => y.Id == productSource.Id) == true)?.ProductId;

                    IReadOnlyCollection<CreateCompletedOrderProductViewItem> orderProductsToSetSource = Model.OrderProducts.OrderBy(x => x.ParentProductId)
                        .Where(x => x.ProductId == productSource.Source.ProductId && x.ParentProductId == parentProductId).ToArray();

                    CreateCompletedOrderProductViewItem orderProductToSetSource = orderProductsToSetSource.FirstOrDefault(x => x.Quantity == productSource.Quantity && x.Source == null);

                    if (orderProductToSetSource != null)
                    {
                        orderProductToSetSource.Source = Dictionaries.GetOrderProductSource(
                            OrderProductSourceType.WarehouseSourceId,
                            productSource.Source.WarehouseId,
                            GetSourceText(productSource.Source.WarehouseId),
                            productSource.Source.AvailOn);
                    }
                    else
                    {
                        CreateCompletedOrderProductViewItem orderProductToSplit = orderProductsToSetSource.OrderBy(x => x.Quantity).FirstOrDefault(x => x.Quantity > productSource.Quantity);

                        if (orderProductToSplit != null)
                        {
                            Model.OrderProducts = SetPosition(SplitOrderProducts(Model.OrderProducts.ToArray(), orderProductToSplit, orderProductToSplit.Quantity - productSource.Quantity)).OrderBy(x => x.Position).ToObservableCollection();

                            orderProductToSplit.Source = Dictionaries.GetOrderProductSource(
                                OrderProductSourceType.WarehouseSourceId,
                                productSource.Source.WarehouseId,
                                GetSourceText(productSource.Source.WarehouseId),
                                productSource.Source.AvailOn);
                        }
                    }
                }
            }

            if (Model.OrderProducts.All(x => x.Source?.WarehouseId > 0) != true)
            {
                MessageFacadeService.ShowMessageBoxWarning("Недостачно товаров на свободном остатке в магазине");
            }
        }

        public IReadOnlyCollection<CreateCompletedOrderProductViewItem> SetPosition(IReadOnlyCollection<CreateCompletedOrderProductViewItem> items)
        {
            items.ForEach(x => x.Position = 0);

            int maxPosition = 0;

            foreach (CreateCompletedOrderProductViewItem item in items.OrderByDescending(x => x.Id))
            {
                if (item.Position == 0)
                {
                    item.Position = maxPosition + 1;

                    maxPosition = item.Position;

                    CreateCompletedOrderProductViewItem childItem = items.FirstOrDefault(x => x.ParentRecordId == item.Id && x.ParentProductId == item.ProductId);

                    if (childItem != null)
                    {
                        childItem.Position = maxPosition + 1;

                        maxPosition = childItem.Position;
                    }
                }
            }

            return items;
        }

        private async Task<IReadOnlyCollection<ProductSourceResultDto>> FetchProductSourcesAsync()
        {
            IReadOnlyCollection<ProductSourcesProductDto> productsRequest = Model.OrderProducts.Select(x => new ProductSourcesProductDto()
            {
                Id = x.Id,
                ProductId = x.ProductId,
                CurrencyOutId = x.CurrencyOutId,
                PriceOut = x.PriceOut,
                Quantity = x.Quantity,
                ParentRecordId = x.ParentRecordId,
                IsAdditionalService = x.ShowAdditionalServiceIcon || x.ShowAccessoryAdditionalServiceIcon
            }).ToArray();

            QueryAutoSourceProducts.ProductSourcesRequest productSourcesRequest = new QueryAutoSourceProducts.ProductSourcesRequest(
                Model.SelectedContractor.Subdivision.Id,
                Model.SelectedCarryType.Id,
                Model.SelectedPayment.Id,
                Model.SelectedWarehouse.Id,
                Model.SelectedCity.Id,
                _warehouses.Where(x => (x.LocationId.HasValue && x.LocationId == Model.SelectedWarehouse.LocationId) || x.Id == Model.SelectedWarehouse.Id).Select(x => x.Id).ToArray(),
                productsRequest);

            return await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryAutoSourceProducts(productSourcesRequest)),
                "получении свободных остатков",
                null,
                this,
                true,
                false);
        }

        private void CleanProductSources()
        {
            foreach (CreateCompletedOrderProductViewItem item in Model.OrderProducts)
            {
                item.Source = null;
            }
        }

        private IReadOnlyCollection<CreateCompletedOrderProductViewItem> SplitOrderProducts(CreateCompletedOrderProductViewItem[] orderProducts, CreateCompletedOrderProductViewItem orderProductToSplit, int splitQuantity)
        {
            List<CreateCompletedOrderProductViewItem> newOrderProducts = new List<CreateCompletedOrderProductViewItem>();

            for (int i = 0; i < orderProducts.Length; i++)
            {
                CreateCompletedOrderProductViewItem item = orderProducts[i];

                newOrderProducts.Add(item);

                if (item.Id == orderProductToSplit.Id)
                {
                    int newQuantity = item.Quantity - splitQuantity;
                    item.Quantity = newQuantity;

                    CreateCompletedOrderProductViewItem newItem = (CreateCompletedOrderProductViewItem)item.Clone();
                    newItem.Id = _idGenerator.GetNext();
                    newItem.Quantity = splitQuantity;

                    SplitSerials(item, newItem, splitQuantity);

                    newOrderProducts.Add(newItem);

                    CreateCompletedOrderProductViewItem childItem = orderProducts.FirstOrDefault(x => x.ParentProductId == item.ProductId && x.ParentRecordId == item.Id);

                    if (childItem != null && childItem.Quantity > splitQuantity)
                    {
                        int newChildQuantity = childItem.Quantity - splitQuantity;
                        childItem.Quantity = newChildQuantity;

                        CreateCompletedOrderProductViewItem newChildItem = (CreateCompletedOrderProductViewItem)childItem!.Clone();

                        newChildItem.Id = _idGenerator.GetNext();
                        newChildItem.Quantity = splitQuantity;
                        newChildItem.ParentRecordId = newItem.Id;
                        newChildItem.ParentProductId = newItem.ProductId;

                        SplitSerials(childItem, newChildItem, splitQuantity);

                        newOrderProducts.Add(newChildItem);
                    }
                }
            }

            return newOrderProducts;
        }

        private void SplitSerials(CreateCompletedOrderProductViewItem splitItem, CreateCompletedOrderProductViewItem newItem, int splitQuantity)
        {
            if (splitItem.Serials?.Any() == true && splitItem.Serials.Count > newItem.Quantity)
            {
                string[] serials = splitItem.Serials.Take(newItem.Quantity).ToArray();

                newItem.Serials.AddRange(serials);
                newItem.ScannedQuantity = serials.Length;

                splitItem.Serials.RemoveRange(serials);
                int newScannedQuantity = splitItem.ScannedQuantity - newItem.ScannedQuantity;
                splitItem.ScannedQuantity = newScannedQuantity;
            }
            else
            {
                splitItem.ScannedQuantity = Math.Max(splitItem.ScannedQuantity - splitQuantity, 0);
                newItem.ScannedQuantity = Math.Max(splitQuantity, 0);
            }
        }

        private async Task<IReadOnlyCollection<int>> MovementProcessingAsync(IReadOnlyCollection<CreateCompletedOrderProductViewItem> orderProducts)
        {
            MovementProductSaveDto[] movementProducts = GetMovementProductSaveDto(orderProducts);

            IReadOnlyCollection<ValidationResultItemDto> canConfirmValidationItems = await WebClient.ExecuteApiRequestAsync(new CanFastMovementProccesing(movementProducts));

            if (canConfirmValidationItems?.Any() == true)
            {
                MessageFacadeService.ShowValidationResultView("Ошибки при создании перемещения", canConfirmValidationItems.Select(x => new ValidationResultItem(x.Message, true)), this);

                return null;
            }

            List<int> movementIds = new List<int>();

            foreach (IGrouping<int, CreateCompletedOrderProductViewItem> movementOrderProductGroup in orderProducts.GroupBy(x => x.Source.WarehouseId))
            {
                if (movementOrderProductGroup.Any())
                {
                    MovementDto movement = await CreateMovementFastCompletedOrderAsync(movementOrderProductGroup.Key, Model.SelectedWarehouse.Id, movementOrderProductGroup.ToArray());

                    if (movement != null)
                    {
                        movementOrderProductGroup.ForEach(x =>
                            x.Source = Dictionaries.GetOrderProductSource(
                                OrderProductSourceType.WarehouseSourceId,
                                Model.SelectedWarehouse.Id,
                                GetSourceText(Model.SelectedWarehouse.Id),
                                x.Source.SourceDate));

                        movementIds.Add(movement.Id);
                    }
                    else
                    {
                        CleanProductSources();
                    }
                }
            }

            return movementIds;
        }

        private async Task<MovementDto> CreateMovementFastCompletedOrderAsync(int fromWarehouseId, int toWarehouseId, IReadOnlyCollection<CreateCompletedOrderProductViewItem> orderProducts)
        {
            MovementDto movement = await CreateEmptyMovementAsync(fromWarehouseId, toWarehouseId);

            if (movement == null)
            {
                return null;
            }

            movement = await AddProductsToMovementAsync(movement!.Id, orderProducts);

            if (movement == null)
            {
                return null;
            }

            movement = await FastSendReceiveMovementAsync(movement!.Id);

            return movement;
        }

        private async Task<MovementDto> CreateEmptyMovementAsync(int fromWarehouseId, int toWarehouseId)
        {
            CreateMovement gatewayRequest = new CreateMovement(
                fromWarehouseId,
                toWarehouseId,
                DateTime.Now,
                DateTime.Now.AddMinutes(1),
                DateTime.Now.AddMinutes(2),
                DateTime.Now.AddMinutes(3),
                false,
                true,
                null,
                null,
                new[] { WarehouseRouteTimePurpose.Orders.Id },
                false,
                true,
                true);

            string nameWarehouseFrom = _warehouses.FirstOrDefault(x => x.Id == fromWarehouseId)?.Name;

            Result<MovementDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(gatewayRequest),
                $"создании перемещения со склада \"{nameWarehouseFrom}\"",
                null,
                this,
                true);

            return result?.IsSuccess == true ? result.Data : null;
        }

        private async Task<MovementDto> AddProductsToMovementAsync(int movementId, IReadOnlyCollection<CreateCompletedOrderProductViewItem> orderProducts)
        {
            MovementProductSaveDto[] movementProducts = GetMovementProductSaveDto(orderProducts);

            MovementUpdateDto dto = new MovementUpdateDto()
            {
                Products = movementProducts
            };

            Result<MovementDto> updateResult = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new UpdateMovement(movementId, dto)),
                $"добавление товаров в перемещение №{movementId}",
                null,
                this,
                true);

            return updateResult?.IsSuccess == true ? updateResult.Data : null;
        }

        private async Task<MovementDto> FastSendReceiveMovementAsync(int movementId)
        {
            LockResponse<MovementDto> lockedEntity = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new LockMovement(movementId)),
                $"блокеровке перемещения {movementId}",
                null,
                this,
                true);

            if (lockedEntity?.Success != true)
            {
                return null;
            }

            Result<MovementDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new FastSendMovement(movementId, true, true)),
                $"быстром отправлении перемещения №{movementId}",
                null,
                this,
                true);

            if (result?.IsSuccess == true)
            {
                result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new ReceiveMovement(movementId, true)),
                    $"получении перемещения №{movementId}",
                    null,
                    this,
                    true);
            }

            await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new UnlockMovement(movementId)),
                $"разблокеровке перемещения №{movementId}",
                null,
                this,
                true);

            return result?.IsSuccess == true ? result.Data : null;
        }

        private string GetSourceText(int warehouseId)
        {
            string nameWarehouse = _warehouses?.FirstOrDefault(x => x.Id == warehouseId)?.Name;

            return $"На складе: {nameWarehouse}";
        }

        private MovementProductSaveDto[] GetMovementProductSaveDto(IReadOnlyCollection<CreateCompletedOrderProductViewItem> orderProducts)
        {
            return orderProducts
                .GroupBy(x => x.ProductId)
                .Select(
                    x =>
                        new MovementProductSaveDto(
                            x.Key,
                            x.Sum(y => y.Quantity),
                            x.Sum(y => y.Quantity),
                            0,
                            x.Where(y => y.ProductTypeId == ProductType.AssembledComputerRuleId && y.Serials?.Any() == true).SelectMany(y => GetMovementProductSn(y)).ToList()))
                .ToArray();

            MovementProductSnDto[] GetMovementProductSn(CreateCompletedOrderProductViewItem item)
            {
                return item.Serials.Select(
                    x => new MovementProductSnDto()
                    {
                        Id = 0,
                        ScannedIn = false,
                        ScannedOut = true,
                        NomenclatureSeries = x
                    }).ToArray();
            }
        }
    }
}