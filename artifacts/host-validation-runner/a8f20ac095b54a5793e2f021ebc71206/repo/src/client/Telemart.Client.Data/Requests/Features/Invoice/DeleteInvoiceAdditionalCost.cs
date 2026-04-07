using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public class DeleteInvoiceAdditionalCost : DeleteEntityResultRequestBase<object>
    {
        public DeleteInvoiceAdditionalCost(int additionalCostId)
            : base(ApiResources.Invoices, ApiResources.AdditionalCosts, additionalCostId)
        {
        }
    }
}