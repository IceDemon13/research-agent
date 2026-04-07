using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public class QueryCheckReceiveGuestProductAct : QueryEntityRequestBase<string>
    {
        public QueryCheckReceiveGuestProductAct()
            : base(ApiResources.Settings, "check_receive_guest_product_act")
        {
        }
    }
}