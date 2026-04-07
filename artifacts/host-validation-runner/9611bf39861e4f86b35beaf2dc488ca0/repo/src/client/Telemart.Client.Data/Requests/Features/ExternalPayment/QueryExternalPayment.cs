using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ExternalPayment
{
    public sealed class QueryExternalPayment : QueryEntityRequestBase<ExternalPaymentDto>
    {
        public QueryExternalPayment(int id)
            : base(ApiResources.ExternalPayments, id)
        {
        }
    }
}