using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public sealed class LockBacklogTask : LockRequestBase<BacklogTaskDto>
    {
        public LockBacklogTask(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.BacklogTasks, id)
        {
        }
    }
}