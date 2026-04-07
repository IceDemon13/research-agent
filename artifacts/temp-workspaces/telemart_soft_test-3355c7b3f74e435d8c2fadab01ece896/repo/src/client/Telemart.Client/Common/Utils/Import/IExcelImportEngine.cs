namespace Telemart.Client.Common.Utils.Import
{
    public interface IExcelImportEngine<TRow>
    {
        ExcelImportResult<TRow> ImportFromXlsx(string fileName);
    }
}
