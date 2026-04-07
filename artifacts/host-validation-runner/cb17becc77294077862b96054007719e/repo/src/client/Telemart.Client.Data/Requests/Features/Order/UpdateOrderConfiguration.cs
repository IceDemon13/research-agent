using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public class UpdateOrderConfiguration : UpdateEntityRequestBase<OrderConfigurationDto, OrderConfigurationDto>
    {
        public UpdateOrderConfiguration(OrderConfigurationDto dto)
            : base(dto, ApiResources.Orders, "configuration")
        {
        }
    }
}