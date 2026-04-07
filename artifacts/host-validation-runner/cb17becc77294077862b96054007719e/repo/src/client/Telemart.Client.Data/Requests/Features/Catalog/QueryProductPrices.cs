using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Prices;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class QueryProductPrices : CallActionWithBodyRequestBase<List<ProductPriceDto>, QueryProductPrices.ProductPriceQueryDto>
    {
        public QueryProductPrices(
            int categoryId,
            string name,
            IReadOnlyCollection<int> contractors,
            IReadOnlyCollection<int> avails,
            IReadOnlyCollection<int> filters,
            IReadOnlyCollection<int> labels,
            IReadOnlyCollection<int> warehouses)
            : base(
                new ProductPriceQueryDto(categoryId, name, contractors, filters, labels, warehouses, avails),
                "prices",
                "query")
        {
        }

        public sealed class ProductPriceQueryDto
        {
            public ProductPriceQueryDto(
                int categoryId,
                string name,
                IReadOnlyCollection<int> contractors,
                IReadOnlyCollection<int> filterIds,
                IReadOnlyCollection<int> labelIds,
                IReadOnlyCollection<int> warehouseIds,
                IReadOnlyCollection<int> availIds)
            {
                CategoryId = categoryId;
                Name = name;
                Contractors = contractors;
                FilterIds = filterIds;
                LabelIds = labelIds;
                WarehouseIds = warehouseIds;
                AvailIds = availIds;
            }

            [JsonProperty("category_id")]
            public int CategoryId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("contractors")]
            public IReadOnlyCollection<int> Contractors { get; set; }

            //// --- filter parameters --- ////

            [JsonProperty("filters")]
            public IReadOnlyCollection<int> FilterIds { get; set; }

            [JsonProperty("labels")]
            public IReadOnlyCollection<int> LabelIds { get; set; }

            [JsonProperty("avail_warehouses")]
            public IReadOnlyCollection<int> WarehouseIds { get; set; }

            [JsonProperty("avails")]
            public IReadOnlyCollection<int> AvailIds { get; set; }
        }
    }
}