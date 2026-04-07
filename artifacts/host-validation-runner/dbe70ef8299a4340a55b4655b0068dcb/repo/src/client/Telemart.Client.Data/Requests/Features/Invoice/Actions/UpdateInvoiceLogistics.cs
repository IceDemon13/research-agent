using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public sealed class UpdateInvoiceLogistics : CallEntityActionWithBodyRequestResultBase<InvoiceDto, InvoiceUpdateLogisticsDto>
    {
        public UpdateInvoiceLogistics(int invoiceId, int? employeeCarrierId, int carryId, string comment, IReadOnlyCollection<InvoiceTtnDto> trackNumbers)
            : base(invoiceId, new InvoiceUpdateLogisticsDto(employeeCarrierId, carryId, comment, trackNumbers), ApiResources.Invoices, "logistics")
        {
        }
    }
}
