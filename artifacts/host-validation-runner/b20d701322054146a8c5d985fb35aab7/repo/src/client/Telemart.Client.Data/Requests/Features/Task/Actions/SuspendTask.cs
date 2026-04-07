using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Task;

namespace Telemart.Client.Data.Requests.Features.Task.Actions
{
    public sealed class SuspendTask : CallEntityActionRequestResultBase<TaskDto>
    {
        public SuspendTask(int id)
            : base(id, ApiResources.Tasks, "suspend")
        {
        }
    }
}