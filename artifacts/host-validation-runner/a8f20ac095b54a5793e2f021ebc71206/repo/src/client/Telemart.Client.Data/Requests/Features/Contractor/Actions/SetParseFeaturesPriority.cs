using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Contractor.Actions
{
    public sealed class SetParseFeaturesPriority : UpdateEntityResultRequestBase<object, SetParseFeaturesPriority.SetParseFeaturesPriorityDto>
    {
        public SetParseFeaturesPriority(int contractorId, int priority)
            : base(new SetParseFeaturesPriorityDto(priority), ApiResources.Contractors, contractorId, "parse_features_priority")
        {
        }

        public sealed class SetParseFeaturesPriorityDto
        {
            public SetParseFeaturesPriorityDto(int priority)
            {
                Priority = priority;
            }

            [JsonProperty("priority")]
            public int Priority { get; set; }
        }
    }
}