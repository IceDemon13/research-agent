using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.SupplierBill;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.ViewModels.SupplierBill.Create
{
    public sealed class CreateSupplierBillModel : BindableBase, IDataErrorInfo, IDisposable
    {
        public CreateSupplierBillModel(ReadOnlyObservableCollection<ContractorDto> suppliers)
        : this()
        {
            Contractors = suppliers;
        }

        public CreateSupplierBillModel()
        {
            DateMinValue = DateTime.Today.AddMonths(-1);
            DateMaxValue = DateTime.Today;

            NameColumnItem = new ComboBoxItem(1, "Название");
            CodeColumnItem = new ComboBoxItem(2, "Код");
            QuantityColumnItem = new ComboBoxItem(3, "Кол-во");
            PriceColumnItem = new ComboBoxItem(4, "Цена");
            TnvedColumnItem = new ComboBoxItem(5, "УКТВЭД");

            ColumnItems = new ObservableCollection<ComboBoxItem>
            {
                CodeColumnItem,
                NameColumnItem,
                QuantityColumnItem,
                PriceColumnItem,
                TnvedColumnItem
            };

            Products = new ObservableRangeCollection<CreateSupplierBillProductModel>();

            Products.CollectionChanged += ProductsOnCollectionChanged;

            OpenAfterCreation = true;

            CurrencyId = Currency.UahId;
        }

        public ReadOnlyObservableCollection<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<Currency> Currencies
        {
            get { return GetProperty(() => Currencies); }
            set { SetProperty(() => Currencies, value); }
        }

        public DateTime DateMinValue
        {
            get { return GetProperty(() => DateMinValue); }
            private set { SetProperty(() => DateMinValue, value); }
        }

        public DateTime DateMaxValue
        {
            get { return GetProperty(() => DateMaxValue); }
            private set { SetProperty(() => DateMaxValue, value); }
        }

        #region Contractor

        public int? ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public ContractorDto Contractor
        {
            get { return GetProperty(() => Contractor); }
            set { SetProperty(() => Contractor, value, () => { RaisePropertyChanged(nameof(Edrpou)); }); }
        }

        public DateTime? Date
        {
            get { return GetProperty(() => Date); }
            set { SetProperty(() => Date, value); }
        }

        public string Number
        {
            get { return GetProperty(() => Number); }
            set { SetProperty(() => Number, value); }
        }

        public int? InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value); }
        }

        public string Edrpou => Contractor?.Edrpou;

        #endregion

        #region Text

        public ComboBoxItem NameColumnItem { get; }

        public ComboBoxItem CodeColumnItem { get; }

        public ComboBoxItem QuantityColumnItem { get; }

        public ComboBoxItem PriceColumnItem { get; }

        public ComboBoxItem TnvedColumnItem { get; }

        public ObservableCollection<ComboBoxItem> ColumnItems
        {
            get { return GetProperty(() => ColumnItems); }
            private set { SetProperty(() => ColumnItems, value); }
        }

        public string Text
        {
            get { return GetProperty(() => Text); }
            set { SetProperty(() => Text, value); }
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

        #endregion

        #region Matching And Confirmation

        public ObservableRangeCollection<CreateSupplierBillProductModel> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        #endregion

        public IPriceConverter PriceConverter
        {
            get { return GetProperty(() => PriceConverter); }
            set { SetProperty(() => PriceConverter, value); }
        }

        public InvoiceDto InvoiceDto
        {
            get { return GetProperty(() => InvoiceDto); }
            set { SetProperty(() => InvoiceDto, value); }
        }

        public SupplierBillDto Result
        {
            get { return GetProperty(() => Result); }
            set { SetProperty(() => Result, value); }
        }

        public ObservableCollection<ValidationResultItem> ValidationItems
        {
            get { return GetProperty(() => ValidationItems); }
            set { SetProperty(() => ValidationItems, value); }
        }

        public bool OpenAfterCreation
        {
            get { return GetProperty(() => OpenAfterCreation); }
            set { SetProperty(() => OpenAfterCreation, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<CreateSupplierBillModel> builder)
        {
            builder.Property(x => x.Contractor).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Edrpou).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Date).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Number).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.InvoiceId).Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Text).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PriceWithTax).Required(() => Resources.RequiredErrorMessage);
        }

        public void SetProducts(IReadOnlyCollection<CreateSupplierBillProductModel> resultItems)
        {
            Products.Clear();
            Products.AddRange(resultItems);
        }

        public void SetSummaryItems()
        {
            int sku = Products.Select(x => x.ProductId.Value).Distinct().Count();
            int totalQuantity = Products.Select(x => x.Quantity).DefaultIfEmpty(0).Sum();
            decimal totalPriceWithTax = Products.Select(x => x.SumWithTax).DefaultIfEmpty(0).Sum();
            decimal totalPriceNoTax = Products.Select(x => x.SumNoTax).DefaultIfEmpty(0).Sum();
            decimal totalTax = totalPriceWithTax - totalPriceNoTax;

            SummaryItems = new[]
            {
                new SummaryViewItem("Поставщик", Contractor.Name),
                new SummaryViewItem("ЕДРПОУ", Contractor.Edrpou),
                new SummaryViewItem("Дата", Date.Value.ToString("dd.MM.yy")),
                new SummaryViewItem("Номер", Number),
                new SummaryViewItem("SKU", sku.ToString(CultureInfo.InvariantCulture)),
                new SummaryViewItem("Кол-во", totalQuantity.ToString(CultureInfo.InvariantCulture)),
                new SummaryViewItem("Без НДС", CurrencyFormatingRules.ToStr(totalPriceNoTax, CurrencyId.Value))
            };

            if (PriceWithTaxEnabled)
            {
                SummaryItems = SummaryItems.Concat(new[]
                {
                    new SummaryViewItem("НДС", CurrencyFormatingRules.ToStr(totalTax, CurrencyId.Value)),
                    new SummaryViewItem("С НДС", CurrencyFormatingRules.ToStr(totalPriceWithTax, CurrencyId.Value))
                });
            }
        }

        public void CurrencyIdChanged()
        {
            if (CurrencyId == Currency.UahId)
            {
                PriceWithTax = true;
                PriceWithTaxEnabled = true;

                if (Products is not null)
                {
                    foreach (CreateSupplierBillProductModel product in Products)
                    {
                        product.TaxRate = product.TaxRateDefault;
                    }
                }
            }
            else
            {
                PriceWithTax = false;
                PriceWithTaxEnabled = false;

                if (Products is not null)
                {
                    foreach (CreateSupplierBillProductModel product in Products)
                    {
                        product.TaxRate = TaxRate.NoPercent;
                    }
                }
            }

            RaisePropertiesChanged(nameof(PriceWithTaxEnabled), nameof(PriceWithTax));
        }

        private void ProductsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CurrencyIdChanged();
        }

        public void Dispose()
        {
            Products.CollectionChanged -= ProductsOnCollectionChanged;
        }
    }
}