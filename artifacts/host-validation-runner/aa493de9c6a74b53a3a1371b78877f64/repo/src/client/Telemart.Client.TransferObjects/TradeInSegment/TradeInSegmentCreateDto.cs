using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeInSegment
{
    public class TradeInSegmentCreateDto
    {
        public TradeInSegmentCreateDto(
            string name,
            decimal? price,
            int categoryId,
            int employeeId,
            IReadOnlyCollection<TradeInSegmentCategoryFeatureSimpleDto> tradeInCategoryFeatures)
        {
            Name = name;
            Price = price;
            CategoryId = categoryId;
            EmployeeId = employeeId;
            TradeInCategoryFeatures = tradeInCategoryFeatures;
        }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("price")]
        public decimal? Price { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; init; }

        [JsonProperty("trade_in_category_features")]
        public IReadOnlyCollection<TradeInSegmentCategoryFeatureSimpleDto> TradeInCategoryFeatures { get; init; }
    }
}