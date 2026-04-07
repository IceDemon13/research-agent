using System.Threading.Tasks;
using DevExpress.XtraReports;

namespace Telemart.Client.Reports.ReportBuilders.Base
{
    public interface IReportPrinterBase<in TData>
    where TData : ReportPrinterDataBase
    {
        Task PrintAsync(TData data);
    }
}