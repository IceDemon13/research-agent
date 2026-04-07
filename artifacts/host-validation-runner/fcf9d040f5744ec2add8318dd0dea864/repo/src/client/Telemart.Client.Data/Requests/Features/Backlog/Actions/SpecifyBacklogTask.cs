using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public sealed class SpecifyBacklogTask : CallEntityActionRequestResultBase<BacklogTaskDto>
    {
        public SpecifyBacklogTask(int id)
            : base(id, ApiResources.BacklogTasks, "specify")
        {
        }
    }
}