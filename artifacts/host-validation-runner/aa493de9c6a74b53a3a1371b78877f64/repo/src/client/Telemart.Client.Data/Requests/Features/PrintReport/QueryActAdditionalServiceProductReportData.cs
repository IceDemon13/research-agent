using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.PrintReport
{
    public sealed class QueryActAdditionalServiceProductReportData : QueryEntityRequestBase<AdditionalServiceProductClientProductReportDataDto>
    {
        public QueryActAdditionalServiceProductReportData(int orderId)
            : base(ApiResources.PrintReport, "act_additional_service_product", orderId)
        {
        }
    }
}