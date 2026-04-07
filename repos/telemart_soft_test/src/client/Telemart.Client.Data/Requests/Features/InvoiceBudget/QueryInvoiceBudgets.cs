using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.InvoiceBudget;

namespace Telemart.Client.Data.Requests.Features.InvoiceBudget
{
    public sealed class QueryInvoiceBudgets : QueryEntitiesRequestBase<InvoiceBudgetDto>
    {
        public QueryInvoiceBudgets()
            : base(null, ApiResources.InvoiceBudgets)
        {
        }
    }
}