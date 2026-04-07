using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using static Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions.UpdateAdditionalServiceProductWarehouse;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions
{
    public sealed class UpdateAdditionalServiceProductWarehouse : CallEntityActionWithBodyRequestResultBase<AdditionalServiceProductDto, UpdateAdditionalServiceProductWarehouseDto>
    {
        public UpdateAdditionalServiceProductWarehouse(int additionalServiceProductId, int warehouseId)
            : base(additionalServiceProductId, new UpdateAdditionalServiceProductWarehouseDto(additionalServiceProductId, warehouseId), ApiResources.AdditionalServicesProducts, "update_warehouse")
        {
        }

        public class UpdateAdditionalServiceProductWarehouseDto
        {
            public UpdateAdditionalServiceProductWarehouseDto(int id, int warehouseId)
            {
                Id = id;
                WarehouseId = warehouseId;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("warehouse_id")]
            public int WarehouseId { get; set; }
        }
    }
}