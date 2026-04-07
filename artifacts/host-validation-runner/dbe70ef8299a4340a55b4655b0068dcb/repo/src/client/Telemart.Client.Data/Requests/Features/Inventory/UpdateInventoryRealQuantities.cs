using System.Net;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Inventory
{
    public sealed class UpdateInventoryRealQuantities : UpdateEntityRequestBase<InventoryQuantitiesSaveDto, InventoryQuantitiesSaveDto>
    {
        public UpdateInventoryRealQuantities(InventoryQuantitiesSaveDto saveDto)
            : base(saveDto, ApiResources.Inventories, saveDto.InventoryId, "products")
        {
            SuccessStatusCode = HttpStatusCode.NoContent;
        }
    }
}