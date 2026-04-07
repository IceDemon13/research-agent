using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class CalculateOrderProductWarranty : CallActionWithBodyRequestBase<CalculateOrderProductWarrantyEndResponseDto, CalculateOrderProductWarrantyEndDto>
    {
        public CalculateOrderProductWarranty(CalculateOrderProductWarrantyEndDto dto)
            : base(dto, "warranties", "calculate_order_product_warranty")
        {
        }
    }
}