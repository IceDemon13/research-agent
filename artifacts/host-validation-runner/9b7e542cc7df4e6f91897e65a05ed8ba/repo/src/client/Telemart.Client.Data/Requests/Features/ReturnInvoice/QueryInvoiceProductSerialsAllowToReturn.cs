using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice
{
    public sealed class QueryInvoiceProductSerialsAllowToReturn : QueryEntitiesRequestBase<InvoiceProductSerialDto>
    {
        public QueryInvoiceProductSerialsAllowToReturn(int[] invoiceIds)
            : base(new InvoiceProductSerialFilteringItem(invoiceIds), $"{ApiResources.ReturnInvoices}/avail_invoice_product_serials")
        {
        }

        internal sealed class InvoiceProductSerialFilteringItem : FilteringItemBase
        {
            public InvoiceProductSerialFilteringItem(int[] invoiceIds)
            {
                InvoiceIds = invoiceIds;
            }

            [FilteringItemProperty("invoice_ids")]
            public int[] InvoiceIds { get; }
        }
    }
}