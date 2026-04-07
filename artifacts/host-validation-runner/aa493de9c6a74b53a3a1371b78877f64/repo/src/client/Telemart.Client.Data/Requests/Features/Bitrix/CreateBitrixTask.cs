using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Bitrix
{
    public class CreateBitrixTask : CreateEntityResultRequestBase<object, CreateBitrixTask.BitrixTaskCreateDto>
    {
        public CreateBitrixTask(int bitrixId, int[] categoryIds, int priorityId)
            : base(new BitrixTaskCreateDto(bitrixId, categoryIds, priorityId), ApiResources.BitrixTasks)
        {
        }

        public class BitrixTaskCreateDto
        {
            public BitrixTaskCreateDto(int bitrixId, int[] categoryIds, int priorityId)
            {
                BitrixId = bitrixId;
                CategoryIds = categoryIds;
                PriorityId = priorityId;
            }

            [JsonProperty("bitrix_id")]
            public int BitrixId { get; set; }

            [JsonProperty("category_ids")]
            public int[] CategoryIds { get; set; }

            [JsonProperty("priority_id")]
            public int PriorityId { get; set; }
        }
    }
}