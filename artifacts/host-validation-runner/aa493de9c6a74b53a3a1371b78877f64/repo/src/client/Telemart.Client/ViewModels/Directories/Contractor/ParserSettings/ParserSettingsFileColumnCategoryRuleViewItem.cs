using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Contractor.ParserSettings
{
    public class ParserSettingsFileColumnCategoryRuleViewItem : TelemartViewItemBase
    {
        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value, () => RaisePropertyChanged(nameof(ColumnNumber))); }
        }

        public byte ColumnNumber
        {
            get { return GetProperty(() => ColumnNumber); }
            set { SetProperty(() => ColumnNumber, value); }
        }

        public bool EqualsCondition
        {
            get { return GetProperty(() => EqualsCondition); }
            set { SetProperty(() => EqualsCondition, value); }
        }

        public string Pattern
        {
            get { return GetProperty(() => Pattern); }
            set { SetProperty(() => Pattern, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ParserSettingsFileColumnCategoryRuleViewItem> builder)
        {
            builder.Property(x => x.ColumnNumber).MatchesInstanceRule((x, y) => !y.Active || (x is > 0 and < 100), () => "Значение должно быть в диапазоне [1..99]");
        }
    }
}