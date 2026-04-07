using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.InvoiceBudget;
using Telemart.Client.TransferObjects.RobotProperty;

namespace Telemart.Client.Data.Requests.Features.InvoiceBudget
{
    public sealed class SaveInvoiceBudgets : CallActionWithBodyRequestResultBase<object, SaveInvoiceBudgetsDto>
    {
        public SaveInvoiceBudgets(SaveInvoiceBudgetsDto dto)
            : base(dto, ApiResources.InvoiceBudgets, "save")
        {
        }
    }
}