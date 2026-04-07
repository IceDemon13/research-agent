using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class CreatePresaleOrder : CreateEntityResultRequestBase<OrderCreatePresaleResponse, OrderCreatePresaleDto>
    {
        public CreatePresaleOrder(OrderCreatePresaleDto dto)
            : base(dto, $"{ApiResources.Orders}/actions/create_presale")
        {
        }
    }
}