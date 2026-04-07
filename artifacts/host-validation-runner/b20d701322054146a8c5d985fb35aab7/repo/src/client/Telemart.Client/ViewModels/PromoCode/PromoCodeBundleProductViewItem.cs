using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.PromoCode
{
    public class PromoCodeBundleProductViewItem : TelemartViewItemBase
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

        public int? DiscountModeId
        {
            get { return GetProperty(() => DiscountModeId); }
            set { SetProperty(() => DiscountModeId, value, RaiseImagePropertyChanged); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public int? DiscountAmount
        {
            get { return GetProperty(() => DiscountAmount); }
            set { SetProperty(() => DiscountAmount, value, RaiseImagePropertyChanged); }
        }

        public bool GiftImageVisible => DiscountModeId == PromoCodeDiscountMode.Price.Id && DiscountAmount == 1;

        public bool BonusesImageVisible => DiscountModeId == PromoCodeDiscountMode.CashbackPercentage.Id || DiscountModeId == PromoCodeDiscountMode.FixedCashback.Id;

        public bool DiscountAmountVisible => DiscountModeId == PromoCodeDiscountMode.Percent.Id || DiscountModeId == PromoCodeDiscountMode.PriceDiscount.Id || (DiscountModeId == PromoCodeDiscountMode.Price.Id && DiscountAmount != 1);

        public static void BuildMetadata(MetadataBuilder<PromoCodeBundleProductViewItem> builder)
        {
            builder.Property(x => x.Quantity).MatchesRule(x => x is > 0 and < 100, () => "Количество должно быть больше 0 и меньше 100");
            builder.Property(x => x.DiscountAmount)
                .MatchesInstanceRule(
                    (x, y) => x is null
                              || (y.DiscountModeId != PromoCodeDiscountMode.CashbackPercentage.Id && y.DiscountModeId != PromoCodeDiscountMode.Percent.Id)
                              || x is > 0 and < 100,
                    () => "Значение скидки в % должно быть больше 0 и меньше 100")
                .MatchesInstanceRule(
                    (x, y) => x is null
                              || (y.DiscountModeId != PromoCodeDiscountMode.Price.Id && y.DiscountModeId != PromoCodeDiscountMode.PriceDiscount.Id && y.DiscountModeId != PromoCodeDiscountMode.FixedCashback.Id)
                              || x is > 0 and < 999_999,
                    () => "Значение скидки в гривнах или бонусах должно быть больше 0 и меньше 999999");
        }

        private void RaiseImagePropertyChanged()
        {
            RaisePropertyChanged(nameof(GiftImageVisible));
            RaisePropertyChanged(nameof(BonusesImageVisible));
            RaisePropertyChanged(nameof(DiscountAmountVisible));
            RaisePropertyChanged(nameof(DiscountAmount));
            RaisePropertyChanged(nameof(DiscountModeId));
        }
    }
}