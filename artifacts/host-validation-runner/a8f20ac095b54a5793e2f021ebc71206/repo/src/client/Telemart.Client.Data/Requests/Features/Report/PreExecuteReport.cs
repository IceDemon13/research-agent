using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.Data.Requests.Features.Report.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Report
{
    public sealed class PreExecuteReport : CallEntityActionRequestBase<ReportParamsDto>
    {
        public PreExecuteReport(int reportId)
            : base(reportId, "reports", "pre-execute")
        {
        }
    }
}