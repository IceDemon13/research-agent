using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.FiscalRegistrar;

namespace Telemart.Client.Data.Requests.Features.FiscalRegistrar
{
    public class QueryFiscalConnectionTypes : QueryEntitiesRequestBase<FiscalConnectionTypeDto>
    {
        public QueryFiscalConnectionTypes()
            : base($"{ApiResources.Fiscal}/connection_types")
        {
        }
    }
}