using DevExpress.XtraReports;
using Telemart.Client.Common.Settings.Printing;

namespace Telemart.Client.Common.ReportFactory
{
    public class BarcodeReportFactoryResult
    {
        public BarcodeReportFactoryResult(IReport report, PrinterSettingsInfo printer)
        {
            Report = report;
            Printer = printer;
        }

        public IReport Report { get; }

        public PrinterSettingsInfo Printer { get; }
    }
}
