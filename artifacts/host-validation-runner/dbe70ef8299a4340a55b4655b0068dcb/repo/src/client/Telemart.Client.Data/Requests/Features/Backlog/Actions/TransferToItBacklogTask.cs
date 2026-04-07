using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public sealed class TransferToItBacklogTask : CallEntityActionRequestResultBase<BacklogTaskDto>
    {
        public TransferToItBacklogTask(int id)
            : base(id, ApiResources.BacklogTasks, "transfer_to_it")
        {
        }
    }
}