using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public sealed class CancelBacklogTask : CallEntityActionRequestResultBase<BacklogTaskDto>
    {
        public CancelBacklogTask(int id)
            : base(id, ApiResources.BacklogTasks, "cancel")
        {
        }
    }
}