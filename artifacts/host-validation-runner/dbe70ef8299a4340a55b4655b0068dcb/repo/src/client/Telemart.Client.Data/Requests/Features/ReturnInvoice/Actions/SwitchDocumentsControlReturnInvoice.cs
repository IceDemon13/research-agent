using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions
{
    public class SwitchDocumentsControlReturnInvoice : CallEntityActionRequestResultBase<ReturnInvoiceDto>
    {
        public SwitchDocumentsControlReturnInvoice(int id)
            : base(id, ApiResources.ReturnInvoices, "switch_documents_control")
        {
        }
    }
}
