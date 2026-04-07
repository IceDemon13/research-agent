using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ParserContractorPriceSearchDto
    {
        public ParserContractorPriceSearchDto(int? contractorId, int[] productIds)
        {
            ContractorId = contractorId;
            ProductIds = productIds;
        }

        public ParserContractorPriceSearchDto()
        {
        }

        [JsonProperty("contractor_id")]
        public int? ContractorId { get; set; }

        [JsonProperty("product_ids")]
        public int[] ProductIds { get; set; }
    }
}