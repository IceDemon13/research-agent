using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Logistics
{
    public sealed class QueryCourierDeliveries : QueryEntitiesRequestBase<CourierDeliveryDto>
    {
        public QueryCourierDeliveries()
        : base("logistics", "courier_deliveries")
        {
        }
    }
}