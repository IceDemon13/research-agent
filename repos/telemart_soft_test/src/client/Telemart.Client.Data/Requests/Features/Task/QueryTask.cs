using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Task;

namespace Telemart.Client.Data.Requests.Features.Task
{
    public sealed class QueryTask : QueryEntityRequestBase<TaskDto>
    {
        public QueryTask(object id)
            : base(ApiResources.Tasks, id)
        {
        }
    }
}