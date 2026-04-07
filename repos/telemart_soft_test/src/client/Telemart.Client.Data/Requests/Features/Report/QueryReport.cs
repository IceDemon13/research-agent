using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.Requests.Features.Report.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Report
{
    public sealed class QueryReport : QueryEntityRequestBase<ReportDto>
    {
        public QueryReport(int id)
            : base("reports", id)
        {
        }
    }
}