using System;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public class QueryNpContractors : QueryEntitiesRequestBase<NpContractorDto>
    {
        public QueryNpContractors()
            : base($"{ApiResources.Novaposhta}/{ApiResources.Contractors}")
        {
        }

        public QueryNpContractors(DateTime? modifiedOnAfter)
            : base(new ModifiedOnAfterFilteringItem { ModifiedOnAfter = modifiedOnAfter }, $"{ApiResources.Novaposhta}/{ApiResources.Contractors}")
        {
        }
    }
}