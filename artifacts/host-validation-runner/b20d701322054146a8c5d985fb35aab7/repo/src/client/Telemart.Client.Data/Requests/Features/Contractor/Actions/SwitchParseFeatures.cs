using System.Net.Http;
using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Contractor.Actions
{
    public class SwitchParseFeatures : CallEntityActionRequestResultBase<object>
    {
        public SwitchParseFeatures(int contractorId)
            : base(contractorId, ApiResources.Contractors, "switch_parse_features")
        {
            Method = HttpMethod.Put;
        }
    }
}