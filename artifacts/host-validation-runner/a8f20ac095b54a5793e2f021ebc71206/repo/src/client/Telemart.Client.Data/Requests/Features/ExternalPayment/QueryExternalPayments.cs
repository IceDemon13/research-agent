using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ExternalPayment
{
    public sealed class QueryExternalPayments : QueryEntitiesRequestBase<ExternalPaymentDto>
    {
        public QueryExternalPayments(int orderId)
            : base(ApiResources.Orders, orderId, "external_payments")
        {
        }
    }
}