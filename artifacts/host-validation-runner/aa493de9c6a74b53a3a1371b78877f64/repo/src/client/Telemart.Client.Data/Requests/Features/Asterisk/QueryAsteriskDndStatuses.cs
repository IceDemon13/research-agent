using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Asterisk;

namespace Telemart.Client.Data.Requests.Features.Asterisk
{
    public sealed class QueryAsteriskDndStatuses : QueryEntitiesRequestBase<AsteriskDndDto>
    {
        public QueryAsteriskDndStatuses()
            : base(ApiResources.Asterisk, "dnd_statuses")
        {
        }
    }
}