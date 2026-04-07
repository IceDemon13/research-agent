using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.NovaposhtaTtn;

namespace Telemart.Client.Data.Requests.Features.Novaposhta.Actions
{
    public sealed class CreateNpTtnByOrder : CallActionWithBodyRequestResultBase<NpDocumentDto, NovaposhtaTtnCreateByOrderDto>
    {
        public CreateNpTtnByOrder(int orderId, int packagePlaces, decimal packageWeight, int? width, int? length, int? height, bool addToApplication)
            : base(new NovaposhtaTtnCreateByOrderDto(orderId, packagePlaces, packageWeight, width, length, height, addToApplication), ApiResources.NovaposhtaDocuments, "create_by_order")
        {
        }
    }
}