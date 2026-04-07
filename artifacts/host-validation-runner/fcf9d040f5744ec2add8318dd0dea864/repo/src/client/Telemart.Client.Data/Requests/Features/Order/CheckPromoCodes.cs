using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.PromoCode;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class CheckPromoCodes : CallActionWithBodyRequestBase<CheckPromoCodesResponse, CheckPromoCodesRequest>
    {
        public CheckPromoCodes(CheckPromoCodesRequest request)
            : base(request, ApiResources.PromoCodes, "check")
        {
        }
    }
}