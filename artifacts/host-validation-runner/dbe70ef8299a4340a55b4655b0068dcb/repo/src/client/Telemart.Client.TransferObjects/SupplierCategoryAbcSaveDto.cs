using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class SupplierCategoryAbcSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("abc_id")]
        public int AbcId { get; set; }
    }
}