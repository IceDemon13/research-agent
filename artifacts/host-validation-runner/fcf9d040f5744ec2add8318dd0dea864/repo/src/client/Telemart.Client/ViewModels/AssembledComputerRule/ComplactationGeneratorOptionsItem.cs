using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public sealed class ComplactationGeneratorOptionsItem : BindableBase
    {
        public bool UseWarehouse
        {
            get { return GetProperty(() => UseWarehouse); }
            set { SetProperty(() => UseWarehouse, value, () => RaisePropertyChanged(nameof(IsSomeSourcesChecked))); }
        }

        public bool UseTransits
        {
            get { return GetProperty(() => UseTransits); }
            set { SetProperty(() => UseTransits, value, () => RaisePropertyChanged(nameof(IsSomeSourcesChecked))); }
        }

        public bool UsePurchases
        {
            get { return GetProperty(() => UsePurchases); }
            set { SetProperty(() => UsePurchases, value, () => RaisePropertyChanged(nameof(IsSomeSourcesChecked))); }
        }

        public decimal OverPricePercent
        {
            get { return GetProperty(() => OverPricePercent); }
            set { SetProperty(() => OverPricePercent, value); }
        }

        public bool IsSomeSourcesChecked => (!UsePurchases && !UseTransits && !UseWarehouse) == false;

        public static void BuildMetadata(MetadataBuilder<ComplactationGeneratorOptionsItem> builder)
        {
            builder.Property(x => x.IsSomeSourcesChecked)
                .MatchesRule(x => x, () => string.Empty);
        }
    }
}