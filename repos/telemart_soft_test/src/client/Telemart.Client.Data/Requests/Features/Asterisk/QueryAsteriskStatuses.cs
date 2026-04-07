using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Asterisk;

namespace Telemart.Client.Data.Requests.Features.Asterisk
{
    public sealed class QueryAsteriskStatuses : QueryEntitiesRequestBase<AsteriskStatusDto>
    {
        public QueryAsteriskStatuses()
            : base(ApiResources.Asterisk, "statuses")
        {
        }
    }
}