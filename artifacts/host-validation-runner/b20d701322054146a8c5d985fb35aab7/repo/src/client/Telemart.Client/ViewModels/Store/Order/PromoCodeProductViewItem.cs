using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class PromoCodeProductViewItem : BindableBase, ILocalіzableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public int? PromoCodeId
        {
            get { return GetProperty(() => PromoCodeId); }
            set { SetProperty(() => PromoCodeId, value); }
        }

        public string PromoCode
        {
            get { return GetProperty(() => PromoCode); }
            set { SetProperty(() => PromoCode, value); }
        }

        public decimal? Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public bool IsGift
        {
            get { return GetProperty(() => IsGift); }
            set { SetProperty(() => IsGift, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public decimal? PriceCurrent
        {
            get { return GetProperty(() => PriceCurrent); }
            set { SetProperty(() => PriceCurrent, value); }
        }

        public decimal PriceNew
        {
            get { return GetProperty(() => PriceNew); }
            set { SetProperty(() => PriceNew, value); }
        }

        public int? BonusesToChargeCurrent
        {
            get { return GetProperty(() => BonusesToChargeCurrent); }
            set { SetProperty(() => BonusesToChargeCurrent, value); }
        }

        public int? BonusesToChargeNew
        {
            get { return GetProperty(() => BonusesToChargeNew); }
            set { SetProperty(() => BonusesToChargeNew, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public int CurrencyCurrentId
        {
            get { return GetProperty(() => CurrencyCurrentId); }
            set { SetProperty(() => CurrencyCurrentId, value); }
        }

        public int CurrencyNewId
        {
            get { return GetProperty(() => CurrencyNewId); }
            set { SetProperty(() => CurrencyNewId, value); }
        }

        public string ProductName => this.GetLocalName(LocalizableNameType.Ukr);
    }
}