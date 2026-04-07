using System;
using System.Collections.Generic;
using System.Text;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public class PurchaseInvoice : CallEntityActionRequestResultBase<InvoiceDto>
    {
        public PurchaseInvoice(int id)
            : base(id, ApiResources.Invoices, "purchase")
        {
        }
    }
}
