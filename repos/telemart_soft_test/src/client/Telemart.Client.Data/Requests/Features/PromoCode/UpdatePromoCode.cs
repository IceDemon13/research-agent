using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PromoCode;

namespace Telemart.Client.Data.Requests.Features.PromoCode
{
    public class UpdatePromoCode : UpdateEntityResultRequestBase<PromoCodeFullDto, PromoCodeSaveDto>
    {
        public UpdatePromoCode(PromoCodeSaveDto dto)
            : base(dto, ApiResources.PromoCodes, dto.Id)
        {
        }
    }
}
