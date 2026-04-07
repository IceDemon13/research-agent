using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.XtraReports;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Order;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Requests.Features.OrderBill;
using Telemart.Client.Data.Requests.Features.PrintReport;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Reports.AdditionalServiceProduct;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Common.RecognizeWarehouseCell;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderGiveCellViewModel : TelemartDialogViewModelBase
    {
        private OrderGiveCellParameter parameter;
        private OrderDto _order;

        public OrderGiveCellViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IOrderReportBuilder orderReportBuilder,
            IMediator mediator,
            IPrintingSettingsStore printingSettingsStore,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            OrderReportBuilder = orderReportBuilder;
            Mediator = mediator;
            PrintingSettingsStore = printingSettingsStore;
            Mapper = mapper;

            RecognizeBarcodeViewModel = new RecognizeWarehouseCellBarcodeViewModel(webClient, dictionaries, messageFacadeService);

            RecognizeBarcodeViewModel.OnStarted += (_, _) => IsRecognitionInProgress = true;
            RecognizeBarcodeViewModel.OnFinishCommand += (_, _) => IsRecognitionInProgress = false;
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;

            IsAutoPrintWarrantyCard = true;

            PrintWarrantyCardCommand = new AsyncCommand(PrintWarrantyCardAsync);
            PrintAcceptanceProtocolCommand = new AsyncCommand(PrintAcceptanceProtocolAsync);
            PrintBillCommand = new AsyncCommand(PrintBillAsync);
            PrintBillInvoiceCommand = new AsyncCommand(PrintBillInvoiceAsync);
        }

        public IAsyncCommand PrintAcceptanceProtocolCommand { get; }

        public IAsyncCommand PrintWarrantyCardCommand { get; }

        public IAsyncCommand PrintBillCommand { get; }

        public IAsyncCommand PrintBillInvoiceCommand { get; }

        public override int Width => 800;

        public override int Height => 500;

        public override int MinWidth => 800;

        public override int MinHeight => 500;

        public ReadOnlyObservableCollection<OrderCellViewItem> OrderCells
        {
            get { return GetProperty(() => OrderCells); }
            private set { SetProperty(() => OrderCells, value); }
        }

        public RecognizeBarcodeViewModelBase<RecognizeWarehouseCellBarcodeResultEventArgs> RecognizeBarcodeViewModel { get; }

        public bool IsRecognitionInProgress
        {
            get { return GetProperty(() => IsRecognitionInProgress); }
            private set { SetProperty(() => IsRecognitionInProgress, value); }
        }

        public bool AllowBills
        {
            get { return GetProperty(() => AllowBills); }
            private set { SetProperty(() => AllowBills, value); }
        }

        public bool IsAutoPrintAcceptanceProtocol
        {
            get { return GetProperty(() => IsAutoPrintAcceptanceProtocol); }
            set { SetProperty(() => IsAutoPrintAcceptanceProtocol, value); }
        }

        public short AcceptanceProtocolQuantity
        {
            get { return GetProperty(() => AcceptanceProtocolQuantity); }
            set { SetProperty(() => AcceptanceProtocolQuantity, value); }
        }

        public bool IsAutoPrintWarrantyCard
        {
            get { return GetProperty(() => IsAutoPrintWarrantyCard); }
            set { SetProperty(() => IsAutoPrintWarrantyCard, value); }
        }

        public bool IsAutoPrintBillInvoice
        {
            get { return GetProperty(() => IsAutoPrintBillInvoice); }
            set { SetProperty(() => IsAutoPrintBillInvoice, value); }
        }

        public bool IsAutoPrintBill
        {
            get { return GetProperty(() => IsAutoPrintBill); }
            set { SetProperty(() => IsAutoPrintBill, value); }
        }

        public string ProgressText
        {
            get { return GetProperty(() => ProgressText); }
            private set { SetProperty(() => ProgressText, value); }
        }

        public bool PrintDocuments
        {
            get { return GetProperty(() => PrintDocuments); }
            private set { SetProperty(() => PrintDocuments, value); }
        }

        public bool IsAutoGuestProductPrint
        {
            get { return GetProperty(() => IsAutoGuestProductPrint); }
            set { SetProperty(() => IsAutoGuestProductPrint, value); }
        }

        public bool IsAutoGuestProductPrintVisible
        {
            get { return GetProperty(() => IsAutoGuestProductPrintVisible); }
            set { SetProperty(() => IsAutoGuestProductPrintVisible, value); }
        }

        public bool IsReadonlyAutoPrintBillInvoice
        {
            get { return GetProperty(() => IsReadonlyAutoPrintBillInvoice); }
            set { SetProperty(() => IsReadonlyAutoPrintBillInvoice, value); }
        }

        private IOrderReportBuilder OrderReportBuilder { get; }

        private IMediator Mediator { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IMapper Mapper { get; }

        protected override async Task HandleOkAsync()
        {
            if (AcceptanceProtocolQuantity < 0)
            {
                MessageFacadeService.ShowNotificationError("Кол-во копий не должно быть отрицательным");
                return;
            }

            if (OrderCells.Any(x => !x.Completed))
            {
                MessageFacadeService.ShowNotificationWarning("Все ячейки должны быть просканированы");
                return;
            }

            ProgressText = "Обработка данных";

            if (PrintDocuments)
            {
                PrintingSettingsInfo settings = null;

                if (IsAutoPrintAcceptanceProtocol || IsAutoPrintWarrantyCard)
                {
                    settings = await PrintingSettingsStore.LoadAsync();

                    bool isPrintingSettingsValid;

                    string progressText = ProgressText;

                    ProgressText = "Инициализация принтеров";

                    using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        isPrintingSettingsValid = await IsPrintingSettingsValidAsync(settings, cancellationTokenSource.Token);
                    }

                    ProgressText = progressText;

                    if (!isPrintingSettingsValid)
                    {
                        MessageFacadeService.ShowNotificationError("Задайте принтеры в настройках");
                        return;
                    }
                }

                await PrintDocumentsAsync(settings);
            }

            IsOk = true;
            Close();
        }

        protected override void OnInitializeInDesignMode()
        {
            PrintDocuments = true;
            AllowBills = true;

            base.OnInitializeInDesignMode();
        }

        protected override void OnParameterChanged(object parameter)
        {
            base.OnParameterChanged(parameter);

            this.parameter = (OrderGiveCellParameter)parameter;

            if (this.parameter?.Order?.WarehouseId != null)
            {
                ((ISupportParameter)RecognizeBarcodeViewModel).Parameter = new RecognizeWarehouseCellBarcodeParameter(this.parameter.Order.WarehouseId.Value, true);

                OrderCells = this.parameter.OrderCells.Select(x => Mapper.Map<OrderCellViewItem>(x)).ToReadOnlyObservableCollection();
            }

            Title = this.parameter.Title;
            PrintDocuments = this.parameter.PrintDocuments;
            _order = this.parameter.Order;

            LegalEntityDto legalEntity = this.parameter.Order.LegalEntity;

            if (legalEntity != null && !legalEntity.White)
            {
                IsAutoPrintAcceptanceProtocol = true;
            }

            if (this.parameter.BillId != null)
            {
                AllowBills = true;
                IsAutoPrintBill = true;
                IsAutoPrintBillInvoice = true;

                IsAutoPrintAcceptanceProtocol = false;
            }

            AcceptanceProtocolQuantity = 1;
            if (this.parameter.Order.PaymentId == Payment.PaylaterId)
            {
                IsAutoPrintAcceptanceProtocol = true;
                AcceptanceProtocolQuantity = 2;
            }

            IsReadonlyAutoPrintBillInvoice = _order?.CarryId == CarryType.PickupId && _order?.PaymentId is Payment.CashlessTaxId or Payment.CashlessNoTaxId;

            IsAutoGuestProductPrint = IsAutoGuestProductPrintVisible = this.parameter.PrintActOutcomeGuestProduct && _order?.CarryId != CarryType.PickupId;
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

        private void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeWarehouseCellBarcodeResultEventArgs e)
        {
            if (e.IsValid)
            {
                OrderCellViewItem orderCell = OrderCells.FirstOrDefault(x => x.CellId == e.Cell.Id);

                if (orderCell == null)
                {
                    e.ErrorText = "Ячейка не найдена в заказе";
                }
                else if (orderCell.Completed)
                {
                    e.ErrorText = "Ячейка уже просканирована";
                }
                else
                {
                    orderCell.Completed = true;
                }
            }

            IsRecognitionInProgress = false;
        }

        private async Task PrintDocumentsAsync(PrintingSettingsInfo settings)
        {
            if (IsAutoPrintAcceptanceProtocol && AcceptanceProtocolQuantity > 0)
            {
                IReport report = await OrderReportBuilder.BuildAcceptanceProtocolReportAsync(parameter.Order, parameter.ContractorName, parameter.CityName, true);

                if (settings.InvoiceFormat == PrintingSettingsInvoiceFormat.TapeId)
                {
                    await Mediator.Send(new PrintReportRequest(report, false, settings.Cheque.Name, settings.Cheque.PaperSource, copies: AcceptanceProtocolQuantity));
                }
                else
                {
                    await Mediator.Send(new PrintReportRequest(report, false, settings.Main.Name, settings.Main.PaperSource, copies: AcceptanceProtocolQuantity));
                }
            }

            if (IsAutoPrintWarrantyCard)
            {
                int[] productIds = parameter.Order.Products
                   .Where(x => x.Product.PrintWarrantyCard)
                   .Select(x => x.Product.Id)
                   .ToArray();

                if (productIds.Any())
                {
                    await Mediator.Send(new PrintWarrantyCardRequest(parameter.Order.Id, productIds, null, false));
                }
            }

            if (AllowBills && IsAutoPrintBill)
            {
                await PrintBillAsync();
            }

            if (AllowBills && IsAutoPrintBillInvoice)
            {
                await PrintBillInvoiceAsync();
            }

            if (IsAutoGuestProductPrint)
            {
                await PrintActOutcomeGuestProductAsync();
            }
        }

        private async Task PrintWarrantyCardAsync()
        {
            int[] productIds = parameter.Order.Products
                .Where(x => x.Product.PrintWarrantyCard)
                .Select(x => x.Product.Id)
                .ToArray();

            if (!productIds.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Выберите хотя бы один товар");
                return;
            }

            await Mediator.Send(new PrintWarrantyCardRequest(parameter.Order.Id, productIds, null, true));
        }

        private async Task PrintAcceptanceProtocolAsync()
        {
            IReport report = await OrderReportBuilder.BuildAcceptanceProtocolReportAsync(parameter.Order, parameter.ContractorName, parameter.CityName, true);

            PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

            if (printSettings.InvoiceFormat == PrintingSettingsInvoiceFormat.TapeId)
            {
                PrintReportRequest printRequest = printSettings.Cheque != null
                    ? new PrintReportRequest(report, true, printSettings.Cheque.Name, printSettings.Cheque.PaperSource, copies: AcceptanceProtocolQuantity)
                    : new PrintReportRequest(report, true, copies: AcceptanceProtocolQuantity);

                await Mediator.Send(printRequest);
            }
            else
            {
                PrintReportRequest printRequest = printSettings.Main != null
                    ? new PrintReportRequest(report, true, printSettings.Main.Name, printSettings.Main.PaperSource, copies: AcceptanceProtocolQuantity)
                    : new PrintReportRequest(report, true, copies: AcceptanceProtocolQuantity);

                await Mediator.Send(printRequest);
            }
        }

        private async Task PrintBillInvoiceAsync()
        {
            try
            {
                if (parameter.BillId.HasValue)
                {
                    byte[] document = await WebClient.ExecuteApiRequestAsBytesAsync(new ExportOrderBillInvoice(parameter.BillId.Value));
                    await FileHelper.OpenAsFileAsync(document, Constants.XlsFileExtension);
                }
            }
            catch (Exception ex)
            {
                MessageFacadeService.ShowNotificationWarning("Ошибка при печати расходной накладной");
                Logger.LogError(ex, "Failed to print bill invoice");
            }
        }

        private async Task PrintBillAsync()
        {
            try
            {
                if (parameter.BillId.HasValue)
                {
                    byte[] document = await WebClient.ExecuteApiRequestAsBytesAsync(new ExportOrderBill(parameter.BillId.Value));
                    await FileHelper.OpenAsFileAsync(document, Constants.XlsFileExtension);
                }
            }
            catch (Exception ex)
            {
                MessageFacadeService.ShowNotificationWarning("Ошибка при печати счета");
                Logger.LogError(ex, "Failed to print bill");
            }
        }

        private async Task PrintActOutcomeGuestProductAsync()
        {
            AdditionalServiceProductClientProductReportDataDto data = await WebClient.ExecuteApiRequestAsync(new QueryActAdditionalServiceProductReportData(_order.Id));

            AdditionalServiceProductClientProductsReportData reportData = new(DateTime.Now, data.FullNameClient, string.Empty, data.PlaceName, WebClient.AuthenticatedEmployee.Name);

            reportData.SetGuestProducs(data.GuestProducts?.Select(x => Mapper.Map<GuestProductReportData>(x)).ToArray());
            reportData.SetNumber(_order.Id);

            IReport report = new ActOutcomeClientProductReport { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }
    }
}