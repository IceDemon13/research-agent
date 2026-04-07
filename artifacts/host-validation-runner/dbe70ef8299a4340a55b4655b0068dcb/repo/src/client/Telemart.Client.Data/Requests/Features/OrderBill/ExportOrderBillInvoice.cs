using System.Net.Http;

namespace Telemart.Client.Data.Requests.Features.OrderBill
{
    public class ExportOrderBillInvoice : RestClientGatewayRequestBase<object>
    {
        public ExportOrderBillInvoice(int billId)
            : base(HttpMethod.Get)
        {
            PathParameters = new object[] { ApiResources.OrderBills, billId, "invoice_report" };
        }
    }
}