using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Tools.Tags
{
    public sealed class ProductTagViewItem : BindableBase, ILocalіzableEntity
    {
        private readonly LocalizableNameType _localizableNameType = LocalizableNameType.Ukr;

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value, () => RaisePropertyChanged(nameof(DisplayName))); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value, () => RaisePropertyChanged(nameof(DisplayName))); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value, () => RaisePropertyChanged(nameof(DisplayName))); }
        }

        public string Link
        {
            get { return GetProperty(() => Link); }
            set { SetProperty(() => Link, value); }
        }

        public string LinkUkr
        {
            get { return GetProperty(() => LinkUkr); }
            set { SetProperty(() => LinkUkr, value); }
        }

        public int? BonusAmount
        {
            get { return GetProperty(() => BonusAmount); }
            set { SetProperty(() => BonusAmount, value); }
        }

        public bool ActivePromo
        {
            get { return GetProperty(() => ActivePromo); }
            set { SetProperty(() => ActivePromo, value); }
        }

        public string Category
        {
            get { return GetProperty(() => Category); }
            set { SetProperty(() => Category, value, () => RaisePropertyChanged(nameof(DisplayCategory))); }
        }

        public string CategoryUkr
        {
            get { return GetProperty(() => CategoryUkr); }
            set { SetProperty(() => CategoryUkr, value, () => RaisePropertyChanged(nameof(DisplayCategory))); }
        }

        public TagColor Color
        {
            get { return GetProperty(() => Color); }
            set { SetProperty(() => Color, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public decimal PricePrev
        {
            get { return GetProperty(() => PricePrev); }
            set { SetProperty(() => PricePrev, value); }
        }

        public TagFormat TagFormat
        {
            get { return GetProperty(() => TagFormat); }
            set { SetProperty(() => TagFormat, value); }
        }

        public string DisplayName => this.GetLocalName(_localizableNameType);

        public string DisplayCategory => _localizableNameType switch
        {
            LocalizableNameType.Ru => Category,
            LocalizableNameType.Ukr => CategoryUkr,
            _ => Category
        };

        public static void BuildMetadata(MetadataBuilder<ProductTagViewItem> builder)
        {
            builder.Property(x => x.TagFormat)
                .MatchesInstanceRule((x, y) => !(x == TagFormat.Promo && y.ActivePromo == false), () => "На товар нет активной акции");
        }
    }
}