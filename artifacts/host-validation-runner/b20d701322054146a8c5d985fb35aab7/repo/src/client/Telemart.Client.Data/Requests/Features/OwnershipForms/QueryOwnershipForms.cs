using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OwnershipForms
{
    public class QueryOwnershipForms : QueryEntitiesRequestBase<OwnershipFormDto>
    {
        public QueryOwnershipForms()
            : base(ApiResources.OwnershipForms)
        {
        }
    }
}
