using System;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public sealed class OpenInvoice : CallEntityActionWithBodyRequestResultBase<InvoiceDto, InvoiceOpenDto>
    {
        public OpenInvoice(int invoiceId, DateTime dateClose, int employeeRequestId)
            : base(invoiceId, new InvoiceOpenDto(invoiceId, dateClose, employeeRequestId), "invoices", "open")
        {
        }
    }
}