using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Bitrix;

namespace Telemart.Client.Data.Requests.Features.Bitrix
{
    public class UpdateBitrixTask : UpdateEntityResultRequestBase<CategorizedBitrixTaskDto, UpdateBitrixTask.BitrixTaskSaveDto>
    {
        public UpdateBitrixTask(int id, int priorityId, int[] categoryIds)
            : base(new BitrixTaskSaveDto(id, priorityId, categoryIds), ApiResources.BitrixTasks, id)
        {
        }

        public class BitrixTaskSaveDto
        {
            public BitrixTaskSaveDto(int id, int priorityId, int[] categoryIds)
            {
                Id = id;
                PriorityId = priorityId;
                CategoryIds = categoryIds;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("priority_id")]
            public int PriorityId { get; set; }

            [JsonProperty("category_ids")]
            public int[] CategoryIds { get; set; }
        }
    }
}