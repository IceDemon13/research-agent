using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Payments;

namespace Telemart.Client.Data.Requests.Features.Payments
{
    public class UpdatePayment : UpdateEntityResultRequestBase<PaymentDto, PaymentSaveDto>
    {
        public UpdatePayment(PaymentSaveDto dto)
            : base(dto, ApiResources.PaymentTypes, dto.Id)
        {
        }
    }
}
