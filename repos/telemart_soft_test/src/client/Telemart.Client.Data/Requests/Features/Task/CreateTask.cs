using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Task;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Task
{
    public sealed class CreateTask : CreateEntityRequestBase<Result<TaskDto>, TaskCreateDto>
    {
        public CreateTask(TaskCreateDto dto)
            : base(dto, ApiResources.Tasks)
        {
        }
    }
}