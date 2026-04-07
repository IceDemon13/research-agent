using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public class Invoice1CSetInvoiceReturnedProducts : CallActionWithBodyRequestResultBase<List<InvoiceDto>, Invoice1CDto>
    {
        public Invoice1CSetInvoiceReturnedProducts(Invoice1CDto invoiceIds)
            : base(invoiceIds, ApiResources.Invoices, "invoices_1c_set_returned_products")
        {
        }
    }
}