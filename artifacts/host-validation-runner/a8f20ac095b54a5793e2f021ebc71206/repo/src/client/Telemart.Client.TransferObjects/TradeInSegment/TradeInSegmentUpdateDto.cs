using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeInSegment
{
    public sealed class TradeInSegmentUpdateDto
    {
        public TradeInSegmentUpdateDto(
            int id,
            string name,
            decimal? price,
            int employeeId,
            IReadOnlyCollection<TradeInSegmentCategoryFeatureSimpleDto> tradeInCategoryFeatures)
        {
            Id = id;
            Name = name;
            Price = price;
            EmployeeId = employeeId;
            TradeInCategoryFeatures = tradeInCategoryFeatures;
        }

        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("price")]
        public decimal? Price { get; init; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; init; }

        [JsonProperty("trade_in_category_features")]
        public IReadOnlyCollection<TradeInSegmentCategoryFeatureSimpleDto> TradeInCategoryFeatures { get; init; }
    }
}
