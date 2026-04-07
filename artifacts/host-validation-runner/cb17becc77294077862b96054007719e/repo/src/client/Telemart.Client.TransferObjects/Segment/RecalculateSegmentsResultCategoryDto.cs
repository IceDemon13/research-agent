using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Segment
{
    public class RecalculateSegmentsResultCategoryDto
    {
        [JsonProperty("parent_category_id")]
        public int ParentCategoryId { get; set; }

        [JsonProperty("parent_category_name")]
        public string ParentCategoryName { get; set; }

        [JsonProperty("calculated_products_quantity")]
        public int CalculatedProductsQuantity { get; set; }

        [JsonProperty("total_category_products_quantity")]
        public int TotalCategoryProductsQuantity { get; set; }

        [JsonProperty("products_quantity_with_not_filled_features_from_parent_category_segments")]
        public int ProductsQuantityWithNotFilledFeaturesFromParentCategorySegments { get; set; }

        [JsonProperty("segment_id")]
        public int SegmentId { get; set; }

        [JsonProperty("segment_name")]
        public string SegmentName { get; set; }
    }
}