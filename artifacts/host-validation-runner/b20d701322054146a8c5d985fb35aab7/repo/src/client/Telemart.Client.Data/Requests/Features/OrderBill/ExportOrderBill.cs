using System.Net.Http;

namespace Telemart.Client.Data.Requests.Features.OrderBill
{
    public class ExportOrderBill : RestClientGatewayRequestBase<object>
    {
        public ExportOrderBill(int billId)
            : base(HttpMethod.Get)
        {
            PathParameters = new object[] { ApiResources.OrderBills, billId, "bill_report" };
        }
    }
}