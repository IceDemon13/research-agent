using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public sealed class ManyReturnInvoiceProductViewItem : TelemartEditorViewItemBase, ILocalіzableEntity
    {
        public DateTime DateArrive
        {
            get { return GetProperty(() => DateArrive); }
            set { SetProperty(() => DateArrive, value); }
        }

        public int InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public Currency Currency
        {
            get { return GetProperty(() => Currency); }
            set { SetProperty(() => Currency, value); }
        }

        public string ProductFullName
        {
            get { return GetProperty(() => ProductFullName); }
            set { SetProperty(() => ProductFullName, value); }
        }

        public string ProductFullNameUa
        {
            get { return GetProperty(() => ProductFullNameUa); }
            set { SetProperty(() => ProductFullNameUa, value); }
        }

        public string ProductFullNameEn
        {
            get { return GetProperty(() => ProductFullNameEn); }
            set { SetProperty(() => ProductFullNameEn, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public int MaxQuantity => Math.Min(
            SerialsQuantity > 0
                ? Math.Min(SerialsQuantity, MaxQuantityDefaultFromInvoice)
                : MaxQuantityDefaultFromInvoice,
            StockQuantity);

        public int OutQuantity
        {
            get { return GetProperty(() => OutQuantity); }
            set { SetProperty(() => OutQuantity, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public string ProductPn
        {
            get { return GetProperty(() => ProductPn); }
            set { SetProperty(() => ProductPn, value); }
        }

        public int SerialsQuantity
        {
            get { return GetProperty(() => SerialsQuantity); }
            set { SetProperty(() => SerialsQuantity, value, () => RaisePropertiesChanged(nameof(Quantity), nameof(OutQuantity), nameof(MaxQuantity))); }
        }

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

        public int? BillsQuantity
        {
            get { return GetProperty(() => BillsQuantity); }
            set { SetProperty(() => BillsQuantity, value); }
        }

        public int WarehouseQuantityFree
        {
            get { return GetProperty(() => WarehouseQuantityFree); }
            set { SetProperty(() => WarehouseQuantityFree, value, () => RaisePropertyChanged(nameof(OutQuantity))); }
        }

        public int SumOutQuantity
        {
            get { return GetProperty(() => SumOutQuantity); }
            set { SetProperty(() => SumOutQuantity, value, () => RaisePropertiesChanged(nameof(MaxQuantity), nameof(OutQuantity))); }
        }

        public string DisplayProductFullName => this.GetLocalName(LocalizableNameType.Ukr);

        public static void BuildMetadata(MetadataBuilder<ManyReturnInvoiceProductViewItem> builder)
        {
            builder.Property(x => x.OutQuantity)
                .MatchesInstanceRule((x, y) => x == 0 || x <= y.MaxQuantity, () => "Количество возвращаемого товара не может быть больше, чем принято по накладной")
                .MatchesInstanceRule((x, y) => x == 0 || y.SumOutQuantity == 0 || y.SumOutQuantity <= y.WarehouseQuantityFree, () => "Нельзя вернуть товаров больше чем на свободном остатке");

            builder.Property(x => x.Quantity)
                .MatchesInstanceRule((x, y) => x <= y.MaxQuantity, () => "Количество возвращаемого товара не может быть больше, чем принято по накладной");
        }

        public string Name => ProductFullName;

        public string NameUkr => ProductFullNameUa;

        public string NameEn => ProductFullNameEn;
    }
}