using DevExpress.XtraReports;

namespace Telemart.Client.Helpers
{
    public interface IReportPrintHelper
    {
        void Print(IReport report, string printerName, string paperSource, bool showPreview, short copies = 1);
    }
}
