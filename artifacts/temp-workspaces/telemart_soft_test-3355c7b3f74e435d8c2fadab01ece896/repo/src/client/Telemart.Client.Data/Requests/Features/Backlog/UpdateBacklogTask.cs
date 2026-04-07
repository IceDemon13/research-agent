using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog
{
    public sealed class UpdateBacklogTask : UpdateEntityResultRequestBase<BacklogTaskDto, BacklogTaskSaveDto>
    {
        public UpdateBacklogTask(int id, BacklogTaskSaveDto dto)
            : base(dto, ApiResources.BacklogTasks, id)
        {
        }
    }
}