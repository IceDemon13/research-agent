using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public class QueryDisassemblyServiceOption : QueryEntityRequestBase<object>
    {
        public QueryDisassemblyServiceOption()
            : base(ApiResources.Settings, "disassembly_service_option")
        {
        }
    }
}