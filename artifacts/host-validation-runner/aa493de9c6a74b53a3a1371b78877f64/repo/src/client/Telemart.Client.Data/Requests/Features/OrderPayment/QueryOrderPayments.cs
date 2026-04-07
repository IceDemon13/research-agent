using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderPayment
{
    public sealed class QueryOrderPayments : QueryEntitiesRequestBase<OrderPaymentDto>
    {
        public QueryOrderPayments(int orderId)
            : base(ApiResources.Orders, orderId, "payments")
        {
        }
    }
}