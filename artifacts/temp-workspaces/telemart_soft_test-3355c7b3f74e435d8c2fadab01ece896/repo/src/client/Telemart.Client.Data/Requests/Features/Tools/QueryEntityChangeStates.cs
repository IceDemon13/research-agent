using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Tools
{
    public sealed class QueryEntityChangeStates : QueryEntitiesRequestBase<EntityChangeStateDto>
    {
        public QueryEntityChangeStates()
            : base("techsupport/entity_change_state")
        {
        }
    }
}