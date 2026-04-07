using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.PromoCode
{
    public class PromoCodeViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public int? SellPlan
        {
            get { return GetProperty(() => SellPlan); }
            set { SetProperty(() => SellPlan, value); }
        }

        public DateTime? DateStart
        {
            get { return GetProperty(() => DateStart); }
            set { SetProperty(() => DateStart, value, () => RaisePropertyChanged(nameof(DateEnd))); }
        }

        public DateTime? DateEnd
        {
            get { return GetProperty(() => DateEnd); }
            set { SetProperty(() => DateEnd, value, () => RaisePropertyChanged(nameof(DateStart))); }
        }

        public string MetaTitle
        {
            get { return GetProperty(() => MetaTitle); }
            set { SetProperty(() => MetaTitle, value); }
        }

        public string MetaTitleUkr
        {
            get { return GetProperty(() => MetaTitleUkr); }
            set { SetProperty(() => MetaTitleUkr, value); }
        }

        public string MetaTitleEn
        {
            get { return GetProperty(() => MetaTitleEn); }
            set { SetProperty(() => MetaTitleEn, value); }
        }

        public bool ShowInSite
        {
            get { return GetProperty(() => ShowInSite); }
            set { SetProperty(() => ShowInSite, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<PromoCodeViewItem> builder)
        {
            builder.Property(x => x.SellPlan).MatchesRule(x => x is null || (x > 0 && x < 1_000_000), () => "Значение должно быть в диапазоне 1..1000000");
            builder.Property(x => x.Value).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DateStart).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DateEnd).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.MetaTitle).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.MetaTitleUkr).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.MetaTitleEn).Required(() => Resources.RequiredErrorMessage);
        }
    }
}
