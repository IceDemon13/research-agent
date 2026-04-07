using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Contractor.Actions;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Store.Invoice.Parsing;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class InvoiceProductBulkAddViewModel : TelemartDialogViewModelBase
    {
        private InvoiceProductBulkAddParameter parameter;

        public InvoiceProductBulkAddViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            HandleTextCommand = new AsyncCommand(HandleTextAsync);
            RemoveColumnItemCommand = new DelegateCommand<ComboBoxItem>(RemoveColumnItem);
            AddColumnItemCommand = new DelegateCommand(AddColumnItem);

            ProductIdColumn = new ComboBoxItem(1, "Код товара");
            ProductNameColumn = new ComboBoxItem(2, "Название товара");
            SupplierProductIdColumn = new ComboBoxItem(3, "Код товара поставщика");
            SupplierProductNameColumn = new ComboBoxItem(4, "Название товара поставщика");
            PnColumn = new ComboBoxItem(5, "Артикул");
            QuantityColumn = new ComboBoxItem(6, "Кол-во");
            PriceColumn = new ComboBoxItem(7, "Цена");

            AllColumns = new ObservableCollection<ComboBoxItem>
            {
                ProductIdColumn,
                ProductNameColumn,
                SupplierProductIdColumn,
                SupplierProductNameColumn,
                PnColumn,
                QuantityColumn,
                PriceColumn
            };

            Columns = new ObservableCollection<ComboBoxItem>();

            Columns.CollectionChanged += ColumnsChanged;

            Items = new ObservableCollection<InvoiceProductBulkAddViewItem>();
        }

        public InvoiceProductBulkAddViewModel()
        {
        }

        public IAsyncCommand HandleTextCommand { get; }

        public IDelegateCommand RemoveColumnItemCommand { get; }

        public IDelegateCommand AddColumnItemCommand { get; }

        public string RawText
        {
            get { return GetProperty(() => RawText); }
            set { SetProperty(() => RawText, value); }
        }

        public bool? PriceWithTax
        {
            get { return GetProperty(() => PriceWithTax); }
            set { SetProperty(() => PriceWithTax, value); }
        }

        public bool PriceWithTaxEnabled
        {
            get { return GetProperty(() => PriceWithTaxEnabled); }
            set { SetProperty(() => PriceWithTaxEnabled, value); }
        }

        public int? CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value, CurrencyIdChanged); }
        }

        public ObservableCollection<InvoiceProductBulkAddViewItem> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        public ObservableCollection<InvoiceProductBulkAddViewItem> ValidItemsToAdd
        {
            get { return GetProperty(() => ValidItemsToAdd); }
            private set { SetProperty(() => ValidItemsToAdd, value); }
        }

        public ObservableCollection<ComboBoxItem> Columns
        {
            get { return GetProperty(() => Columns); }
            private init { SetProperty(() => Columns, value); }
        }

        public ObservableCollection<ComboBoxItem> AllColumns
        {
            get { return GetProperty(() => AllColumns); }
            private init { SetProperty(() => AllColumns, value); }
        }

        public ReadOnlyObservableCollection<Currency> Currencies
        {
            get { return GetProperty(() => Currencies); }
            private set { SetProperty(() => Currencies, value); }
        }

        #region DialogSettings

        public override int Height => 780;

        public override int MinHeight => 630;

        public override int MinWidth => 640;

        public override int Width => 800;

        #endregion

        private ComboBoxItem ProductIdColumn { get; }

        private ComboBoxItem ProductNameColumn { get; }

        private ComboBoxItem SupplierProductIdColumn { get; }

        private ComboBoxItem SupplierProductNameColumn { get; }

        private ComboBoxItem PnColumn { get; }

        private ComboBoxItem QuantityColumn { get; }

        private ComboBoxItem PriceColumn { get; }

        public static void BuildMetadata(MetadataBuilder<InvoiceProductBulkAddViewModel> builder)
        {
            builder.Property(x => x.CurrencyId).Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.PriceWithTax)
                .MatchesInstanceRule(
                    (x, y) => x.HasValue || y.CurrencyId != Currency.UahId || !y.Columns.Contains(y.PriceColumn),
                    () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleOkAsync()
        {
            if (Items?.Any() != true)
            {
                MessageFacadeService.ShowNotificationWarning("Нечего добавлять");
                return;
            }

            if (Items.Any(x => !x.IsValid))
            {
                if (!MessageFacadeService.Confirm("В списке есть нераспознанные товары. Добавить уже распознанные?"))
                {
                    return;
                }
            }

            ValidItemsToAdd = Items
                .Where(x => x.IsValid)
                .GroupBy(x => x.Product.Id)
                .Select(x => new InvoiceProductBulkAddViewItem
                {
                    Product = x.First().Product,
                    Quantity = x.Sum(w => w.Quantity),
                    ProductId = x.First().ProductId,
                    SupplierProductId = x.First().SupplierProductId,
                    ProductName = x.First().ProductName,
                    Pn = x.First().Pn,
                    SupplierProductName = x.First().SupplierProductName,
                    Price = x.First().GetPriceWithTaxCalculation()
                })
                .ToObservableRangeCollection();

            List<string> columnNames = Columns.Select(x => x.DisplayValue).ToList();

            await WebClient.ExecuteApiRequestAsync(new UpdateContractorInvoiceBulkAddProductColumns(parameter.ContractorId, new UpdateContractorInvoiceBulkAddProductColumnsDto(columnNames)));

            IsOk = true;
            Close();
        }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (InvoiceProductBulkAddParameter)Parameter;

            ContractorDto contractor = await WebClient.ExecuteApiRequestAsync(new QueryContractor(parameter.ContractorId));

            foreach (string columnName in contractor.InvoiceBulkAddProductColumns)
            {
                ComboBoxItem column = AllColumns.FirstOrDefault(x => x.DisplayValue == columnName);

                if (column != default)
                {
                    Columns.Add(column);
                }
            }

            Currencies = Dictionaries.GetItems<Currency>().ToReadOnlyObservableCollection();

            Title = "Добавить товары в накладную из списка";
        }

        private async Task HandleTextAsync()
        {
            if (string.IsNullOrWhiteSpace(RawText))
            {
                MessageFacadeService.ShowNotificationWarning("Поле не заполнено");
                return;
            }

            try
            {
                InvoiceProductTextParserSettings settings = new(
                    Columns.IndexOf(ProductIdColumn),
                    Columns.IndexOf(ProductNameColumn),
                    Columns.IndexOf(SupplierProductIdColumn),
                    Columns.IndexOf(SupplierProductNameColumn),
                    Columns.IndexOf(QuantityColumn),
                    Columns.IndexOf(PriceColumn),
                    Columns.IndexOf(PnColumn));

                InvoiceProductTextParserResult parserResult = InvoiceProductTextParser.Parse(RawText, settings);

                if (!parserResult.IsOk)
                {
                    ShowValidationResultView("Ошибки", parserResult.Errors);
                    return;
                }

                foreach (InvoiceProductBulkAddViewItem item in parserResult.Items)
                {
                    item.Pn = item.Pn?.Trim();
                    item.SupplierProductName = item.SupplierProductName?.Trim();
                    item.ProductName = item.ProductName?.Trim();
                }

                QueryRecognizeSupplierProductsDto requestDto =
                    new(parserResult.Items.Select(x => new RecognizeSupplierProductDto(x.ProductId, x.ProductName, x.SupplierProductId, x.SupplierProductName, x.Pn)).ToList(), parameter.ContractorId);

                IReadOnlyCollection<SupplierProductDto> recognizedSupplierProducts = await WebClient.ExecuteApiRequestAsync(new QueryRecognizeSupplierProducts(requestDto));

                if (!recognizedSupplierProducts.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Ни один товар не распознан");
                    return;
                }

                List<ValidationResultItem> validationItems = new();

                validationItems.AddRange(recognizedSupplierProducts
                    .Where(x => x.ProductId is null)
                    .Select(x => new ValidationResultItem($"Товар '{x.SupplierProductName}' найден, но не сопоставлен", true)));

                validationItems.AddRange(recognizedSupplierProducts
                    .Where(x => x.FoundedOnlyInNomenclature)
                    .Select(x => new ValidationResultItem($"Товар '{x.ProductName}' был найден только в нашей номенклатуре", false)));

                if (validationItems.Any())
                {
                    MessageFacadeService.ShowValidationResultView("Ошибки", validationItems, this);
                }

                recognizedSupplierProducts.ForEach(x => x.SupplierId = parameter.ContractorId);

                List<ProductDto> products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(
                    parameter.ContractorId,
                    recognizedSupplierProducts
                        .Where(x => x.ProductId.HasValue)
                        .Select(x => x.ProductId.Value)
                        .ToArray()));

                (SupplierProductDto supplierProduct, ProductDto product)[] joinedProductDatas = recognizedSupplierProducts.Select(x => (x, products.FirstOrDefault(z => z.Id == x.ProductId))).ToArray();

                foreach (ProductDto product in products)
                {
                    product.Price = 0;
                    product.CurrencyId = Currency.GetByName(product.Currency).Id;
                }

                Items.Clear();

                foreach (InvoiceProductBulkAddViewItem item in parserResult.Items)
                {
                    (SupplierProductDto supplierProduct, ProductDto product) joinedProductData = joinedProductDatas.FirstOrDefault(x =>
                        (item.ProductId.HasValue && x.supplierProduct.ProductId == item.ProductId)
                        || (item.ProductName != null && (item.ProductName == x.product.Name || item.ProductName == x.product.NameFullRu || item.ProductName == x.product.NameFullUa || item.ProductName == x.product.NameFullEn))
                        || (item.SupplierProductId != null && x.supplierProduct.SupplierProductId == item.SupplierProductId)
                        || (item.Pn != null && x.supplierProduct.Pn == item.Pn)
                        || (item.SupplierProductName != null && x.supplierProduct.SupplierProductName == item.SupplierProductName));

                    InvoiceProductBulkAddViewItem newItem;

                    if (joinedProductData.product is null)
                    {
                        newItem = new()
                        {
                            Quantity = item.Quantity,
                            Product = null,
                            Pn = item.Pn,
                            PriceWithTax = PriceWithTax,
                            Price = item.Price,
                            ProductId = item.ProductId,
                            SupplierProductId = item.SupplierProductId,
                            ProductName = item.ProductName,
                            SupplierProductName = item.SupplierProductName
                        };
                    }
                    else
                    {
                        newItem = new()
                        {
                            Quantity = item.Quantity,
                            Product = joinedProductData.product,
                            Pn = joinedProductData.product.Pn,
                            PriceWithTax = PriceWithTax,
                            Price = item.Price,
                            ProductId = joinedProductData.product.Id,
                            SupplierProductId = joinedProductData.supplierProduct.SupplierProductId,
                            ProductName = joinedProductData.product.Name,
                            SupplierProductName = joinedProductData.supplierProduct.SupplierProductName,
                            TaxRate = Dictionaries.GetItemById<TaxRate>(joinedProductData.product.TaxRateId)
                        };
                    }

                    Items.Add(newItem);
                }
            }
            catch (UnexpectedSatusException exception)
            {
                Logger.LogError(exception, "Failed to search by products");
                MessageFacadeService.ShowNotificationError("Ошибка при поиске товаров");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to handle products text");
                MessageFacadeService.ShowNotificationError("Ошибка при обработке текста");
            }
        }

        private void AddColumnItem()
        {
            if (AllColumns.Count == Columns.Count)
            {
                MessageFacadeService.ShowNotificationWarning("Нечего добавлять");
                return;
            }

            SelectItemViewModel viewModel = DialogDocumentManagerService.ShowView<SelectItemViewModel>(new SelectItemParameter(AllColumns.Where(x => !Columns.Contains(x)).ToArray(), "Выбор колонки", "Колонка"), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Columns.Add(viewModel.SelectedItem.Value);
        }

        private void RemoveColumnItem(ComboBoxItem item)
        {
            if (item == ProductIdColumn && !Columns.Contains(SupplierProductIdColumn) && !Columns.Contains(SupplierProductNameColumn) && !Columns.Contains(PnColumn) && !Columns.Contains(ProductNameColumn))
            {
                MessageFacadeService.ShowNotificationError($"Нельзя удалять \"{item.DisplayValue}\"");
                return;
            }

            if (item == SupplierProductIdColumn && !Columns.Contains(ProductIdColumn) && !Columns.Contains(SupplierProductNameColumn) && !Columns.Contains(PnColumn) && !Columns.Contains(ProductNameColumn))
            {
                MessageFacadeService.ShowNotificationError($"Нельзя удалять \"{item.DisplayValue}\"");
                return;
            }

            if (item == SupplierProductNameColumn && !Columns.Contains(ProductIdColumn) && !Columns.Contains(SupplierProductIdColumn) && !Columns.Contains(PnColumn) && !Columns.Contains(ProductNameColumn))
            {
                MessageFacadeService.ShowNotificationError($"Нельзя удалять \"{item.DisplayValue}\"");
                return;
            }

            if (item == PnColumn && !Columns.Contains(ProductIdColumn) && !Columns.Contains(SupplierProductIdColumn) && !Columns.Contains(SupplierProductNameColumn) && !Columns.Contains(ProductNameColumn))
            {
                MessageFacadeService.ShowNotificationError($"Нельзя удалять \"{item.DisplayValue}\"");
                return;
            }

            if (item == ProductNameColumn && !Columns.Contains(ProductIdColumn) && !Columns.Contains(SupplierProductIdColumn) && !Columns.Contains(SupplierProductNameColumn) && !Columns.Contains(PnColumn))
            {
                MessageFacadeService.ShowNotificationError($"Нельзя удалять \"{item.DisplayValue}\"");
                return;
            }

            Columns.Remove(item);
        }

        private void ColumnsChanged(object sender, object e)
        {
            if (Columns != null && Columns.Any(x => x.Id == PriceColumn.Id) && CurrencyId == Currency.UahId)
            {
                PriceWithTaxEnabled = true;
            }
            else
            {
                PriceWithTaxEnabled = false;
                PriceWithTax = null;
            }

            RaisePropertiesChanged(nameof(PriceWithTaxEnabled), nameof(Columns), nameof(PriceWithTax));
        }

        private void CurrencyIdChanged()
        {
            if ((CurrencyId.HasValue && CurrencyId != Currency.UahId) || Columns is null || !Columns.Contains(PriceColumn))
            {
                PriceWithTax = null;
                PriceWithTaxEnabled = false;
            }
            else
            {
                PriceWithTaxEnabled = true;
            }

            RaisePropertiesChanged(nameof(PriceWithTaxEnabled), nameof(PriceWithTax));
        }
    }
}