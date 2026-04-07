using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Task;

namespace Telemart.Client.Data.Requests.Features.Task.Actions
{
    public sealed class CancelTask : CallEntityActionRequestResultBase<TaskDto>
    {
        public CancelTask(int id)
            : base(id, ApiResources.Tasks, "cancel")
        {
        }
    }
}
