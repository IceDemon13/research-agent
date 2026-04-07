using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Task;

namespace Telemart.Client.Data.Requests.Features.Task.Actions
{
    public sealed class LockTask : LockRequestBase<TaskDto>
    {
        public LockTask(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Tasks, id)
        {
        }
    }
}