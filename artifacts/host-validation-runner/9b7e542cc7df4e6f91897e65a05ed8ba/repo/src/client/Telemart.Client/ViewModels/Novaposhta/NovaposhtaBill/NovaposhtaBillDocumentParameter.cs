namespace Telemart.Client.ViewModels.Novaposhta.NovaposhtaBill
{
    public class NovaposhtaBillDocumentParameter
    {
        public NovaposhtaBillDocumentParameter(int billId)
        {
            BillId = billId;
        }

        public int BillId { get; }
    }
}
