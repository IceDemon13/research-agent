using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PromoCode;

namespace Telemart.Client.Data.Requests.Features.PromoCode
{
    public class CreatePromoCode : CreateEntityResultRequestBase<PromoCodeFullDto, PromoCodeSaveDto>
    {
        public CreatePromoCode(PromoCodeSaveDto dto)
            : base(dto, ApiResources.PromoCodes)
        {
        }
    }
}
