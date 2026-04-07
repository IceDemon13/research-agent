using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.InvoiceBudget;

namespace Telemart.Client.Data.Requests.Features.InvoiceBudget
{
    public sealed class QueryCategorySpentBudgets : CallActionWithBodyRequestResultBase<CategorySpentBudgetsDto, QueryCategorySpentBudgetsDto>
    {
        public QueryCategorySpentBudgets(QueryCategorySpentBudgetsDto dto)
            : base(dto, $"{ApiResources.InvoiceBudgets}/categories", "query_spent")
        {
        }
    }
}