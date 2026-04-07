using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public class QueryAverageShowcaseQuantityLocation : QueryEntityRequestBase<string>
    {
        public QueryAverageShowcaseQuantityLocation()
            : base(ApiResources.Settings, "average_showcase_quantity_location")
        {
        }
    }
}