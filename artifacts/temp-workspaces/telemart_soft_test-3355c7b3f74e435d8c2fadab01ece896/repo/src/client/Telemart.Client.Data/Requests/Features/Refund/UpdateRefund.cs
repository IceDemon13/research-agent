using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Refund
{
    public sealed class UpdateRefund : UpdateEntityResultRequestBase<RefundDto, RefundSaveDto>
    {
        public UpdateRefund(int id, RefundSaveDto dto)
            : base(dto, ApiResources.Refunds, id)
        {
        }
    }
}
