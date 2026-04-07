using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Contractor.ParserSettings
{
    public class ParserSettingsFileViewItem : TelemartViewItemBase
    {
        public int StartLine
        {
            get { return GetProperty(() => StartLine); }
            set { SetProperty(() => StartLine, value); }
        }

        public string StartWord
        {
            get { return GetProperty(() => StartWord); }
            set { SetProperty(() => StartWord, value); }
        }

        public string StopWord
        {
            get { return GetProperty(() => StopWord); }
            set { SetProperty(() => StopWord, value); }
        }

        public int? SheetsTypeId
        {
            get { return GetProperty(() => SheetsTypeId); }
            set { SetProperty(() => SheetsTypeId, value, () => RaisePropertiesChanged(nameof(SheetsRecognizeTypeId), nameof(SheetsRecognizePattern))); }
        }

        public int? SheetsRecognizeTypeId
        {
            get { return GetProperty(() => SheetsRecognizeTypeId); }
            set { SetProperty(() => SheetsRecognizeTypeId, value); }
        }

        public string SheetsRecognizePattern
        {
            get { return GetProperty(() => SheetsRecognizePattern); }
            set { SetProperty(() => SheetsRecognizePattern, value); }
        }

        public int? SupplierWarehouseId
        {
            get { return GetProperty(() => SupplierWarehouseId); }
            set { SetProperty(() => SupplierWarehouseId, value); }
        }

        public ParserSettingsFileColumnViewItem FileColumn
        {
            get { return GetProperty(() => FileColumn); }
            set { SetProperty(() => FileColumn, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ParserSettingsFileViewItem> builder)
        {
            builder.Property(x => x.StartLine).MatchesRule(x => x > 0, () => "Номер строки не должен быть отрицательным");
            builder.Property(x => x.SheetsRecognizeTypeId).MatchesInstanceRule((x, y) => x is not null || y.SheetsTypeId != ParserSettingsFileSheetsType.Selectively.Id, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.SheetsRecognizePattern).MatchesInstanceRule((x, y) => x is not null || y.SheetsTypeId != ParserSettingsFileSheetsType.Selectively.Id, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.SheetsTypeId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SupplierWarehouseId).Required(() => Resources.RequiredErrorMessage);
        }
    }
}