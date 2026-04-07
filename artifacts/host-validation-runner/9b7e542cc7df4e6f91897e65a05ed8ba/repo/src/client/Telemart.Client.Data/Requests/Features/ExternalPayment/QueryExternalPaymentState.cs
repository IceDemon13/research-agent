using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.ExternalPayment
{
    public sealed class QueryExternalPaymentState : QueryEntityRequestBase<Result<ContractorPaymentStateDto>>
    {
        public QueryExternalPaymentState(int externalPaymentId)
            : base(ApiResources.ExternalPayments, externalPaymentId, "state")
        {
        }
    }
}