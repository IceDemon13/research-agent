using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Area
{
    public sealed class QueryArea : QueryEntityRequestBase<AreaDto>
    {
        public QueryArea(object id)
            : base(ApiResources.Areas, id)
        {
        }
    }
}