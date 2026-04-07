using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public sealed class InvoiceEditViewMessage : EditorParameter
    {
        public InvoiceEditViewMessage(int invoiceId)
        : base(invoiceId)
        {
        }
    }
}
