using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class InvoiceCurrencyRateViewItem : TelemartCloneableViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int FromCurrencyId
        {
            get { return GetProperty(() => FromCurrencyId); }
            set { SetProperty(() => FromCurrencyId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public decimal? ConversionRate
        {
            get { return GetProperty(() => ConversionRate); }
            set { SetProperty(() => ConversionRate, value); }
        }

        public static void BuildMetadata(MetadataBuilder<InvoiceCurrencyRateViewItem> builder)
        {
            builder.Property(x => x.ConversionRate)
                .MatchesInstanceRule((x, y) => x is null || (x > 0 && x < 999), () => "Курс должен быть в диапазоне 0..999");
        }
    }
}