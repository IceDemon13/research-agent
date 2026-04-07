using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog
{
    public sealed class QueryBacklogTasks : QueryEntitiesPagedRequestBase<BacklogTaskDto>
    {
        public QueryBacklogTasks(IFilteringItem filter)
            : base(filter, ApiResources.BacklogTasks)
        {
        }
    }
}