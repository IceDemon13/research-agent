using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.HotlineCompetitor;

namespace Telemart.Client.Data.Requests.Features.HotlineCompetitor
{
    public class QueryHotlineCompetitors : QueryEntitiesRequestBase<HotlineCompetitorDto>
    {
        public QueryHotlineCompetitors()
            : base(ApiResources.HotlineCompetitors)
        {
        }
    }
}
