using System;
using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public class DelayInvoice : CallEntityActionWithBodyRequestResultBase<InvoiceDto, InvoiceDelayDto>
    {
        public DelayInvoice(int id, DateTime arriveDate, int? warehouseId, IReadOnlyCollection<InvoiceDelayProductDto> products, int expireReasonId)
            : base(id, new InvoiceDelayDto(id, products, arriveDate, expireReasonId, warehouseId), ApiResources.Invoices, "delay")
        {
        }
    }
}