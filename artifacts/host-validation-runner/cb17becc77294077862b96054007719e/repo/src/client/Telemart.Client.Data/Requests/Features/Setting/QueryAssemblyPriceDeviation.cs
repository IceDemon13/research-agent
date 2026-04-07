using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public class QueryAssemblyPriceDeviation : QueryEntityRequestBase<string>
    {
        public QueryAssemblyPriceDeviation()
            : base(ApiResources.Settings, "assembly_price_deviation")
        {
        }
    }
}