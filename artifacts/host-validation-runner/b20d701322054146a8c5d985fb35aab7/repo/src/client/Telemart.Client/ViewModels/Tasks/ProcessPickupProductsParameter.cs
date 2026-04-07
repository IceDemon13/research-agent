using System.Collections.Generic;
using Telemart.Client.TransferObjects.Task;

namespace Telemart.Client.ViewModels.Tasks
{
    public sealed class ProcessPickupProductsParameter
    {
        public ProcessPickupProductsParameter(IReadOnlyCollection<PickupProductDto> products, int taskId)
        {
            Products = products;
            TaskId = taskId;
        }

        public int TaskId { get; }

        public IReadOnlyCollection<PickupProductDto> Products { get; }
    }
}