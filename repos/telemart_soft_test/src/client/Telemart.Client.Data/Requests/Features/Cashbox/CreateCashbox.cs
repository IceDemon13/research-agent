using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Data.Requests.Features.Cashbox
{
    public class CreateCashbox : CreateEntityResultRequestBase<CashboxDto, CashboxSaveDto>
    {
        public CreateCashbox(CashboxSaveDto dto)
            : base(dto, ApiResources.Cashboxes)
        {
        }
    }
}