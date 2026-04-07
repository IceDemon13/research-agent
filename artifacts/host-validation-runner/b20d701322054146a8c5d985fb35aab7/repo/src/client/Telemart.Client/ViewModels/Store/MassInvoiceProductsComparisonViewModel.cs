using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using MediatR;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.ReportFactory;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.Requests.Features.Invoice.Actions;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class MassInvoiceProductsComparisonViewModel : ProductsComparisonViewModelBase
    {
        private List<int> SupplierAllowDocumentsInvoiceIds { get; set; }

        public MassInvoiceProductsComparisonViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessenger messenger,
            IMediator mediator,
            IErrorHandler errorHandler,
            IBarcodeReportFactory barcodeReportFactory,
            DocumentCommands documentsCommand,
            IMessageFacadeService messageFacadeService,
            IPrintingSettingsStore printingSettings)
            : base(webClient, dictionaries, messageFacadeService, barcodeReportFactory, mediator, messenger, documentsCommand, printingSettings)
        {
            ErrorHandler = errorHandler;
        }

        private IErrorHandler ErrorHandler { get; }

        protected override async Task HandleLoadedAsync()
        {
            MassInvoiceProductsComparisonParameter parameter = (MassInvoiceProductsComparisonParameter)Parameter;

            InvoiceProducts = parameter.InvoiceProducts;
            SupplierAllowDocumentsInvoiceIds = parameter.SupplierAllowDocumentsInvoiceIds;

            PagedResult<ProductAttributesDto> products = await WebClient.ExecuteApiRequestAsync(new QueryProductsAttributesByIds(parameter.InvoiceProducts.Select(x => x.ProductId).ToArray(), true));

            RecognizeBarcodeViewModel.Init(new RecognizeBarcodeSettings(true, true), products.Data);

            ProductComparisonViewItems = parameter.InvoiceProducts
                .Where(x => x.QuantityReal > 0)
                .GroupBy(x => x.ProductId)
                .Select(x => new { x.Key, FullName = x.First().GetLocalFullName(LocalizableNameType.Ukr), QuantityReal = x.Sum(z => z.QuantityReal!.Value), SerialNumbers = x.SelectMany(z => z.SerialNumbers), Product = RecognizeBarcodeViewModel.FindById(x.Key), MinQuantity = x.Sum(z => z.BillsQuantity) + x.Sum(z => z.QuantityReturned) })
                .Select(x => new ProductComparisonViewItem(x.Key, x.FullName, x.QuantityReal, x.Product.KeepSerial, x.Product.SelfBarcode, x.MinQuantity, x.SerialNumbers))
                .OrderBy(x => x.FullName)
                .ToObservableCollection();

            Title = "Массовая сверка товаров";

            ComparisonStopwatch = Stopwatch.StartNew();
        }

        protected override async Task HandleOkAsync()
        {
            if (!ProductComparisonViewItems.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Ничего не просканировано");
                return;
            }

            List<ProductComparisonResult> diviationItems = GetDeviation();

            if (diviationItems.Any())
            {
                ProductComparisonResultViewModel validationViewModel = DialogDocumentManagerService.ShowView<ProductComparisonResultViewModel>(new object[] { diviationItems, true }, this);

                if (!validationViewModel.IsOk)
                {
                    return;
                }
            }

            InvoiceMassComparisonSaveDto saveDto = new InvoiceMassComparisonSaveDto
            {
                Invoices = GetDistributedProductsOnInvoiceProducts().GroupBy(x => x.InvoiceId).Select(x => new InvoiceComparisonSaveDto
                {
                    InvoiceId = x.Key,
                    ComparisonTime = ComparisonStopwatch.Elapsed,
                    Products = x.Select(z => new InvoiceProductComparisonDto(z.ProductId, z.QuantityReal, z.SerialNumbers, z.Barcodes)).ToList()
                }).ToList()
            };

            (await ErrorHandler.HandleErrorsAsync(x => WebClient.ExecuteApiRequestAsync(new SaveInvoiceMassComparison(saveDto)), "сверке товаров", "Сверка сохранена", this, true))
                .IfNotNull(x =>
                {
                    IsOk = true;
                    Close();
                });
        }

        protected override List<ProductComparisonResult> GetDeviation()
        {
            Dictionary<int, ProductComparisonResult> items = new Dictionary<int, ProductComparisonResult>();

            foreach (ProductComparisonViewItem gridItem in ProductComparisonViewItems)
            {
                items[gridItem.ProductId] = new ProductComparisonResult(gridItem.FullName, 0, gridItem.QuantityReal);
            }

            foreach (IGrouping<int, InvoiceProductViewItem> invoiceProducts in InvoiceProducts.GroupBy(x => x.ProductId))
            {
                int gridItemQuantity = 0;

                if (items.TryGetValue(invoiceProducts.Key, out ProductComparisonResult item))
                {
                    gridItemQuantity = item.QuantityReal;
                }

                items[invoiceProducts.Key] = new ProductComparisonResult(
                    invoiceProducts.First().FullName,
                    invoiceProducts.Sum(x => x.Quantity),
                    gridItemQuantity);
            }

            return items.Values.Where(x => x.DeviationQuantity != 0).ToList();
        }

        private IEnumerable<InvoiceProductViewItem> GetDistributedProductsOnInvoiceProducts()
        {
            List<InvoiceProductViewItem> resultProducts = new List<InvoiceProductViewItem>();

            foreach (ProductComparisonViewItem product in ProductComparisonViewItems)
            {
                Queue<string> barcodes = new Queue<string>(RecognizeBarcodeViewModel.GetAssignedBarcodesById(product.ProductId));
                Queue<string> serialNumbers = new Queue<string>(product.Serials);

                int notDistributedQuantity = product.QuantityReal;

                List<InvoiceProductViewItem> currentProductInvoiceProducts = InvoiceProducts
                    .Where(x => x.ProductId == product.ProductId)
                    .ToList();

                if (currentProductInvoiceProducts.Any())
                {
                    bool invoiceProductQuantityAlreadyCleared = false;

                    // first distribute only expected amount of products
                    foreach (InvoiceProductViewItem invoiceProduct in currentProductInvoiceProducts.OrderByDescending(x => SupplierAllowDocumentsInvoiceIds.Contains(x.InvoiceId)))
                    {
                        ClearInvoiceProductQuantityBeforeDistributing(invoiceProduct, ref invoiceProductQuantityAlreadyCleared);

                        while (notDistributedQuantity > 0 && invoiceProduct.QuantityReal < invoiceProduct.Quantity)
                        {
                            DistributeItemToInvoiceProduct(invoiceProduct, serialNumbers, barcodes, ref notDistributedQuantity);
                        }
                    }

                    // if there are more products scanned than expected, we distribute to the first invoice with priority to a supplier that does not support documents
                    if (notDistributedQuantity > 0)
                    {
                        InvoiceProductViewItem invoiceProduct = currentProductInvoiceProducts.FirstOrDefault(x => !SupplierAllowDocumentsInvoiceIds.Contains(x.InvoiceId)) ?? currentProductInvoiceProducts.First();

                        if (invoiceProductQuantityAlreadyCleared == false)
                        {
                            ClearInvoiceProductQuantityBeforeDistributing(invoiceProduct, ref invoiceProductQuantityAlreadyCleared);
                        }

                        while (notDistributedQuantity > 0)
                        {
                            DistributeItemToInvoiceProduct(invoiceProduct, serialNumbers, barcodes, ref notDistributedQuantity);
                        }
                    }
                }
                else
                {
                    currentProductInvoiceProducts.Add(new InvoiceProductViewItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.FullName,
                        SerialNumbers = serialNumbers.ToObservableCollection(),
                        Barcodes = barcodes.ToObservableCollection(),
                        InvoiceId = InvoiceProducts.FirstOrDefault(x => !SupplierAllowDocumentsInvoiceIds.Contains(x.InvoiceId))?.InvoiceId ?? InvoiceProducts.First().InvoiceId,
                        QuantityReal = notDistributedQuantity
                    });
                }

                resultProducts.AddRange(currentProductInvoiceProducts);
            }

            return resultProducts;

            static void DistributeItemToInvoiceProduct(InvoiceProductViewItem invoiceProduct, Queue<string> serialNumbers, Queue<string> barcodes, ref int notDistributedQuantity)
            {
                invoiceProduct.QuantityReal++;

                if (serialNumbers.Any())
                {
                    invoiceProduct.SerialNumbers.Add(serialNumbers.Dequeue());
                }

                if (barcodes.Any())
                {
                    (invoiceProduct.Barcodes ??= new ObservableCollection<string>()).Add(barcodes.Dequeue());
                }

                notDistributedQuantity--;
            }

            static void ClearInvoiceProductQuantityBeforeDistributing(InvoiceProductViewItem invoiceProduct, ref bool invoiceProductQuantityAlreadyCleared)
            {
                invoiceProduct.SerialNumbers.Clear();
                invoiceProduct.QuantityReal = 0;
                invoiceProductQuantityAlreadyCleared = true;
            }
        }
    }
}