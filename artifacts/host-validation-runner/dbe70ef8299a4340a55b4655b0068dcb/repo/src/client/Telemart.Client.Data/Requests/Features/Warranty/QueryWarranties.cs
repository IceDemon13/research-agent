using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Warranty
{
    public sealed class QueryWarranties : QueryEntitiesPagedRequestBase<WarrantyDto>
    {
        public QueryWarranties()
            : base(ApiResources.Warranties)
        {
        }
    }
}