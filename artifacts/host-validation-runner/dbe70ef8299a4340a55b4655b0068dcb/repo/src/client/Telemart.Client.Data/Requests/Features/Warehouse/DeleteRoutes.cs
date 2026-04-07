using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Warehouse.Route;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public sealed class DeleteRoutes : CallActionWithBodyRequestResultBase<object, DeleteRoutesDto>
    {
        public DeleteRoutes(DeleteRoutesDto dto)
            : base(dto, ApiResources.Warehouses, "routes/delete_many")
        {
        }
    }
}