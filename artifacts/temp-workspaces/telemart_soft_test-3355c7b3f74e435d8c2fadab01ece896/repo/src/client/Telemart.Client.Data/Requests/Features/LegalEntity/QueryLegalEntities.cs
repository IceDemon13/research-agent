using System;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.LegalEntity
{
    public class QueryLegalEntities : QueryEntitiesRequestBase<LegalEntityDto>
    {
        public QueryLegalEntities()
            : base(ApiResources.LegalEntities)
        {
        }

        public QueryLegalEntities(DateTime? modifiedOnAfter)
            : base(new ModifiedOnAfterFilteringItem { ModifiedOnAfter = modifiedOnAfter }, ApiResources.LegalEntities)
        {
        }
    }
}