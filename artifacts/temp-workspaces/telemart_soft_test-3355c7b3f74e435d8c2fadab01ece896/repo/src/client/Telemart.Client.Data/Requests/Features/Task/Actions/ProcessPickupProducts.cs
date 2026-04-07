using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Task;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Task.Actions
{
    public sealed class ProcessPickupProducts : CallEntityActionWithBodyRequestResultBase<TaskDto, ProcessPickupProductsDto>
    {
        public ProcessPickupProducts(ProcessPickupProductsDto dto)
            : base(dto.TaskId, dto, ApiResources.Tasks, "process_pickup_products")
        {
        }
    }
}