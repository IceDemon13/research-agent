using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Accessory;

namespace Telemart.Client.Data.Requests.Features.Accessory
{
    public class QueryAccessories : QueryEntitiesRequestBase<AccessoryDto>
    {
        public QueryAccessories()
            : base(ApiResources.Accessories)
        {
        }
    }
}