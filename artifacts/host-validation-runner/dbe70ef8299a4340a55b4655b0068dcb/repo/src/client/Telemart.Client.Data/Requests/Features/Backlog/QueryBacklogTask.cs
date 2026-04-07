using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog
{
    public sealed class QueryBacklogTask : QueryEntityRequestBase<BacklogTaskDto>
    {
        public QueryBacklogTask(int id)
            : base(ApiResources.BacklogTasks, id)
        {
        }
    }
}