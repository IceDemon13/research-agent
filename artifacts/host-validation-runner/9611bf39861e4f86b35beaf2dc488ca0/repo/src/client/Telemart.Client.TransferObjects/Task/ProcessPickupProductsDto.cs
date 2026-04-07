using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Task
{
    public sealed class ProcessPickupProductsDto
    {
        public ProcessPickupProductsDto(int taskId, PickupProductsDto productsDto)
        {
            TaskId = taskId;
            ProductsDto = productsDto;
        }

        [JsonProperty("task_id")]
        public int TaskId { get; set; }

        [JsonProperty("products_dto")]
        public PickupProductsDto ProductsDto { get; set; }
    }
}