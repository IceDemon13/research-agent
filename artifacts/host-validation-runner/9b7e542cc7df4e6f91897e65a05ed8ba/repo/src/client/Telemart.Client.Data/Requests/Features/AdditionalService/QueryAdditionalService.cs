using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService
{
    public sealed class QueryAdditionalService : QueryEntityRequestBase<AdditionalServiceDto>
    {
        public QueryAdditionalService(int id)
            : base(ApiResources.AdditionalServices, id)
        {
        }
    }
}