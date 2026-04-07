using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.LegalEntity
{
    public class QueryLegalEntity : QueryEntityRequestBase<LegalEntityDto>
    {
        public QueryLegalEntity(int id)
            : base(ApiResources.LegalEntities, id)
        {
        }
    }
}