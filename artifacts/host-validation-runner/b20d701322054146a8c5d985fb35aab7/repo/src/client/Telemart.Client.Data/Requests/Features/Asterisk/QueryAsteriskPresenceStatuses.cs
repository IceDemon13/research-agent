using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Asterisk;

namespace Telemart.Client.Data.Requests.Features.Asterisk
{
    public sealed class QueryAsteriskPresenceStatuses : QueryEntitiesRequestBase<AsteriskPresenceDto>
    {
        public QueryAsteriskPresenceStatuses()
            : base(ApiResources.Asterisk, "presence_statuses")
        {
        }
    }
}