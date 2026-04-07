using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.ViewModels.Novaposhta
{
    public class UpdateNovaposhtaTtnParameter
    {
        public UpdateNovaposhtaTtnParameter(int orderId, NpDocumentDto npDocument)
        {
            OrderId = orderId;
            NpDocument = npDocument;
        }

        public int OrderId { get; }

        public NpDocumentDto NpDocument { get; }
    }
}