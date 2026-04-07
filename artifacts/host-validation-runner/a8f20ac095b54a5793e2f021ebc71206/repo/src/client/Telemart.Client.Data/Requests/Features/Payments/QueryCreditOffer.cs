using Telemart.Client.Data.Requests.Base;
using Telemart.Common.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Payments
{
    public class QueryCreditOffer : QueryEntityRequestBase<CreditOfferDto>
    {
        public QueryCreditOffer(int id)
            : base(ApiResources.PaymentTypes, ApiResources.CreditOffers, id)
        {
        }
    }
}