using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Accessory;

namespace Telemart.Client.Data.Requests.Features.Accessory
{
    public class QueryAccessory : QueryEntityRequestBase<AccessoryDto>
    {
        public QueryAccessory(int id)
            : base(ApiResources.Accessories, id)
        {
        }
    }
}