using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Task;

namespace Telemart.Client.Data.Requests.Features.Task
{
    public sealed class QueryTasks : QueryEntitiesPagedRequestBase<TaskDto>
    {
        public QueryTasks(IFilteringItem filter)
            : base(filter, ApiResources.Tasks)
        {
        }
    }
}