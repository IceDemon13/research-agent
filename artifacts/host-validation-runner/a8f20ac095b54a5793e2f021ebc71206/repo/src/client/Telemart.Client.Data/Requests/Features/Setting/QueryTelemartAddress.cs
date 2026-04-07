using Telemart.Client.Data.Requests.Base;

using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public sealed class QueryTelemartAddress : QueryRequestBase<TelemartAddressDto>
    {
        public QueryTelemartAddress()
            : base(ApiResources.Settings, "telemart_address")
        {
        }
    }
}