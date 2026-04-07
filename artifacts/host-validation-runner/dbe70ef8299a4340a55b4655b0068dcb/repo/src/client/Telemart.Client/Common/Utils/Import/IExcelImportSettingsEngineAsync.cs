using System.Threading.Tasks;

namespace Telemart.Client.Common.Utils.Import
{
    public interface IExcelImportSettingsEngineAsync<TRow, in TSettings>
    {
        Task<ExcelImportResult<TRow>> ImportFromXlsxAsync(string fileName, TSettings settings);
    }
}
