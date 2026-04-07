using System.Text.RegularExpressions;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.TradeIn
{
    public class TraderInCoefViewItem : TelemartViewItemBase
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

        public decimal? Coef
        {
            get { return GetProperty(() => Coef); }
            set { SetProperty(() => Coef, value); }
        }

        public int IndicatorValueId
        {
            get { return GetProperty(() => IndicatorValueId); }
            set { SetProperty(() => IndicatorValueId, value); }
        }

        public int IndicatorId
        {
            get { return GetProperty(() => IndicatorId); }
            set { SetProperty(() => IndicatorId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<TraderInCoefViewItem> builder)
        {
            builder.Property(x => x.Coef)
                .MatchesRule(x => x is >= 0 and < 1, () => "Коэффициент должен быть больше либо равен 0 и меньше 1");
        }
    }
}