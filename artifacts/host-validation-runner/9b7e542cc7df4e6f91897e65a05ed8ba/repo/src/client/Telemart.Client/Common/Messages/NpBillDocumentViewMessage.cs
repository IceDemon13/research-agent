using Telemart.Client.ViewModels.Novaposhta.NovaposhtaBill;

namespace Telemart.Client.Common.Messages
{
    public class NpBillDocumentViewMessage : NovaposhtaBillDocumentParameter
    {
        public NpBillDocumentViewMessage(int billId)
            : base(billId)
        {
        }
    }
}
