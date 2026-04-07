using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CategoryReorderChildrenDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("children_positions")]
        public IReadOnlyCollection<CategoryPositionDto> ChildrenPositions { get; set; }
    }
}