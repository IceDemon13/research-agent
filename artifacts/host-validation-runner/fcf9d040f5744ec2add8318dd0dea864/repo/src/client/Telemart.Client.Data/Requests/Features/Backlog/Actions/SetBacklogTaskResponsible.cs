using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public sealed class SetBacklogTaskResponsible : UpdateEntityResultRequestBase<BacklogTaskDto, BacklogTaskResponsibleDto>
    {
        public SetBacklogTaskResponsible(int taskId, int? responsibleId)
            : base(new BacklogTaskResponsibleDto(taskId, responsibleId), ApiResources.BacklogTasks, taskId, "responsible")
        {
        }
    }
}