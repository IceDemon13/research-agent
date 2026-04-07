using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public class CurrencyTypeRateViewItem : TelemartCloneableViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int FromCurrencyTypeId
        {
            get { return GetProperty(() => FromCurrencyTypeId); }
            set { SetProperty(() => FromCurrencyTypeId, value); }
        }

        public int FromCurrencyId
        {
            get { return GetProperty(() => FromCurrencyId); }
            set { SetProperty(() => FromCurrencyId, value); }
        }

        public int ToCurrencyTypeId
        {
            get { return GetProperty(() => ToCurrencyTypeId); }
            set { SetProperty(() => ToCurrencyTypeId, value); }
        }

        public decimal ConversionRate
        {
            get { return GetProperty(() => ConversionRate); }
            set { SetProperty(() => ConversionRate, value, () => RaisePropertyChanged(nameof(ConversionRateChanged))); }
        }

        public decimal ConversionRateInitial
        {
            get { return GetProperty(() => ConversionRateInitial); }
            set { SetProperty(() => ConversionRateInitial, value); }
        }

        public decimal ConversionRateOld
        {
            get { return GetProperty(() => ConversionRateOld); }
            set { SetProperty(() => ConversionRateOld, value); }
        }

        public decimal? AutoConvertedFromConversionRate
        {
            get { return GetProperty(() => AutoConvertedFromConversionRate); }
            set { SetProperty(() => AutoConvertedFromConversionRate, value); }
        }

        public bool ConversionRateChanged => ConversionRate != ConversionRateOld;

        public bool ConversionRateInitialChanged => ConversionRate != ConversionRateInitial;

        public static void BuildMetadata(MetadataBuilder<CurrencyTypeRateViewItem> builder)
        {
            const decimal MinRateValue = 0;
            const decimal MaxRateValue = 999;

            builder.Property(x => x.ConversionRate)
                .MatchesRule(x => x > MinRateValue && x < MaxRateValue, () => "Курс должен быть в диапазане 0..999");
        }
    }
}