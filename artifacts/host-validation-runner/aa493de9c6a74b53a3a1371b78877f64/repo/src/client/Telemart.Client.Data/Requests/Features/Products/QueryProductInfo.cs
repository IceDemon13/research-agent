using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class QueryProductInfo : QueryEntityRequestBase<ProductInfoDto>
    {
        public QueryProductInfo(int id, int? contractorId)
            : base(ApiResources.Products, id, "info")
        {
            if (contractorId.HasValue)
            {
                UrlParameters = new (string Name, object Value)[] { ("contractor", contractorId) };
            }
        }
    }
}