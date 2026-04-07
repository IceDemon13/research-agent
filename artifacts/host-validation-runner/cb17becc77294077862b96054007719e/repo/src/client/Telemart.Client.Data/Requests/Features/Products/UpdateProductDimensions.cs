using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public class UpdateProductDimensions : UpdateEntityResultRequestBase<ProductDimensionsDto, UpdateProductDimensions.ProductDimensionsSaveDto>
    {
        public UpdateProductDimensions(int productId, int width, int height, int depth, double weight)
            : base(new ProductDimensionsSaveDto(productId, width, height, depth, weight), ApiResources.Products, productId, "dimensions")
        {
        }

        public class ProductDimensionsSaveDto
        {
            public ProductDimensionsSaveDto(int id, int width, int height, int depth, double weight)
            {
                Id = id;
                Width = width;
                Height = height;
                Depth = depth;
                Weight = weight;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("width")]
            public int Width { get; set; }

            [JsonProperty("height")]
            public int Height { get; set; }

            [JsonProperty("depth")]
            public int Depth { get; set; }

            [JsonProperty("weight")]
            public double Weight { get; set; }
        }
    }
}