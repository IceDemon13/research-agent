using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CategoryMoveDto
    {
        public CategoryMoveDto(int categoryId, int targetCategoryId)
        {
            Id = categoryId;
            ParentId = targetCategoryId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parent_id")]
        public int ParentId { get; set; }
    }
}