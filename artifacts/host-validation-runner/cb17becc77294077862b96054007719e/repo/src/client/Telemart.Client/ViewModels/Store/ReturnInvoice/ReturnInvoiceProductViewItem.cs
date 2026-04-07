using System;
using System.Collections.Generic;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public class ReturnInvoiceProductViewItem : TelemartEditorViewItemBase, ILocalіzableEntity
    {
        public ReturnInvoiceProductViewItem(decimal price)
        {
            OldPrice = price;
            Price = price;
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            private set { SetProperty(() => ProductId, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            private set { SetProperty(() => CategoryId, value); }
        }

        public string CategoryName
        {
            get { return GetProperty(() => CategoryName); }
            private set { SetProperty(() => CategoryName, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public Currency Currency
        {
            get { return GetProperty(() => Currency); }
            private set { SetProperty(() => Currency, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public int? BillsQuantity
        {
            get { return GetProperty(() => BillsQuantity); }
            set { SetProperty(() => BillsQuantity, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public int? AcceptQuantity
        {
            get { return GetProperty(() => AcceptQuantity); }
            set { SetProperty(() => AcceptQuantity, value, () => RaisePropertiesChanged(nameof(Quantity), nameof(OutQuantity), nameof(MaxQuantity))); }
        }

        public int OutQuantity
        {
            get { return GetProperty(() => OutQuantity); }
            set { SetProperty(() => OutQuantity, value); }
        }

        public string ProductPn
        {
            get { return GetProperty(() => ProductPn); }
            set { SetProperty(() => ProductPn, value); }
        }

        public List<string> SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            set { SetProperty(() => SerialNumbers, value); }
        }

        public int SerialsQuantity
        {
            get { return GetProperty(() => SerialsQuantity); }
            set { SetProperty(() => SerialsQuantity, value, () => RaisePropertiesChanged(nameof(Quantity), nameof(OutQuantity), nameof(MaxQuantity))); }
        }

        public string FullName
        {
            get { return GetProperty(() => FullName); }
            set { SetProperty(() => FullName, value); }
        }

        public string FullNameUkr
        {
            get { return GetProperty(() => FullNameUkr); }
            set { SetProperty(() => FullNameUkr, value); }
        }

        public CategoryType CategoryType
        {
            get { return GetProperty(() => CategoryType); }
            set { SetProperty(() => CategoryType, value); }
        }

        public bool KeepSerial
        {
            get { return GetProperty(() => KeepSerial); }
            set { SetProperty(() => KeepSerial, value); }
        }

        public int UsdCurrency
        {
            get { return GetProperty(() => UsdCurrency); }
            set { SetProperty(() => UsdCurrency, value); }
        }

        public int MaxQuantity => Math.Min(
            SerialsQuantity > 0
                ? Math.Min(SerialsQuantity, MaxQuantityDefaultFromInvoice)
                : MaxQuantityDefaultFromInvoice,
            StockQuantity);

        public string ProductFullName => this.GetLocalName(LocalizableNameType.Ukr);

        public int MaxQuantityDefaultFromInvoice
        {
            get { return GetProperty(() => MaxQuantityDefaultFromInvoice); }
            set { SetProperty(() => MaxQuantityDefaultFromInvoice, value, () => RaisePropertiesChanged(nameof(Quantity), nameof(OutQuantity), nameof(MaxQuantity))); }
        }

        public int StockQuantity
        {
            get { return GetProperty(() => StockQuantity); }
            set { SetProperty(() => StockQuantity, value, () => RaisePropertiesChanged(nameof(Quantity), nameof(OutQuantity), nameof(MaxQuantity))); }
        }

        public decimal OldPrice { get; }

        public static void BuildMetadata(MetadataBuilder<ReturnInvoiceProductViewItem> builder)
        {
            builder.Property(x => x.OutQuantity)
                .MatchesInstanceRule((x, y) => y.AcceptQuantity.HasValue || x <= y.MaxQuantity, () => "Превышено допустимое количество возвращаемых товаров");
            builder.Property(x => x.Quantity)
                .MatchesInstanceRule((x, y) => y.AcceptQuantity.HasValue || x <= y.MaxQuantity, () => "Превышено допустимое количество возвращаемых товаров");
            builder.Property(x => x.Price).MatchesInstanceRule((x, y) => x > 0, () => "Цена товара должна быть больше 0");
        }

        public string Name => FullName;

        public string NameUkr => FullNameUkr;

        public string NameEn => string.Empty;
    }
}