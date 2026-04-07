using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.Requests.Features.Report.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Report
{
    public sealed class QueryReports : QueryEntitiesRequestBase<ReportSimpleDto>
    {
        public QueryReports()
            : base("reports")
        {
        }
    }
}