using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Validation;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Common.Localization;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class InvoiceProductViewItem : BindableBase, ILocalіzableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value); }
        }

        public IPriceConverter PriceConverter
        {
            get { return GetProperty(() => PriceConverter); }
            set { SetProperty(() => PriceConverter, value, CalculateExtraCharge); }
        }

        public string SupplierProductId
        {
            get { return GetProperty(() => SupplierProductId); }
            set { SetProperty(() => SupplierProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public string ProductNameUa
        {
            get { return GetProperty(() => ProductNameUa); }
            set { SetProperty(() => ProductNameUa, value); }
        }

        public string ProductNameEn
        {
            get { return GetProperty(() => ProductNameEn); }
            set { SetProperty(() => ProductNameEn, value); }
        }

        public string ProductPrefix
        {
            get { return GetProperty(() => ProductPrefix); }
            set { SetProperty(() => ProductPrefix, value); }
        }

        public string ProductPn
        {
            get { return GetProperty(() => ProductPn); }
            set { SetProperty(() => ProductPn, value); }
        }

        public string FullName
        {
            get { return GetProperty(() => FullName); }
            set { SetProperty(() => FullName, value); }
        }

        public string FullNameUa
        {
            get { return GetProperty(() => FullNameUa); }
            set { SetProperty(() => FullNameUa, value); }
        }

        public string FullNameEn
        {
            get { return GetProperty(() => FullNameEn); }
            set { SetProperty(() => FullNameEn, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public decimal? ExtraCharge
        {
            get { return GetProperty(() => ExtraCharge); }
            set { SetProperty(() => ExtraCharge, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value, () => RaisePropertiesChanged(nameof(IsOrderQuantityMoreThanInvoiceQuantity), nameof(UnitAdditionalCost))); }
        }

        public int OrderQuantity
        {
            get { return GetProperty(() => OrderQuantity); }
            set { SetProperty(() => OrderQuantity, value); }
        }

        public int? QuantityReal
        {
            get { return GetProperty(() => QuantityReal); }
            set { SetProperty(() => QuantityReal, value); }
        }

        public int QuantityReturned
        {
            get { return GetProperty(() => QuantityReturned); }
            set { SetProperty(() => QuantityReturned, value); }
        }

        public int BillsQuantity
        {
            get { return GetProperty(() => BillsQuantity); }
            set { SetProperty(() => BillsQuantity, value); }
        }

        public int QuantityReserved
        {
            get { return GetProperty(() => QuantityReserved); }
            set { SetProperty(() => QuantityReserved, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value, CalculateExtraCharge); }
        }

        public decimal? PriceTelemartUah
        {
            get { return GetProperty(() => PriceTelemartUah); }
            set { SetProperty(() => PriceTelemartUah, value, CalculateExtraCharge); }
        }

        public Currency Currency
        {
            get { return GetProperty(() => Currency); }
            set { SetProperty(() => Currency, value, CurrencyChagedCallback); }
        }

        public Currency OriginalCurrency
        {
            get { return GetProperty(() => OriginalCurrency); }
            set { SetProperty(() => OriginalCurrency, value); }
        }

        public int EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public double Weight
        {
            get { return GetProperty(() => Weight); }
            set { SetProperty(() => Weight, value); }
        }

        public decimal AdditionalCost
        {
            get { return GetProperty(() => AdditionalCost); }
            set { SetProperty(() => AdditionalCost, value, () => RaisePropertiesChanged(nameof(UnitAdditionalCost))); }
        }

        public string ImageToolTip
        {
            get { return GetProperty(() => ImageToolTip); }
            set { SetProperty(() => ImageToolTip, value); }
        }

        public bool ErrorImage
        {
            get { return GetProperty(() => ErrorImage); }
            set { SetProperty(() => ErrorImage, value); }
        }

        public EmployeeSimpleDto Employee
        {
            get { return GetProperty(() => Employee); }
            set { SetProperty(() => Employee, value); }
        }

        public ObservableCollection<string> SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            set { SetProperty(() => SerialNumbers, value); }
        }

        public ObservableCollection<string> Barcodes
        {
            get { return GetProperty(() => Barcodes); }
            set { SetProperty(() => Barcodes, value); }
        }

        public bool IsEditableForCurrentUser
        {
            get { return GetProperty(() => IsEditableForCurrentUser); }
            set { SetProperty(() => IsEditableForCurrentUser, value); }
        }

        public bool CanEditCurrency
        {
            get { return GetProperty(() => CanEditCurrency); }
            set { SetProperty(() => CanEditCurrency, value); }
        }

        public bool PriceRedColor
        {
            get { return GetProperty(() => PriceRedColor); }
            set { SetProperty(() => PriceRedColor, value); }
        }

        public bool PriceGreenColor
        {
            get { return GetProperty(() => PriceGreenColor); }
            set { SetProperty(() => PriceGreenColor, value); }
        }

        public bool Preorder
        {
            get { return GetProperty(() => Preorder); }
            set { SetProperty(() => Preorder, value); }
        }

        public bool IsOrderQuantityMoreThanInvoiceQuantity => OrderQuantity > Quantity;

        public decimal UnitAdditionalCost => Quantity == 0
            ? 0
            : AdditionalCost / Quantity;

        public string Name => ProductName;

        public string NameUkr => ProductNameUa;

        public string NameEn => ProductNameEn;

        public string ProductDisplayName => this.GetLocalName(LocalizableNameType.Ukr);

        public static void BuildMetadata(MetadataBuilder<InvoiceProductViewItem> builder)
        {
            builder.Property(x => x.Price).MaxProductPrice();
        }

        public void SetPriceConverter(IPriceConverter priceConverter)
        {
            PriceConverter = priceConverter;
        }

        public string GetLocalFullName(LocalizableNameType type) => type switch
        {
            LocalizableNameType.Ru => FullName,
            LocalizableNameType.Ukr => string.IsNullOrEmpty(FullNameUa) ? FullName : FullNameUa,
            LocalizableNameType.En => string.IsNullOrEmpty(FullNameEn) ? FullName : FullNameEn,
            _ => FullName
        };

        private void CalculateExtraCharge()
        {
            if (PriceConverter is null || !PriceConverter.CanConvert(Currency?.Id ?? 0, Currency.UahId, CurrencyTypeIds.UsdPlusId))
            {
                return;
            }

            decimal price = Currency is null ? 0 : PriceConverter.Convert(Price, Currency.Id, Currency.UahId, CurrencyTypeIds.UsdPlusId);

            decimal? extraCharge = ProductHelper.GetExtraCharge(price + UnitAdditionalCost, PriceTelemartUah);

            ExtraCharge = extraCharge;
        }

        private void CurrencyChagedCallback()
        {
            OriginalCurrency ??= Currency;
            CalculateExtraCharge();
        }
    }
}