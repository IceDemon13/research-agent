using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Payments
{
    public class CreditOfferViewItem : TelemartViewItemBase
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
        
        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value); }
        }
        
        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value); }
        }

        public int PaymentId
        {
            get { return GetProperty(() => PaymentId); }
            set { SetProperty(() => PaymentId, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public int Month
        {
            get { return GetProperty(() => Month); }
            set { SetProperty(() => Month, value); }
        }

        public int CreditPartCount
        {
            get { return GetProperty(() => CreditPartCount); }
            set { SetProperty(() => CreditPartCount, value); }
        }

        public decimal? Fee
        {
            get { return GetProperty(() => Fee); }
            set { SetProperty(() => Fee, value); }
        }

        public static void BuildMetadata(MetadataBuilder<CreditOfferViewItem> builder)
        {
            builder.Property(x => x.Fee)
                .MatchesRule(x => x is >= 0 and < 100, () => "Значение должно быть больше или равно 0 и меньше 100");
            builder.Property(x => x.CreditPartCount)
                .MatchesRule(x => x is > 0 and < 49, () => "Значение должно быть больше 0 и меньше 49");
            builder.Property(x => x.Month)
                .MatchesRule(x => x is > 0 and < 49, () => "Значение должно быть больше 0 и меньше 49");
            builder.Property(x => x.Name)
                .MatchesRule(x => x is { Length: > 0 and < 100 }, () => "Длина названия должна быть от 1 до 100 символов");
            builder.Property(x => x.NameUkr)
                .MatchesRule(x => x is { Length: > 0 and < 100 }, () => "Длина названия должна быть от 1 до 100 символов");
            builder.Property(x => x.NameEn)
                .MatchesRule(x => x is { Length: > 0 and < 100 }, () => "Длина названия должна быть от 1 до 100 символов");
            builder.Property(x => x.PaymentId)
                .Required(() => Resources.RequiredErrorMessage);
        }
    }
}