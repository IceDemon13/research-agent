using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public class QueryOrderCells : QueryEntitiesRequestBase<OrderCellDto>
    {
        public QueryOrderCells(int orderId)
            : base(ApiResources.Orders, orderId, ApiResources.Cells)
        {
        }
    }
}