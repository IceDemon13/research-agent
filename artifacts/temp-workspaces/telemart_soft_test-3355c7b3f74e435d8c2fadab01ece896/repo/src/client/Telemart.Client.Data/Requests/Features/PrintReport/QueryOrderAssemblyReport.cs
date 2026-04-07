using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.PrintReport
{
    public sealed class QueryOrderAssemblyReport : QueryEntityRequestBase<OrderAssemblyReportDataDto>
    {
        public QueryOrderAssemblyReport(int orderId)
            : base(ApiResources.PrintReport, "order_assembly", orderId)
        {
        }
    }
}