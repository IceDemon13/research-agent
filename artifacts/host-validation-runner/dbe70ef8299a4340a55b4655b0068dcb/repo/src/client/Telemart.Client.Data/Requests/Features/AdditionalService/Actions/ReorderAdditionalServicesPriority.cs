using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService.Actions
{
    public sealed class ReorderAdditionalServicesPriority : CallActionWithBodyRequestResultBase<object, ReorderAdditionalServicesPriority.ReorderAdditionalServicesPriorityDto>
    {
        public ReorderAdditionalServicesPriority(AdditionalServicePriorityDto[] priorities)
            : base(new ReorderAdditionalServicesPriorityDto(priorities), ApiResources.AdditionalServices, "reorder_priorities")
        {
        }

        public class ReorderAdditionalServicesPriorityDto
        {
            public ReorderAdditionalServicesPriorityDto(AdditionalServicePriorityDto[] priorities)
            {
                Priorities = priorities;
            }

            [JsonProperty("priorities")]
            public AdditionalServicePriorityDto[] Priorities { get; set; }
        }
    }
}