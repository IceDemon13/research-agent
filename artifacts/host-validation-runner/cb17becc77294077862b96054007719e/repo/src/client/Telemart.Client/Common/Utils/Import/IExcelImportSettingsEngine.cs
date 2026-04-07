namespace Telemart.Client.Common.Utils.Import
{
    public interface IExcelImportSettingsEngine<TRow, in TSettings>
    {
        ExcelImportResult<TRow> ImportFromXlsx(string fileName, TSettings settings);
    }
}
