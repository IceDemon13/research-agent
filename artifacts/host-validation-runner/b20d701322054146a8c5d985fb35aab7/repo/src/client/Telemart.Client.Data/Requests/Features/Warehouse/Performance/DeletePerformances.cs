using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Performance
{
    public sealed class DeletePerformances : CallActionWithBodyRequestResultBase<object, DeletePerfomancesDto>
    {
        public DeletePerformances(DeletePerfomancesDto dto)
            : base(dto, ApiResources.Warehouses, "performances/delete_many")
        {
        }
    }
}