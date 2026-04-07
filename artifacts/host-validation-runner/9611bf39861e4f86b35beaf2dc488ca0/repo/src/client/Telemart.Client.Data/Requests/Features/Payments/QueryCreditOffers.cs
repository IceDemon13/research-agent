using Telemart.Client.Data.Requests.Base;
using Telemart.Common.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Payments
{
    public sealed class QueryCreditOffers : QueryEntitiesRequestBase<CreditOfferDto>
    {
        public QueryCreditOffers()
            : base($"{ApiResources.PaymentTypes}/{ApiResources.CreditOffers}")
        {
        }
    }
}