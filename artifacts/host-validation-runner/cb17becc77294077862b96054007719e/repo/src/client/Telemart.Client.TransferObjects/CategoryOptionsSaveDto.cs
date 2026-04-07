using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CategoryOptionsSaveDto
    {
        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("options")]
        public IReadOnlyCollection<CategoryOptionSaveDto> Options { get; set; }
    }
}