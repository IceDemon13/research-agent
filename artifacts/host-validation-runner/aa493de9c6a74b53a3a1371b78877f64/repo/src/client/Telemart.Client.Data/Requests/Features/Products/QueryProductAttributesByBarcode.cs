using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class QueryProductAttributesByBarcode : QueryEntityRequestBase<ProductAttributesDto>
    {
        public QueryProductAttributesByBarcode(string barcode, bool includeSn)
            : base(ApiResources.ProductAttributes)
        {
            UrlParameters = new (string Name, object Value)[]
            {
                ("barcode", barcode),
                ("includeSn", includeSn)
            };
        }
    }
}