using System.Net.Http;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    // TODO: Change method to POST
    public sealed class SetOrderProductNotInStock : CallEntityActionRequestResultBase<OrderProductDto>
    {
        public SetOrderProductNotInStock(int id, int orderProductId)
            : base(orderProductId, $"{ApiResources.Orders}/{id}/{ApiResources.Products}", "not_in_stock")
        {
            Method = HttpMethod.Put;
        }
    }
}