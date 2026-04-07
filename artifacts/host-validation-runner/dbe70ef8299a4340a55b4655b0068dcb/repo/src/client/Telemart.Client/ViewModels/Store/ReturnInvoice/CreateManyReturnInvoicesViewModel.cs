using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Data.Requests.Features.Purchase;
using Telemart.Client.Data.Requests.Features.ReturnInvoice;
using Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ReturnInvoice;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public class CreateManyReturnInvoicesViewModel : TelemartDialogViewModelBase
    {
        private ManyReturnInvoiceParameter _parameter;

        public CreateManyReturnInvoicesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
            Messenger = messenger;

            ExportCommand = new DelegateCommand<TableView>(Export);
            HandleOutQuantityChangedCommand = new DelegateCommand<CellValueChangedEventArgs>(ChangeOutQuantity);
        }

        public IDelegateCommand ExportCommand { get; }

        public IDelegateCommand HandleOutQuantityChangedCommand { get; }

        public ReadOnlyObservableCollection<ManyReturnInvoiceProductViewItem> ManyInvoiceProducts
        {
            get { return GetProperty(() => ManyInvoiceProducts); }
            set { SetProperty(() => ManyInvoiceProducts, value); }
        }

        public ManyReturnInvoiceProductViewItem SelectedInvoiceProduct
        {
            get { return GetProperty(() => SelectedInvoiceProduct); }
            set { SetProperty(() => SelectedInvoiceProduct, value); }
        }

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        private IErrorHandler ErrorHandler { get; }

        private IMessenger Messenger { get; }

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        protected override async Task HandleLoadedAsync()
        {
            _parameter = (ManyReturnInvoiceParameter)Parameter;

            Title = "Создание возвратов";

            ManyInvoiceProducts = _parameter.Invoices?.SelectMany(invoice => invoice.InvoiceProducts
                .Where(x => x.QuantityReal - x.QuantityReturned > 0)
                .Select(product => new ManyReturnInvoiceProductViewItem()
                {
                    DateArrive = invoice.DateArrive,
                    InvoiceId = invoice.Id,
                    ProductId = product.ProductId,
                    ProductPn = product.ProductPn,
                    ProductFullName = product.ProductName.GetStringWithPrefix(product.ProductPrefix),
                    ProductFullNameUa = product.ProductNameUa.GetStringWithPrefix(product.ProductPrefixUa),
                    ProductFullNameEn = product.ProductNameEn.GetStringWithPrefix(product.ProductPrefixEn),
                    CurrencyId = product.CurrencyId,
                    Currency = Currency.GetById(product.CurrencyId),
                    Price = product.Price,
                    BillsQuantity = product.BillsQuantity,
                    Quantity = product.Quantity,
                    MaxQuantityDefaultFromInvoice = product.QuantityReal.Value - product.QuantityReturned
                })).ToReadOnlyObservableCollection();

            await Task.WhenAll(LoadWarehouseProductSourcesAsync(), LoadAvailInvoiceProductSerialDtoAsync());
        }

        protected override async Task HandleOkAsync()
        {
            int countProductReturn = ManyInvoiceProducts.Count(x => x.OutQuantity > 0);

            if (countProductReturn == 0)
            {
                MessageFacadeService.ShowNotificationInfo("Не выбраны товары для возврата");

                return;
            }

            ManyReturnInvoiceProductsCreateDto[] invoiceProductsDtos = ManyInvoiceProducts
                .Where(x => x.OutQuantity > 0)
                .GroupBy(x => x.InvoiceId)
                .Select(group => new ManyReturnInvoiceProductsCreateDto(group.Key, group.Select(MapProduct).ToArray()))
                .ToArray();

            ManyReturnInvoicesCreateDto returnInvoicesCreateDto = new ManyReturnInvoicesCreateDto()
            {
                WarehouseId = _parameter.WarehouseId,
                ReceiverCityId = _parameter.ReceiverCityId,
                CarryId = _parameter.CarryId,
                ReturnDate = _parameter.ReturnDate,
                TtnPayerTypeId = _parameter.TtnPayerTypeId,
                DeliveryData = _parameter.DeliveryData,
                InvoiceProducts = invoiceProductsDtos
            };

            Result<List<ReturnInvoiceDto>> resultRreturnInvoiceDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateManyReturnInvoices(returnInvoicesCreateDto)),
                "создании возвратов",
                "Возвраты созданы",
                this,
                true,
                confirmText: "Создать множество возвратов");

            if (resultRreturnInvoiceDtos.IsSuccess)
            {
                Messenger.Send(new ReturnInvoicesMessage(resultRreturnInvoiceDtos.Data.ToArray(), MessageType.Added));
                CloseOk();
            }
        }

        private QueryPurchaseSources.PurchaseSourcesRequest FilterProductSources(int[] products)
        {
            return new QueryPurchaseSources.PurchaseSourcesRequest()
            {
                WarehouseIds = new[] { _parameter.WarehouseId },
                IncludeInvoices = false,
                IncludeTransits = false,
                StockStrategy = StockStrategy.Database,
                ProductIds = products
            };
        }

        private async Task LoadWarehouseProductSourcesAsync()
        {
            int[] productIds = _parameter.Invoices.SelectMany(x => x.InvoiceProducts.Select(y => y.ProductId)).ToArray();

            ProductSourceDto[] warehouseProductSources = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryPurchaseSources(FilterProductSources(productIds))),
                "получении источников",
                null,
                this,
                true,
                showNotification: false);

            foreach (ManyReturnInvoiceProductViewItem invoiceProduct in ManyInvoiceProducts)
            {
                ProductSourceDto source = warehouseProductSources.FirstOrDefault(x => x.ProductId == invoiceProduct.ProductId);

                if (source != null)
                {
                    invoiceProduct.StockQuantity = source.Quantity
                                                   - source.ReservedByOrder
                                                   - source.ReservedByShowcase
                                                   - source.ReservedByAssemblyComplectation
                                                   - source.ReservedByReturnInvoice;

                    invoiceProduct.WarehouseQuantityFree = Math.Max(
                        source.Quantity
                        - source.ReservedByOrder
                        - source.ReservedByAssemblyComplectation
                        - source.ReservedByReturnInvoice
                        - source.LeftoversReserveQuantity ?? 0,
                        0);
                }
                else
                {
                    invoiceProduct.StockQuantity = invoiceProduct.WarehouseQuantityFree = 0;
                }
            }
        }

        private async Task LoadAvailInvoiceProductSerialDtoAsync()
        {
            int[] invoiceIds = _parameter.Invoices.Select(x => x.Id).ToArray();

            List<InvoiceProductSerialDto> availInvoiceProductSerialDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryInvoiceProductSerialsAllowToReturn(invoiceIds)),
                "получении серий",
                null,
                this,
                true,
                showNotification: false);

            foreach (ManyReturnInvoiceProductViewItem invoiceProduct in ManyInvoiceProducts)
            {
                invoiceProduct.SerialsQuantity = availInvoiceProductSerialDtos.Count(x => x.ProductId == invoiceProduct.ProductId && x.InvoiceId == invoiceProduct.InvoiceId);
            }
        }

        private void Export(TableView table)
        {
            if (table?.Grid == null)
            {
                return;
            }

            string fileName = $"Return_invoices_export_{DateTime.Now:yyyy-MM-dd}_{DateTime.Now:hh_mm_ss}";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            ((FileDialogServiceBase)SaveFileDialogService).InitialDirectory = folderPath;

            SaveFileDialogService.DefaultFileName = $"{fileName}";

            SaveFileDialogService.ShowDialog(
                _ =>
                {
                    List<ColumnBase> columnChooserColumns = new List<ColumnBase>(table.ColumnChooserColumns);

                    foreach (ColumnBase tableViewColumnChooserColumn in columnChooserColumns)
                    {
                        tableViewColumnChooserColumn.Visible = true;
                    }

                    string filePath = SaveFileDialogService.File.GetFullName();

                    table.ExportToXlsx(filePath, new XlsxExportOptionsEx(TextExportMode.Value));

                    MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");

                    foreach (ColumnBase tableViewColumnChooserColumn in columnChooserColumns)
                    {
                        tableViewColumnChooserColumn.Visible = false;
                    }
                },
                folderPath,
                fileName);

            IsLongOperationInProgress = false;
        }

        private ReturnInvoiceProductCreateDto MapProduct(ManyReturnInvoiceProductViewItem source)
        {
            return new ReturnInvoiceProductCreateDto
            {
                ProductId = source.ProductId,
                Quantity = source.OutQuantity,
                Price = source.Price
            };
        }

        private void ChangeOutQuantity(CellValueChangedEventArgs e)
        {
            if (SelectedInvoiceProduct != null)
            {
                ManyReturnInvoiceProductViewItem[] items = ManyInvoiceProducts
                    .Where(x => x.ProductId == SelectedInvoiceProduct.ProductId)
                    .ToArray();

                if (items.Length > 1)
                {
                    int sumOutQuantity = items.Sum(x => x.OutQuantity);

                    foreach (ManyReturnInvoiceProductViewItem invoiceProduct in items)
                    {
                        invoiceProduct.SumOutQuantity = sumOutQuantity;
                    }
                }
            }
        }
    }
}