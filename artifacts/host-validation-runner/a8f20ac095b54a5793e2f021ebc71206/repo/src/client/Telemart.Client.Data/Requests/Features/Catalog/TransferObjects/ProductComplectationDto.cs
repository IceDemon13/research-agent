using System.Collections.Generic;

namespace Telemart.Client.Data.Requests.Features.Catalog.TransferObjects
{
    public class ProductComplectationDto
    {
        public int CategoryId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal? PriceUah { get; set; }

        public decimal? PriceUsd { get; set; }

        public int? Priority { get; set; }

        public IReadOnlyCollection<(int FeatureId, int FeatureValueId, string FeatureValue)> Features { get; set; }
    }
}