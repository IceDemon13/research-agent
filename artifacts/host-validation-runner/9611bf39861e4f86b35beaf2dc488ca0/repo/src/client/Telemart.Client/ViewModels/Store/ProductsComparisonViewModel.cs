using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.ReportFactory;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Invoice.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store
{
    internal sealed class ProductsComparisonViewModel : ProductsComparisonViewModelBase
    {
        private int invoiceId;
        private int supplierId;

        public ProductsComparisonViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessenger messenger,
            IMediator mediator,
            IBarcodeReportFactory barcodeReportFactory,
            DocumentCommands documentsCommand,
            IMessageFacadeService messageFacadeService,
            IPrintingSettingsStore printingSettings)
            : base(webClient, dictionaries, messageFacadeService, barcodeReportFactory, mediator, messenger, documentsCommand, printingSettings)
        {
        }

        public ProductsComparisonViewModel()
        {
        }

        public InvoiceDto InvoiceFromServer { get; private set; }

        public bool VisibleWarningText
        {
            get { return GetProperty(() => VisibleWarningText); }
            set { SetProperty(() => VisibleWarningText, value); }
        }

        public string WarningText => "Импортный товар! Проверьте наличие импортного стикера на товаре. Если его нет, нужно промаркировать";

        protected override async Task HandleLoadedAsync()
        {
            if (!InvoiceProducts.Any())
            {
                MessageFacadeService.ShowNotificationError("В накладной нет товаров для сверки");
                Close();
                return;
            }

            IReadOnlyCollection<ProductAttributesDto> products = await WebClient.ExecuteApiRequestAsync(new QueryInvoiceProductAttributes(invoiceId));

            ContractorDto contractor = await WebClient.ExecuteApiRequestAsync(new QueryContractor(supplierId));

            VisibleWarningText = contractor.CountryId.HasValue && contractor.CountryId != Constants.UkraineCountryId;

            RecognizeBarcodeViewModel.Init(new RecognizeBarcodeSettings(true, true), products);

            ProductComparisonViewItems = InvoiceProducts
                .Where(x => x.QuantityReal > 0)
                .Select(x => new { x.ProductId, FullName = x.GetLocalFullName(LocalizableNameType.Ukr), QuantityReal = x.QuantityReal.Value, x.SerialNumbers, Product = RecognizeBarcodeViewModel.FindById(x.ProductId), MinQuantity = x.BillsQuantity + x.QuantityReturned })
                .Select(x => new ProductComparisonViewItem(x.ProductId, x.FullName, x.QuantityReal, x.Product.KeepSerial, x.Product.SelfBarcode, x.MinQuantity, x.SerialNumbers) { SupplierId = supplierId })
                .OrderBy(x => x.FullName)
                .ToObservableCollection();

            SelectedProduct = ProductComparisonViewItems.FirstOrDefault();

            Title = "Сверка товаров";

            ComparisonStopwatch = Stopwatch.StartNew();
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                List<ProductComparisonResult> items = GetDeviation();

                if (items.Any())
                {
                    ProductComparisonResultViewModel validationViewModel = DialogDocumentManagerService.ShowView<ProductComparisonResultViewModel>(new object[] { items, true }, this);

                    if (!validationViewModel.IsOk)
                    {
                        return;
                    }
                }

                List<InvoiceProductComparisonDto> invoiceProductComparisonDtos = ProductComparisonViewItems
                    .Where(x => x.QuantityReal > 0)
                    .Select(x => new InvoiceProductComparisonDto(
                        x.ProductId,
                        x.QuantityReal,
                        x.Serials,
                        RecognizeBarcodeViewModel.GetAssignedBarcodesById(x.ProductId)))
                    .ToList();

                InvoiceComparisonSaveDto saveDto = new InvoiceComparisonSaveDto
                {
                    InvoiceId = invoiceId,
                    ComparisonTime = ComparisonStopwatch.Elapsed,
                    Products = invoiceProductComparisonDtos
                };

                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new SaveInvoiceComparison(saveDto));

                InvoiceFromServer = result.Data;

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении результатов сверки накладной");
                ShowValidationResultView("Ошибки при сохранении результатов сверки накладной", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save invoice compare result");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving invoice compare result");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении результатов сверки накладной");
            }
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            InvoiceViewItem invoice = (InvoiceViewItem)parameter;

            invoiceId = invoice.Id;
            supplierId = invoice.SupplierId;
            InvoiceProducts = invoice.InvoiceProducts.ToObservableCollection();
        }
    }
}