using System.Threading.Tasks;

namespace Telemart.Client.Common.Utils.Import
{
    public interface IExcelImportEngineAsync<TRow>
    {
        Task<ExcelImportResult<TRow>> ImportFromXlsxAsync(string fileName);
    }
}
