using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.InvoiceBudget;

namespace Telemart.Client.Data.Requests.Features.InvoiceBudget
{
    public sealed class QueryInvoiceCategoryBudgets : QueryEntitiesRequestBase<InvoiceCategoryBudgetDto>
    {
        public QueryInvoiceCategoryBudgets()
            : base(null, $"{ApiResources.InvoiceBudgets}/categories")
        {
        }
    }
}