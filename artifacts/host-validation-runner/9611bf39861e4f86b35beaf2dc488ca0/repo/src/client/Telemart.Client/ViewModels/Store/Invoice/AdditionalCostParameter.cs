using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class AdditionalCostParameter : EditorParameter
    {
        public AdditionalCostParameter(int id, int invoiceId)
            : base(id)
        {
            InvoiceId = invoiceId;
        }

        public int InvoiceId { get; set; }
    }
}