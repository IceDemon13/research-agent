using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public sealed class RealizeBacklogTask : CallEntityActionRequestResultBase<BacklogTaskDto>
    {
        public RealizeBacklogTask(int id)
            : base(id, ApiResources.BacklogTasks, "realize")
        {
        }
    }
}