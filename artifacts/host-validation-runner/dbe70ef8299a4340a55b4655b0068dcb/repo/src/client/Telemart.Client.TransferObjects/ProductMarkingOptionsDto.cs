using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ProductMarkingOptionsDto : ICloneable
    {
        [JsonProperty("marker_manufacture")]
        public string MarkerManufacture { get; set; }

        [JsonProperty("marker_manufacture_address")]
        public string MarkerManufactureAddress { get; set; }

        [JsonProperty("feature_id")]
        public int? FeatureId { get; set; }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ProductMarkingOptionsDto Clone()
        {
            ProductMarkingOptionsDto productMarkingOptionsNew = new ProductMarkingOptionsDto
            {
                MarkerManufacture = MarkerManufacture,
                MarkerManufactureAddress = MarkerManufactureAddress,
                FeatureId = FeatureId
            };

            return productMarkingOptionsNew;
        }
    }
}