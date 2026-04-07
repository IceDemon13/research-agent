using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public sealed class FeatureViewItem : BindableBase, ICloneable, IDataErrorInfo, ILockableEntity, IComparable, IComparable<FeatureViewItem>
    {
        public FeatureViewItem()
        {
            ShowOnSite = true;
        }

        public static string TypeDisplayValue => "Характеристика";

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int GroupId
        {
            get { return GetProperty(() => GroupId); }
            set { SetProperty(() => GroupId, value); }
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

        public bool IsMultiValue
        {
            get { return GetProperty(() => IsMultiValue); }
            set { SetProperty(() => IsMultiValue, value, IsMultiValueChangedCallback); }
        }

        public bool MultiLanguage
        {
            get { return GetProperty(() => MultiLanguage); }
            set { SetProperty(() => MultiLanguage, value); }
        }

        public bool ManualInput
        {
            get { return GetProperty(() => ManualInput); }
            set { SetProperty(() => ManualInput, value); }
        }

        public bool Settings
        {
            get { return GetProperty(() => Settings); }
            set { SetProperty(() => Settings, value); }
        }

        public int? FeatureOptionId
        {
            get { return GetProperty(() => FeatureOptionId); }
            set { SetProperty(() => FeatureOptionId, value); }
        }

        public int? FeatureValueDiscountId
        {
            get { return GetProperty(() => FeatureValueDiscountId); }
            set { SetProperty(() => FeatureValueDiscountId, value); }
        }

        public string Separator
        {
            get { return GetProperty(() => Separator); }
            set { SetProperty(() => Separator, value); }
        }

        public string Hint
        {
            get { return GetProperty(() => Hint); }
            set { SetProperty(() => Hint, value); }
        }

        public string HintUkr
        {
            get { return GetProperty(() => HintUkr); }
            set { SetProperty(() => HintUkr, value); }
        }

        public string HintEn
        {
            get { return GetProperty(() => HintEn); }
            set { SetProperty(() => HintEn, value); }
        }

        public string Suffix
        {
            get { return GetProperty(() => Suffix); }
            set { SetProperty(() => Suffix, value); }
        }

        public string SuffixUkr
        {
            get { return GetProperty(() => SuffixUkr); }
            set { SetProperty(() => SuffixUkr, value); }
        }

        public string SuffixEn
        {
            get { return GetProperty(() => SuffixEn); }
            set { SetProperty(() => SuffixEn, value); }
        }

        public string Mask
        {
            get { return GetProperty(() => Mask); }
            set { SetProperty(() => Mask, value); }
        }

        public string Regex
        {
            get { return GetProperty(() => Regex); }
            set { SetProperty(() => Regex, value); }
        }

        public bool ShowOnSite
        {
            get { return GetProperty(() => ShowOnSite); }
            set { SetProperty(() => ShowOnSite, value); }
        }

        public bool HideMinus
        {
            get { return GetProperty(() => HideMinus); }
            set { SetProperty(() => HideMinus, value); }
        }

        public bool PrintInTags
        {
            get { return GetProperty(() => PrintInTags); }
            set { SetProperty(() => PrintInTags, value); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public ObservableCollection<FeatureContractorKeyViewItem> FeatureContractorKeys
        {
            get { return GetProperty(() => FeatureContractorKeys); }
            set { SetProperty(() => FeatureContractorKeys, value); }
        }

        public bool CustomRegex
        {
            get { return GetProperty(() => CustomRegex); }
            set { SetProperty(() => CustomRegex, value, () => RaisePropertiesChanged(nameof(Regex))); }
        }

        public int? IconId
        {
            get { return GetProperty(() => IconId); }
            set { SetProperty(() => IconId, value); }
        }

        public string IconUrl
        {
            get { return GetProperty(() => IconUrl); }
            set { SetProperty(() => IconUrl, value); }
        }

        public string FullNameIcon
        {
            get { return GetProperty(() => FullNameIcon); }
            set { SetProperty(() => FullNameIcon, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static bool operator <(FeatureViewItem left, FeatureViewItem right)
        {
            return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(FeatureViewItem left, FeatureViewItem right)
        {
            return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
        }

        public static bool operator >(FeatureViewItem left, FeatureViewItem right)
        {
            return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(FeatureViewItem left, FeatureViewItem right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
        }

        public static void BuildMetadata(MetadataBuilder<FeatureViewItem> builder)
        {
            builder.Property(x => x.GroupId)
                .MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(100);

            builder.Property(x => x.NameUkr)
               .Required(() => Resources.RequiredErrorMessage)
               .MaxLength(100);

            builder.Property(x => x.NameEn)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(100);

            builder.Property(x => x.Hint)
                .MaxLength(200);

            builder.Property(x => x.HintUkr)
              .MaxLength(200);

            builder.Property(x => x.HintEn)
                .MaxLength(200);

            builder.Property(x => x.Suffix)
                .MaxLength(20);

            builder.Property(x => x.SuffixUkr)
               .MaxLength(20);

            builder.Property(x => x.SuffixEn)
                .MaxLength(20);

            builder.Property(x => x.Regex).MatchesInstanceRule((x, y) => !y.CustomRegex || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Separator)
                .MatchesInstanceRule(
                    (x, y) => !y.IsMultiValue || !string.IsNullOrEmpty(x),
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.FeatureOptionId)
                .MatchesInstanceRule(
                    (x, y) => x.HasValue || y.Settings == false,
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.FeatureValueDiscountId)
                .MatchesInstanceRule(
                    (x, y) => x.HasValue || y.FeatureOptionId != FeatureOption.PredetermineParentId,
                    () => Resources.RequiredErrorMessage);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public FeatureViewItem Clone()
        {
            FeatureViewItem item = ReflectionObjectCloner.Clone(this);

            if (FeatureContractorKeys != null)
            {
                item.FeatureContractorKeys = FeatureContractorKeys.Select(x =>
                {
                    FeatureContractorKeyViewItem viewItem = ReflectionObjectCloner.Clone(x);
                    return viewItem;
                }).ToObservableCollection();
            }

            return item;
        }

        public int CompareTo(object other)
        {
            return CompareTo(other as FeatureViewItem);
        }

        public int CompareTo(FeatureViewItem other)
        {
            if (other == null)
            {
                return 1;
            }

            return Position.CompareTo(other.Position);
        }

        private void IsMultiValueChangedCallback()
        {
            if (!IsMultiValue)
            {
                Separator = null;
            }

            RaisePropertyChanged(nameof(Separator));
        }
    }
}