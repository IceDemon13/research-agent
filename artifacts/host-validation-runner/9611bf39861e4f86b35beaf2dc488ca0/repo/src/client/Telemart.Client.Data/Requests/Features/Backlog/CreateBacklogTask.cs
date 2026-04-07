using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog
{
    public sealed class CreateBacklogTask : CreateEntityResultRequestBase<BacklogTaskDto, BacklogTaskCreateDto>
    {
        public CreateBacklogTask(BacklogTaskCreateDto dto)
            : base(dto, ApiResources.BacklogTasks)
        {
        }
    }
}