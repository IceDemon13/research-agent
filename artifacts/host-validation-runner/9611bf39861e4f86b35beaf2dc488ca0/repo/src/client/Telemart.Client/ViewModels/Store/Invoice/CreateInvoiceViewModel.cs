using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Contractor.Warehouse;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class CreateInvoiceViewModel : TelemartDialogViewModelBase
    {
        public CreateInvoiceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            Editable = WebClient.IsOperationAllowed(BusinessOperation.InvoiceCustomCreate);

            ClearCommand = new DelegateCommand(Clear);
            HandleDateFromChangedCommand = new AsyncCommand(DateFromChangedCallbackAsync);
            HandleSelectedSupplierChangedCommand = new AsyncCommand(SelectedSupplierChangedCallbackAsync);
        }

        public CreateInvoiceViewModel()
        {
        }

        #region Commands

        public IDelegateCommand ClearCommand { get; }

        public IAsyncCommand HandleDateFromChangedCommand { get; }

        public IAsyncCommand HandleSelectedSupplierChangedCommand { get; }

        #endregion

        #region INPC

        public CarryType CarryType
        {
            get { return GetProperty(() => CarryType); }
            set { SetProperty(() => CarryType, value); }
        }

        public DateTime? DateClose
        {
            get { return GetProperty(() => DateClose); }
            set { SetProperty(() => DateClose, value); }
        }

        public DateTime? DateFrom
        {
            get { return GetProperty(() => DateFrom); }
            set { SetProperty(() => DateFrom, value); }
        }

        public DateTime DateFromMax => DateMin.AddDays(60);

        public DateTime? DateArrive
        {
            get { return GetProperty(() => DateArrive); }
            set { SetProperty(() => DateArrive, value); }
        }

        public DateTime? DateGet
        {
            get { return GetProperty(() => DateGet); }
            set { SetProperty(() => DateGet, value); }
        }

        public DateTime DateMax => DateMin.AddDays(65);

        public DateTime DateMin => DateTime.Today;

        public bool Editable { get; }

        public InvoiceTemplateViewItem SelectedInvoiceTemplate
        {
            get
            {
                return GetProperty(() => SelectedInvoiceTemplate);
            }

            set
            {
                SetProperty(() => SelectedInvoiceTemplate, value, () =>
                {
                    if (SelectedInvoiceTemplate == null)
                    {
                        ClearProperties();
                    }
                    else
                    {
                        SupplierWarehouseId = SelectedInvoiceTemplate.SupplierWarehouseId;
                        WarehouseId = SelectedInvoiceTemplate.WarehouseId;
                        CarryType = SelectedInvoiceTemplate.CarryType;
                        DateClose = SelectedInvoiceTemplate.DateClose;
                        DateGet = SelectedInvoiceTemplate.DateGet;
                        DateArrive = SelectedInvoiceTemplate.DateArrive;
                    }
                });
            }
        }

        public ContractorDto SelectedSupplier
        {
            get { return GetProperty(() => SelectedSupplier); }
            set { SetProperty(() => SelectedSupplier, value); }
        }

        public int? SupplierWarehouseId
        {
            get { return GetProperty(() => SupplierWarehouseId); }
            set { SetProperty(() => SupplierWarehouseId, value); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        #endregion

        #region Collections

        public ObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public ObservableCollection<InvoiceTemplateViewItem> InvoiceTemplates
        {
            get { return GetProperty(() => InvoiceTemplates); }
            private set { SetProperty(() => InvoiceTemplates, value); }
        }

        public ObservableCollection<ContractorDto> Suppliers
        {
            get { return GetProperty(() => Suppliers); }
            private set { SetProperty(() => Suppliers, value); }
        }

        public ObservableCollection<ComboBoxItem> SupplierWarehouses
        {
            get { return GetProperty(() => SupplierWarehouses); }
            private set { SetProperty(() => SupplierWarehouses, value); }
        }

        public ObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public bool OpenWithSupplier
        {
            get { return GetProperty(() => OpenWithSupplier); }
            private set { SetProperty(() => OpenWithSupplier, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<CreateInvoiceViewModel> builder)
        {
            builder.Property(x => x.SelectedSupplier).Required(() => "Поставщик должен быть заполнен");
            builder.Property(x => x.CarryType).Required(() => "Доставка должна быть заполнена");
            builder.Property(x => x.SupplierWarehouseId).Required(() => "Склад поставщика должен быть заполнен");
            builder.Property(x => x.WarehouseId).Required(() => "Склад должен быть заполнен");
            builder.Property(x => x.DateClose).Required(() => "Дата закрытия должна быть заполнена");
            builder.Property(x => x.DateGet).Required(() => "Дата поступления должна быть заполнена");
            builder.Property(x => x.DateArrive).Required(() => "Дата прибытия должна быть заполнена");
        }

        protected override async Task HandleLoadedAsync()
        {
            Title = "Создание накладной";

            CarryTypes = Dictionaries.GetItems<CarryType>().Where(x => x.IsActive()).ToObservableCollection();

            await Task.WhenAll(RefreshWarehousesAsync(), RefreshContractorsAsync());

            CreateInvoiceParameter parameter = Parameter as CreateInvoiceParameter;

            if (parameter != null)
            {
                SelectedSupplier = Suppliers.FirstOrDefault(x => x.Id == parameter.SupplierId);
            }

            OpenWithSupplier = parameter != null;

            async Task RefreshWarehousesAsync()
            {
                List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
                Warehouses = warehouses
                    .Where(x => (x.TypeId == WarehouseKind.Main.Id || x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.ShowCase.Id || x.TypeId == WarehouseKind.Assembly.Id) && x.Active == 1)
                    .OrderByDescending(x => x.Position)
                    .ToObservableCollection();
            }

            async Task RefreshContractorsAsync()
            {
                List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
                Suppliers = contractors
                    .Where(x => x.IsSupplier && x.Active && !x.IsFolder)
                    .OrderBy(x => x.Name)
                    .ToObservableCollection();
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (SupplierWarehouseId == null || WarehouseId == null || CarryType == null || DateGet == null || DateClose == null)
            {
                return;
            }

            if (DateClose.Value >= DateGet.Value)
            {
                MessageFacadeService.ShowNotificationWarning("Время закрытия накладной должно быть меньше времени получения");
                return;
            }

            if (DateGet < DateTime.Now)
            {
                MessageFacadeService.ShowNotificationWarning("Время получения должно быть больше текущего времени");
                return;
            }

            if (SelectedInvoiceTemplate.State != null &&
                SupplierWarehouseId == SelectedInvoiceTemplate.SupplierWarehouseId &&
                WarehouseId == SelectedInvoiceTemplate.SupplierWarehouseId &&
                CarryType == SelectedInvoiceTemplate.CarryType &&
                DateGet == SelectedInvoiceTemplate.DateGet &&
                DateClose == SelectedInvoiceTemplate.DateClose)
            {
                MessageFacadeService.ShowNotificationWarning("Такая накладная уже существует");
                return;
            }

            InvoiceCreateDto createDto = new InvoiceCreateDto
            {
                SupplierId = SelectedSupplier.Id,
                SupplierWarehouseId = SupplierWarehouseId,
                WarehouseId = WarehouseId.Value,
                CarryId = CarryType.Id,
                DateClose = DateClose.Value,
                DateGet = DateGet.Value,
                DateArrive = DateArrive.Value,
                PaymentId = Payment.NoId,
                Main = IsMain()
            };

            try
            {
                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new CreateInvoice(createDto));

                MessageFacadeService.ShowNotificationInfo($"Накладная {result.Data.Id} успешно создана");
                Messenger.Send(new InvoiceMessage(result.Data, MessageType.Added));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании накладной");
                ShowValidationResultView("Ошибки при создании накладной", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create invoice");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании накладной");
                Logger.LogError(exception, "Error while creating invoice");
            }
        }

        private static DateTime? GetDateWithoutTimeForFutureOrPast(DateTime? fromDateTime)
        {
            return fromDateTime.HasValue && fromDateTime.Value.Date != DateTime.Today
                ? fromDateTime.Value.Date
                : fromDateTime;
        }

        private void Clear()
        {
            SelectedSupplier = null;
            DateFrom = null;
            InvoiceTemplates = null;

            ClearProperties();
        }

        private void ClearProperties()
        {
            SupplierWarehouseId = null;
            WarehouseId = null;
            CarryType = null;
            DateClose = null;
            DateGet = null;
            DateArrive = null;
        }

        private Task DateFromChangedCallbackAsync()
        {
            if (SelectedSupplier == null || DateFrom == null)
            {
                return Task.CompletedTask;
            }

            InvoiceTemplates = null;
            ClearProperties();

            return SelectSupplierInternalAsync(SelectedSupplier.Id, DateFrom);
        }

        private async Task RefreshSupplerWarehousesAsync(int supplerId)
        {
            IReadOnlyCollection<SupplierWarehouseDto> supplerWarehouses = await WebClient.ExecuteApiRequestAsync(new QueryContractorWarehouses(supplerId));
            SupplierWarehouses = supplerWarehouses.Select(x => new ComboBoxItem(x.Id, x.Name)).ToObservableCollection();
        }

        private async Task SelectedSupplierChangedCallbackAsync()
        {
            if (SelectedSupplier == null)
            {
                return;
            }

            DateFrom = null;
            InvoiceTemplates = null;
            ClearProperties();

            await RefreshSupplerWarehousesAsync(SelectedSupplier.Id);
            await SelectSupplierInternalAsync(SelectedSupplier.Id, DateFrom);
        }

        private async Task SelectSupplierInternalAsync(int supplierId, DateTime? fromDateTime)
        {
            try
            {
                DateTime? dateForClosestDelivery = GetDateWithoutTimeForFutureOrPast(fromDateTime);

                List<InvoiceTemplateDto> templates = await WebClient.ExecuteApiRequestAsync(new QueryInvoiceTemplates(supplierId, dateForClosestDelivery));

                IOrderedEnumerable<InvoiceTemplateViewItem> templateViewItems = templates
                    .Select(x => Mapper.Map(x, InvoiceTemplateViewItem.Create()))
                    .OrderBy(x => x.DateGet);

                InvoiceTemplates = new ObservableCollection<InvoiceTemplateViewItem>(templateViewItems);
            }
            catch (Exception exception)
            {
                SelectedSupplier = null;
                Logger.LogError(exception, "Failed to select supplier");
                MessageFacadeService.ShowNotificationError("Ошибка получения вариантов доставки");
            }
        }

        private bool IsMain()
        {
            return SelectedInvoiceTemplate?.Main == true
                   && SelectedInvoiceTemplate.DateGet == DateGet
                   && SelectedInvoiceTemplate.DateClose == DateClose
                   && SelectedInvoiceTemplate.DateArrive == DateArrive;
        }
    }
}