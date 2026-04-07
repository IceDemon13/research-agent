using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.CheckCompatibility
{
    public class ValidatedProductDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("validation_data")]
        public IReadOnlyCollection<ProductValidationRelationDto> ValidationData { get; init; }

        [JsonProperty("error_messages")]
        public string[] Messages { get; init; }
    }
}