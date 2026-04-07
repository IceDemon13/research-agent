using System.Collections.ObjectModel;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Contractor.ParserSettings
{
    public class ParserSettingsFileColumnViewItem : TelemartViewItemBase
    {
        public string NameColumnNumbers
        {
            get { return GetProperty(() => NameColumnNumbers); }
            set { SetProperty(() => NameColumnNumbers, value); }
        }

        public string CategoryColumnNumbers
        {
            get { return GetProperty(() => CategoryColumnNumbers); }
            set { SetProperty(() => CategoryColumnNumbers, value); }
        }

        public string CodeColumnNumbers
        {
            get { return GetProperty(() => CodeColumnNumbers); }
            set { SetProperty(() => CodeColumnNumbers, value); }
        }

        public string PartNumberColumnNumbers
        {
            get { return GetProperty(() => PartNumberColumnNumbers); }
            set { SetProperty(() => PartNumberColumnNumbers, value); }
        }

        public int? AvailColumnNumber
        {
            get { return GetProperty(() => AvailColumnNumber); }
            set { SetProperty(() => AvailColumnNumber, value); }
        }

        public ParserSettingsFileColumnCategoryRuleViewItem CategoryRule1
        {
            get { return GetProperty(() => CategoryRule1); }
            set { SetProperty(() => CategoryRule1, value); }
        }

        public ParserSettingsFileColumnCategoryRuleViewItem CategoryRule2
        {
            get { return GetProperty(() => CategoryRule2); }
            set { SetProperty(() => CategoryRule2, value); }
        }

        public ParserSettingsFileColumnCategoryRuleViewItem CategoryRule3
        {
            get { return GetProperty(() => CategoryRule3); }
            set { SetProperty(() => CategoryRule3, value); }
        }

        public ObservableCollection<ParserSettingsFilePriceViewItem> Prices
        {
            get { return GetProperty(() => Prices); }
            set { SetProperty(() => Prices, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ParserSettingsFileColumnViewItem> builder)
        {
            const string separatorError = "Допускаются только целыe числа, разделенные запятой";
            const string separatorRegex = @"^(\d+(,|,\d+)*)?$";

            builder.Property(x => x.AvailColumnNumber).MatchesRule(x => x is null or > 0 and < 100, () => "Значение должно быть в диапазоне [1..99]");
            builder.Property(x => x.CodeColumnNumbers)
                .MatchesRegularExpression(separatorRegex, () => separatorError)
                .MatchesInstanceRule(
                    (x, y) => !string.IsNullOrWhiteSpace(y.PartNumberColumnNumbers) || !string.IsNullOrWhiteSpace(x),
                    () => Resources.RequiredErrorMessage);
            builder.Property(x => x.PartNumberColumnNumbers)
                .MatchesRegularExpression(separatorRegex, () => separatorError)
                .MatchesInstanceRule(
                    (x, y) => !string.IsNullOrWhiteSpace(y.CodeColumnNumbers) || !string.IsNullOrWhiteSpace(x),
                    () => Resources.RequiredErrorMessage);
            builder.Property(x => x.CategoryColumnNumbers)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesRegularExpression(separatorRegex, () => separatorError);
            builder.Property(x => x.NameColumnNumbers)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesRegularExpression(separatorRegex, () => separatorError);
        }
    }
}