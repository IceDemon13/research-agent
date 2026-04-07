using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public sealed class ServiceInvoiceViewMessage : EditorParameter
    {
        public ServiceInvoiceViewMessage(int serviceInvoiceId)
            : base(serviceInvoiceId)
        {
        }
    }
}
