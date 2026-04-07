using Telemart.Client.Reports.ReportBuilders.Base;

namespace Telemart.Client.Reports.ReportBuilders.AssemblyService.PassportReport
{
    public class AssemblyServicePassportReportPrinterData : ReportPrinterDataBase
    {
        public AssemblyServicePassportReportPrinterData(int assemblyServiceId, bool showPreview)
        : base(showPreview)
        {
            AssemblyServiceId = assemblyServiceId;
        }

        public int AssemblyServiceId { get; }
    }
}