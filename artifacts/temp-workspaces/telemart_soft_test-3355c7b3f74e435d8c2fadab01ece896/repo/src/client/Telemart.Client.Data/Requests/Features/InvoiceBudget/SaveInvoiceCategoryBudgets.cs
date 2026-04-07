using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.InvoiceBudget;

namespace Telemart.Client.Data.Requests.Features.InvoiceBudget
{
    public sealed class SaveInvoiceCategoryBudgets : CallActionWithBodyRequestResultBase<object, SaveInvoiceCategoryBudgetsDto>
    {
        public SaveInvoiceCategoryBudgets(SaveInvoiceCategoryBudgetsDto dto)
            : base(dto, $"{ApiResources.InvoiceBudgets}/categories", "save")
        {
        }
    }
}