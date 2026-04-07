using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Data.Requests.Features.Cashbox
{
    public class UpdateCashbox : UpdateEntityResultRequestBase<CashboxDto, CashboxSaveDto>
    {
        public UpdateCashbox(int id, CashboxSaveDto dto)
            : base(dto, ApiResources.Cashboxes, id)
        {
        }
    }
}