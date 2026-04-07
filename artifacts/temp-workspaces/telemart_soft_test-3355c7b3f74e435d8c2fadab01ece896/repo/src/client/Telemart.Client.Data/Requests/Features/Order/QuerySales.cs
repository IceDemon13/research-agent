using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class QuerySales : QueryEntitiesRequestBase<OrderSaleDto>
    {
        public QuerySales(string serialNumber, int? contractorId, int? productId)
            : base("sales")
        {
            UrlParameters = GetParameters(serialNumber, contractorId, productId);
        }

        private static IEnumerable<(string, object)> GetParameters(string serialNumber, int? contractorId, int? productId)
        {
            yield return ("sn", serialNumber);

            if (contractorId.HasValue)
            {
                yield return ("client_id", contractorId.Value);
            }

            if (productId.HasValue)
            {
                yield return ("product_id", productId.Value);
            }
        }
    }
}