using System.Net.Http;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    // TODO: Change request to action
    public sealed class ConvertProductPrice : RestClientGatewayRequestBase<ProductPriceConvertResponse>
    {
        public ConvertProductPrice(int productId, int fromCurrencyId, int toCurrencyId, decimal value)
            : base(HttpMethod.Post)
        {
            PathParameters = new object[] { ApiResources.Products, productId, "price" };

            Body = new ProductPriceConvertRequest(
                fromCurrencyId,
                toCurrencyId,
                value);
        }
    }
}