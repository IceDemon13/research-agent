using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions
{
    public sealed class CreateManyReturnInvoices : CallActionWithBodyRequestResultBase<List<ReturnInvoiceDto>, ManyReturnInvoicesCreateDto>
    {
        public CreateManyReturnInvoices(ManyReturnInvoicesCreateDto dto)
            : base(dto, ApiResources.ReturnInvoices, "create_many")
        {
        }
    }
}