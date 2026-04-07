using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ProductCompatibility;

namespace Telemart.Client.Data.Requests.Features.ProductCompatibility
{
    public class QueryProductCompatibility : QueryEntityRequestBase<ProductCompatibilityDto>
    {
        public QueryProductCompatibility(object id)
            : base(ApiResources.ProductCompatibilities, id)
        {
        }
    }
}