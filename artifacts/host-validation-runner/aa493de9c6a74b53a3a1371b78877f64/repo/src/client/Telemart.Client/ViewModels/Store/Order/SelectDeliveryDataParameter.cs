using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class SelectDeliveryDataParameter
    {
        public SelectDeliveryDataParameter(
            int carryId,
            int cityId,
            string addressOld,
            DeliveryDataDto deliveryDataOld,
            bool selectNpAddressForWarehouse = false)
        {
            AddressOld = addressOld;
            DeliveryDataOld = deliveryDataOld;
            CarryId = carryId;
            CityId = cityId;
            SelectNpAddressForWarehouse = selectNpAddressForWarehouse;
        }

        public string AddressOld { get; }

        public DeliveryDataDto DeliveryDataOld { get; }

        public int CarryId { get; }

        public int CityId { get; }

        public bool SelectNpAddressForWarehouse { get; }
    }
}