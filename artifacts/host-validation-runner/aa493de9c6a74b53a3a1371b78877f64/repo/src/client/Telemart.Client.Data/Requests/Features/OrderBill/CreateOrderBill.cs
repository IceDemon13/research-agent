using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderBill
{
    public sealed class CreateOrderBill : CreateEntityResultRequestBase<OrderBillDto, CreateOrderBillDto>
    {
        public CreateOrderBill(CreateOrderBillDto dto)
            : base(dto, ApiResources.OrderBills)
        {
        }
    }
}
