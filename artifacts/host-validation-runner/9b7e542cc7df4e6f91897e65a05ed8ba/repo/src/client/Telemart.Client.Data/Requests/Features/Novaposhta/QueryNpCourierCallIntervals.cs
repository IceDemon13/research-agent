using System;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public sealed class QueryNpCourierCallIntervals : QueryEntitiesRequestBase<NpCourierCallIntervalDto>
    {
        public QueryNpCourierCallIntervals(int warehouseId, DateTime? dateTime = null)
            : base(ApiResources.Novaposhta, "courier_call/intervals")
        {
            UrlParameters = new (string, object)[] { ("warehouse_id", warehouseId), ("date", dateTime) };
        }
    }
}
