using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Carry
{
    public class CarryPriceViewItem : TelemartEditorViewItemBase
    {
        public decimal? WeightFrom
        {
            get { return GetProperty(() => WeightFrom); }
            set { SetProperty(() => WeightFrom, value); }
        }

        public decimal? WeightTo
        {
            get { return GetProperty(() => WeightTo); }
            set { SetProperty(() => WeightTo, value); }
        }

        public int Cost
        {
            get { return GetProperty(() => Cost); }
            set { SetProperty(() => Cost, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public static void BuildMetadata(MetadataBuilder<CarryPriceViewItem> builder)
        {
            builder.Property(x => x.WeightFrom)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.WeightTo)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Cost)
              .MatchesInstanceRule((x, y) => y.Cost >= 0, () => "Значение не может быть отрицательным");
        }
    }
}
