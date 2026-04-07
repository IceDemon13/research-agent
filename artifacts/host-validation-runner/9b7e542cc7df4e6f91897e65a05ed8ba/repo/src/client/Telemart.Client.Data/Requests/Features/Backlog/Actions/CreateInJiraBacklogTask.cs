using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public sealed class CreateInJiraBacklogTask : CallEntityActionWithBodyRequestResultBase<BacklogTaskDto, CreateInJiraBacklogDto>
    {
        public CreateInJiraBacklogTask(int id, CreateInJiraBacklogDto dto)
            : base(id, dto, ApiResources.BacklogTasks, "create_in_jira")
        {
        }
    }
}