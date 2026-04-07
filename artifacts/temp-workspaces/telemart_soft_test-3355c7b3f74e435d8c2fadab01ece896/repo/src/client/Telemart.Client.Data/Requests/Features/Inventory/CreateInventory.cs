using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Inventory
{
    public sealed class CreateInventory : CreateEntityRequestBase<InventoryDto, InventoryCreateDto>
    {
        public CreateInventory(InventoryCreateDto dto)
            : base(dto, ApiResources.Inventories)
        {
        }
    }
}
