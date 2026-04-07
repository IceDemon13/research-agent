using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public sealed class QueryInvoiceAdditionalCost : QueryEntityRequestBase<InvoiceAdditionalCostDto>
    {
        public QueryInvoiceAdditionalCost(int additionalCostId)
            : base(ApiResources.Invoices, ApiResources.AdditionalCosts, additionalCostId)
        {
        }
    }
}