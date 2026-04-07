using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Area
{
    public sealed class QueryAreas : QueryEntitiesRequestBase<AreaDto>
    {
        public QueryAreas()
            : base($"{ApiResources.Areas}")
        {
        }
    }
}