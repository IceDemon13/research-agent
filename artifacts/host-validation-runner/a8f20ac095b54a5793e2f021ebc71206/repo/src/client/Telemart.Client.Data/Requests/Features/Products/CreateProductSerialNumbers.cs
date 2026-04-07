using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public class CreateProductSerialNumbers : CreateEntityResultRequestBase<ProductSerialNumbersDto, CreateProductSerialNumbers.CreateProductSerialNumbersDto>
    {
        public CreateProductSerialNumbers(int productId, int count)
            : base(new CreateProductSerialNumbersDto(productId, count), ApiResources.Products, productId, "serial_numbers")
        {
        }

        public class CreateProductSerialNumbersDto
        {
            public CreateProductSerialNumbersDto(int productId, int count)
            {
                ProductId = productId;
                Count = count;
            }

            [JsonProperty("product_id")]
            public int ProductId { get; set; }

            [JsonProperty("count")]
            public int Count { get; set; }
        }
    }
}