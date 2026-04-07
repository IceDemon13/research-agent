using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Task;

namespace Telemart.Client.Data.Requests.Features.Task.Actions
{
    public sealed class ReopenTask : CallEntityActionRequestResultBase<TaskDto>
    {
        public ReopenTask(int id)
            : base(id, ApiResources.Tasks, "reopen")
        {
        }
    }
}