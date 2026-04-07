using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderPayment
{
    public sealed class UpdateOrderPayment : UpdateEntityResultRequestBase<OrderPaymentDto, OrderPaymentSaveDto>
    {
        public UpdateOrderPayment(int orderId, OrderPaymentSaveDto dto)
            : base(dto, ApiResources.Orders, orderId, "payments", dto.Id)
        {
        }
    }
}