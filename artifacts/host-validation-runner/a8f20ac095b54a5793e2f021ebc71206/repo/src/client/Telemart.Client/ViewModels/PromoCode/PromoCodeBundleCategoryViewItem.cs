using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.PromoCode
{
    public class PromoCodeBundleCategoryViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int? DiscountModeId
        {
            get { return GetProperty(() => DiscountModeId); }
            set { SetProperty(() => DiscountModeId, value, RaiseImagePropertyChanged); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public string CompareMethod
        {
            get { return GetProperty(() => CompareMethod); }
            set { SetProperty(() => CompareMethod, value); }
        }

        public int? FeatureId
        {
            get { return GetProperty(() => FeatureId); }
            set { SetProperty(() => FeatureId, value); }
        }

        public string FeatureName
        {
            get { return GetProperty(() => FeatureName); }
            set { SetProperty(() => FeatureName, value); }
        }

        public int? FeatureValueId
        {
            get { return GetProperty(() => FeatureValueId); }
            set { SetProperty(() => FeatureValueId, value); }
        }

        public string FeatureValueName
        {
            get { return GetProperty(() => FeatureValueName); }
            set { SetProperty(() => FeatureValueName, value); }
        }

        public int? DiscountAmount
        {
            get { return GetProperty(() => DiscountAmount); }
            set { SetProperty(() => DiscountAmount, value, RaiseImagePropertyChanged); }
        }

        public bool GiftImageVisible => DiscountModeId == PromoCodeDiscountMode.Price.Id && DiscountAmount == 1;

        public bool BonusesImageVisible => DiscountModeId == PromoCodeDiscountMode.CashbackPercentage.Id || DiscountModeId == PromoCodeDiscountMode.FixedCashback.Id;

        public bool DiscountAmountVisible => DiscountModeId == PromoCodeDiscountMode.Percent.Id || DiscountModeId == PromoCodeDiscountMode.PriceDiscount.Id || (DiscountModeId == PromoCodeDiscountMode.Price.Id && DiscountAmount != 1);

        public static void BuildMetadata(MetadataBuilder<PromoCodeBundleCategoryViewItem> builder)
        {
            builder.Property(x => x.CompareMethod).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CategoryId).Required(() => Resources.RequiredErrorMessage);
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