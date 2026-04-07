using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core;
using Telemart.Client.Core.ErrorHandling;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Purchase;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Invoice;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Purchase
{
    public sealed class SetInvoiceSourceViewModel : TelemartDialogViewModelBase
    {
        private bool loaded;

        public SetInvoiceSourceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
            SelectSupplierCommand = new AsyncCommand<EditValueChangedEventArgs>(SelectSupplierAsync);
            CreateInvoiceCommand = new AsyncCommand<SetInvoiceSourceViewItem>(CreateInvoiceAsync, x => x != null && !x.InvoiceExists);

            Messenger.Register<InvoiceMessage>(this, OnInvoiceMessage);
        }

        public SetInvoiceSourceViewModel()
        {
        }

        #region Commands

        public IAsyncCommand CreateInvoiceCommand { get; }

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        public IAsyncCommand SelectSupplierCommand { get; }

        #endregion

        #region INPC Properties

        public SetSourceParameter Data
        {
            get { return GetProperty(() => Data); }
            private set { SetProperty(() => Data, value); }
        }

        public ReadOnlyObservableCollection<InvoiceState> InvoiceStates
        {
            get { return GetProperty(() => InvoiceStates); }
            private set { SetProperty(() => InvoiceStates, value); }
        }

        public ContractorDto SelectedSupplier
        {
            get { return GetProperty(() => SelectedSupplier); }
            set { SetProperty(() => SelectedSupplier, value); }
        }

        public SetInvoiceSourceViewItem SelectedInvoiceSource
        {
            get { return GetProperty(() => SelectedInvoiceSource); }
            set { SetProperty(() => SelectedInvoiceSource, value); }
        }

        public ObservableCollection<SetInvoiceSourceViewItem> InvoiceSources
        {
            get { return GetProperty(() => InvoiceSources); }
            private set { SetProperty(() => InvoiceSources, value); }
        }

        public ObservableCollection<SetInvoiceSourceViewItem> FixedInvoiceSources
        {
            get { return GetProperty(() => FixedInvoiceSources); }
            private set { SetProperty(() => FixedInvoiceSources, value); }
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

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public bool OkHandled
        {
            get { return GetProperty(() => OkHandled); }
            private set { SetProperty(() => OkHandled, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 450;

        public override int MinHeight => 200;

        public override int MinWidth => 600;

        public override int Width => 800;

        public override int MaxWidth => 1024;

        public override int MaxHeight => 768;

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            Data = new SetSourceParameter
            {
                Quantity = 1000,
                Price = 100,
                CurrencyId = Currency.Uah.Id,
                OrderWarehouseName = "ул.Довженко Магазин (Телемарт)",
                OrderDeliveryTime = DateTime.Now,
                OrderProductState = OrderProductStatus.New
            };

            SummaryItems = GetItems();
        }

        protected override bool CanOk()
        {
            return SelectedInvoiceSource != null;
        }

        protected override async Task HandleLoadedAsync()
        {
            Data = (SetSourceParameter)Parameter;

            SummaryItems = GetItems();

            InvoiceStates = Dictionaries.GetItems<InvoiceState>().ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshContractorsAsync(), RefreshWarehouses());

            if (Data.ProductInvoiceId.HasValue && Data.ProductInvoiceId.Value > 0)
            {
                InvoiceDto invoice = await WebClient.ExecuteApiRequestAsync(new QueryInvoice(Data.ProductInvoiceId.Value));

                SelectedSupplier = Suppliers.FirstOrDefault(x => x.Id == invoice.SupplierId);

                if (SelectedSupplier != null)
                {
                    await SetInvoiceSourcesAsync(SelectedSupplier.Id);

                    SelectedInvoiceSource = InvoiceSources.FirstOrDefault(x => x.InvoiceId.HasValue && x.InvoiceId.Value == Data.ProductInvoiceId.Value);
                }
            }

            Title = "Выбор накладной";

            loaded = true;

            async Task RefreshContractorsAsync()
            {
                List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
                Suppliers = contractors.Where(x => x.Active && x.IsSupplier && !x.IsFolder).OrderBy(x => x.Name).ToReadOnlyObservableCollection();
            }

            async Task RefreshWarehouses()
            {
                List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
                Warehouses = warehouses.ToReadOnlyObservableCollection();
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (SelectedInvoiceSource.InvoiceExists)
            {
                try
                {
                    UpdatePurchaseSource gatewayRequest = new UpdatePurchaseSource(Data.ProductRecordId, GetPurchaseSaveSource());

                    PurchaseDto purchase = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                    Messenger.Send(new PurchaseMessage(purchase, MessageType.Changed));
                    MessageFacadeService.ShowNotificationInfo("Источник успешно установлен");

                    IsOk = true;
                    Close();
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
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Запрещено выбирать не созданную накладную");
            }

            OkHandled = true;
        }

        private IEnumerable<SummaryViewItem> GetItems()
        {
            yield return new SummaryViewItem("Кол-во", Data.Quantity.ToString("D"));
            yield return new SummaryViewItem("Цена", CurrencyFormatingRules.ToStr(Data.Price, Data.CurrencyId));
            yield return new SummaryViewItem("Склад", Data.OrderWarehouseName);
            yield return new SummaryViewItem("Дата Х", Data.OrderDeliveryTime?.ToString(DateFormattingRules.FullDateTimeFormat));
            yield return new SummaryViewItem("Статус", Data.OrderProductState.Name);
        }

        private async Task CreateInvoiceAsync(SetInvoiceSourceViewItem invoiceSource)
        {
            if (invoiceSource.IsNewRow)
            {
                CreateNewInvoiceInternal();
            }
            else
            {
                await CreateInvoiceInternalAsync(invoiceSource);
            }
        }

        private async Task CreateInvoiceInternalAsync(SetInvoiceSourceViewItem invoiceSource)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                InvoiceCreateDto invoiceCreateDto = new InvoiceCreateDto
                {
                    SupplierWarehouseId = invoiceSource.SupplierWarehouseId,
                    PaymentId = Payment.NoId,
                    WarehouseId = invoiceSource.WarehouseId,
                    CarryId = invoiceSource.CarryType.Id,
                    DateGet = invoiceSource.DateGet.Value,
                    DateClose = invoiceSource.DateClose.Value,
                    SupplierId = invoiceSource.SupplierId,
                    DateArrive = invoiceSource.DateArrive.Value
                };

                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new CreateInvoice(invoiceCreateDto));

                invoiceSource.InvoiceId = result.Data.Id;
                invoiceSource.State = InvoiceStates.FirstOrDefault(x => x.Id == result.Data.StateId);

                MessageFacadeService.ShowNotificationInfo("Накладная успешно создана");
                Messenger.Send(new InvoiceMessage(result.Data, MessageType.Added));
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

        private void CreateNewInvoiceInternal()
        {
            if (SelectedSupplier != null)
            {
                DialogDocumentManagerService.ShowView<CreateInvoiceViewModel>(new CreateInvoiceParameter(SelectedSupplier.Id), this);
            }
        }

        private void OnInvoiceMessage(InvoiceMessage message)
        {
            if (message.MessageType == MessageType.Added && message.Entity.SupplierId == SelectedSupplier?.Id)
            {
                AsyncHelper.RunSync(() => SetInvoiceSourcesAsync(SelectedSupplier.Id));
                SelectedInvoiceSource = InvoiceSources.FirstOrDefault(x => x.InvoiceId == message.Entity.Id);
            }
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (e.OriginalSource is TableView || e.OriginalSource is RowControl)
                {
                    OkCommand.Execute(null);
                }
            }
            else if (e.Key == Key.Escape)
            {
                CancelCommand.Execute(null);
            }
            else if (e.Key == Key.Insert)
            {
                CreateInvoiceCommand.Execute(SelectedInvoiceSource);
            }
        }

        private async Task SelectSupplierAsync(EditValueChangedEventArgs arg)
        {
            if (loaded)
            {
                InvoiceSources = null;

                if (arg.NewValue is ContractorDto supplier)
                {
                    try
                    {
                        await SetInvoiceSourcesAsync(supplier.Id);
                    }
                    catch (Exception exception)
                    {
                        Logger.LogError(exception, "Failed to select supplier");
                        MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                    }
                }
            }
        }

        private async Task SetInvoiceSourcesAsync(int supplierId)
        {
            QueryPurchaseInvoiceSources gatewayRequest = new QueryPurchaseInvoiceSources(supplierId, Data.ProductRecordId);

            List<PurchaseInvoiceSourceDto> invoiceSources = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

            InvoiceSources = invoiceSources
                .Select(x => Mapper.Map(x, SetInvoiceSourceViewItem.Create(Data.OrderDeliveryTime, Data.Quantity, Data.OrderState)))
                .OrderBy(x => x.DeliveryDateTime)
                .ThenBy(x => x.InvoiceExists)
                .ToObservableCollection();

            SetInvoiceSourceViewItem createInvoiceViewItem = SetInvoiceSourceViewItem.Create();

            InvoiceSources.Add(createInvoiceViewItem);

            FixedInvoiceSources = new ObservableCollection<SetInvoiceSourceViewItem> { createInvoiceViewItem };
        }

        private PurchaseSourceSaveDto GetPurchaseSaveSource()
        {
            string warehouseName = Warehouses.FirstOrDefault(x => x.Id == SelectedInvoiceSource.WarehouseId)?.Name ?? string.Empty;

            return PurchaseSourceSaveDto.Purchase(
                SelectedInvoiceSource.WarehouseId,
                warehouseName,
                SelectedInvoiceSource.InvoiceId.Value,
                SelectedInvoiceSource.CarryType.Name,
                SelectedSupplier.Name,
                SelectedInvoiceSource.SupplierWarehouseName,
                SelectedInvoiceSource.DateClose.Value,
                SelectedInvoiceSource.DateGet.Value);
        }
    }
}