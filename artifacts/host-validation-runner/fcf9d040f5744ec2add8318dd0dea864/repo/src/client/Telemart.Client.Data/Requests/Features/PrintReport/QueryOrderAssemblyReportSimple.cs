using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.PrintReport
{
    public sealed class QueryOrderAssemblyReportSimple : QueryEntityRequestBase<OrderAssemblyReportSimpleDataDto>
    {
        public QueryOrderAssemblyReportSimple(int orderId)
            : base(ApiResources.PrintReport, "order_assembly_simple", orderId)
        {
        }
    }
}
