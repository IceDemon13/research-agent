using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.PromoCode
{
    public class PromoCodeProductViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int PromoCodeId
        {
            get { return GetProperty(() => PromoCodeId); }
            set { SetProperty(() => PromoCodeId, value); }
        }

        public int? DiscountModeId
        {
            get { return GetProperty(() => DiscountModeId); }
            set { SetProperty(() => DiscountModeId, value, () => RaisePropertyChanged(nameof(Amount))); }
        }

        public int? CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value, () => RaisePropertyChanged(nameof(IsProduct))); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int? Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value); }
        }

        public bool ShowInSite
        {
            get { return GetProperty(() => ShowInSite); }
            set { SetProperty(() => ShowInSite, value); }
        }

        public bool IsProduct => ProductId.HasValue;

        public static void BuildMetadata(MetadataBuilder<PromoCodeProductViewItem> builder)
        {
            builder.Property(x => x.DiscountModeId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Amount)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => y.DiscountModeId != PromoCodeDiscountMode.Percent.Id || !x.HasValue || (x > 0 && x < 100), () => "Значение поля должно быть больше 0 и меньше 100");
        }
    }
}
