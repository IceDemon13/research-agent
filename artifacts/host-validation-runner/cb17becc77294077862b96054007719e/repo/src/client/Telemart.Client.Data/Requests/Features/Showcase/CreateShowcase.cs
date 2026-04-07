using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public class CreateShowcase : CreateEntityResultRequestBase<ShowcaseDto, CreateShowcase.ShowcaseCreateDto>
    {
        public CreateShowcase(int productId, int warehouseId, int capacity, bool active)
            : base(new ShowcaseCreateDto(productId, warehouseId, capacity, active), ApiResources.Showcases)
        {
        }

        public class ShowcaseCreateDto
        {
            public ShowcaseCreateDto(int productId, int warehouseId, int capacity, bool active)
            {
                ProductId = productId;
                WarehouseId = warehouseId;
                Capacity = capacity;
                Active = active;
            }

            [JsonProperty("product_id")]
            public int ProductId { get; set; }

            [JsonProperty("warehouse_id")]
            public int WarehouseId { get; set; }

            [JsonProperty("capacity")]
            public int Capacity { get; set; }

            [JsonProperty("active")]
            public bool Active { get; set; }
        }
    }
}
