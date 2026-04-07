using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public sealed class AddToPlanBacklogTask : CallEntityActionWithBodyRequestResultBase<BacklogTaskDto, AddToPlanBacklogTaskDto>
    {
        public AddToPlanBacklogTask(int id, AddToPlanBacklogTaskDto dto)
            : base(id, dto, ApiResources.BacklogTasks, "add_to_plan")
        {
        }
    }
}