using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public sealed class UnlockBacklogTask : UnlockRequestBase<BacklogTaskDto>
    {
        public UnlockBacklogTask(int id, bool force = false)
            : base(force, ApiResources.BacklogTasks, id)
        {
        }
    }
}