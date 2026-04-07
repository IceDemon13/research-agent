using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.Requests.Features.ReportLayout.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Report
{
    public sealed class QueryReportLayouts : QueryEntitiesRequestBase<ReportLayoutDto>
    {
        public QueryReportLayouts(int reportId)
            : base("reports", reportId, "layouts")
        {
        }
    }
}