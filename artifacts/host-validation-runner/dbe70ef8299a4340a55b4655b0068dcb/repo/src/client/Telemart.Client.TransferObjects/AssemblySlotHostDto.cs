using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssemblySlotHostDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }

        [JsonProperty("validation_message_template_ru")]
        public string ValidationMessageTemplateRu { get; set; }

        [JsonProperty("validation_message_template_ukr")]
        public string ValidationMessageTemplateUkr { get; set; }

        [JsonProperty("validation_message_template_en")]
        public string ValidationMessageTemplateEn { get; set; }

        [JsonProperty("combine")]
        public bool Combine { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("consumers")]
        public IReadOnlyCollection<AssemblySlotConsumerDto> Consumers { get; set; }
    }
}