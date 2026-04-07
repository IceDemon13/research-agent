using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public sealed class CompleteBacklogTask : CallEntityActionRequestResultBase<BacklogTaskDto>
    {
        public CompleteBacklogTask(int id)
            : base(id, ApiResources.BacklogTasks, "complete")
        {
        }
    }
}