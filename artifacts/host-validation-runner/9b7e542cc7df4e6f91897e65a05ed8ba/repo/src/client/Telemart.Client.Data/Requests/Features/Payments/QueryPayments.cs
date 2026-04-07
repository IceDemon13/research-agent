using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Payments;

namespace Telemart.Client.Data.Requests.Features.Payments
{
    public sealed class QueryPayments : QueryEntitiesRequestBase<PaymentDto>
    {
        public QueryPayments()
            : base(ApiResources.PaymentTypes)
        {
        }
    }
}
