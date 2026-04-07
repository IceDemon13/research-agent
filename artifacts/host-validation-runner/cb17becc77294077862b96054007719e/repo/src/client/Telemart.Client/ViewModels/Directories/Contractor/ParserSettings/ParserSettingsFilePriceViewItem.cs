using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Contractor.ParserSettings
{
    public class ParserSettingsFilePriceViewItem : TelemartViewItemBase
    {
        public byte? ColumnNumber
        {
            get { return GetProperty(() => ColumnNumber); }
            set { SetProperty(() => ColumnNumber, value); }
        }

        public int? CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public int? ParserPriceTypeId
        {
            get { return GetProperty(() => ParserPriceTypeId); }
            set { SetProperty(() => ParserPriceTypeId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ParserSettingsFilePriceViewItem> builder)
        {
            builder.Property(x => x.ColumnNumber).MatchesRule(x => x is > 0 and < 99, () => "Номер колонки должен быть в диапазоне [1..99]");
            builder.Property(x => x.CurrencyId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ParserPriceTypeId).Required(() => Resources.RequiredErrorMessage);
        }
    }
}