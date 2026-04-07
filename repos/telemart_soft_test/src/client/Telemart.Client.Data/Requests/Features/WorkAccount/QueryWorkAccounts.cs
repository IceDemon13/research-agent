using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.WorkAccount;

namespace Telemart.Client.Data.Requests.Features.WorkAccount
{
    public class QueryWorkAccounts : QueryEntitiesRequestBase<WorkAccountDto>
    {
        public QueryWorkAccounts()
            : base($"{ApiResources.WorkAccounts}")
        {
        }
    }
}