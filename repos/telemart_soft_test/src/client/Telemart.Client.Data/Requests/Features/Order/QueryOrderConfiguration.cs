using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class QueryOrderConfiguration : QueryRequestBase<OrderConfigurationDto>
    {
        public QueryOrderConfiguration()
            : base(ApiResources.Orders, "configuration")
        {
        }
    }
}