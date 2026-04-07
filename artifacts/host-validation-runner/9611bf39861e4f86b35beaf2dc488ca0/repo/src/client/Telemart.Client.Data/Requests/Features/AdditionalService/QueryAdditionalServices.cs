using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService
{
    public sealed class QueryAdditionalServices : QueryEntitiesPagedRequestBase<AdditionalServiceDto>
    {
        public QueryAdditionalServices(IFilteringItem filter)
            : base(filter, ApiResources.AdditionalServices)
        {
        }
    }
}