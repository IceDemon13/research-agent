using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ProductCompatibility;

namespace Telemart.Client.Data.Requests.Features.ProductCompatibility
{
    public class QueryProductCompatibilities : QueryEntitiesRequestBase<ProductCompatibilityDto>
    {
        public QueryProductCompatibilities()
            : base(ApiResources.ProductCompatibilities)
        {
        }
    }
}
