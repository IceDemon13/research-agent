using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Entity
{
    public sealed class QueryEntities : QueryEntitiesRequestBase<EntityDto>
    {
        public QueryEntities()
            : base(ApiResources.Entities)
        {
        }
    }
}