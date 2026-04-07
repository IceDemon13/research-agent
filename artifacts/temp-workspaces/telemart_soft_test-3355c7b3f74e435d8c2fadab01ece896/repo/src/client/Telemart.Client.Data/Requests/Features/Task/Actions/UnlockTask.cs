using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Task;

namespace Telemart.Client.Data.Requests.Features.Task.Actions
{
    public sealed class UnlockTask : UnlockRequestBase<TaskDto>
    {
        public UnlockTask(int id, bool force = false)
            : base(force, ApiResources.Tasks, id)
        {
        }
    }
}