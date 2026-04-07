using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Task;

namespace Telemart.Client.Data.Requests.Features.Task.Actions
{
    public sealed class StartTask : CallEntityActionRequestResultBase<TaskDto>
    {
        public StartTask(int id)
            : base(id, ApiResources.Tasks, "start")
        {
        }
    }
}