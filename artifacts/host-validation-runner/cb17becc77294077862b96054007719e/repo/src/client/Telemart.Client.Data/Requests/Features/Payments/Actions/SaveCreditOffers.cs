using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Payments.Actions
{
    public sealed class SaveCreditOffers : CallActionWithBodyRequestResultBase<object, CreditOffersSaveDto>
    {
        public SaveCreditOffers(CreditOffersSaveDto dto)
            : base(dto, $"{ApiResources.PaymentTypes}/{ApiResources.CreditOffers}", "save")
        {
        }
    }
}