using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Payments;

namespace Telemart.Client.Data.Requests.Features.Payments
{
    public class QueryPayment : QueryEntityRequestBase<PaymentDto>
    {
        public QueryPayment(int id)
            : base(ApiResources.PaymentTypes, id)
        {
        }
    }
}