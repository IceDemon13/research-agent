using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderLogisticsParameter
    {
        public OrderLogisticsParameter(
            int id,
            int subdivisionId,
            int paymentId,
            int carryId,
            int cityId,
            int? warehouseId,
            int? assemblyWarehouseId,
            int? bufferWarehouseId,
            int? additionalServiceWarehouseId,
            int stateId,
            bool freeDelivery,
            bool ignoreDeliveryCostCalculation,
            OrderPaymentRecordViewItem[] orderPayments,
            OrderProductViewItem[] orderProducts,
            int bonusesQuantity,
            int bonusesToChargeQuantity,
            int currentPackageDeliveryCost,
            IReadOnlyCollection<OrderFolderDto> folders,
            decimal? moneyBackAmount)
        {
            Id = id;
            CarryId = carryId;
            PaymentId = paymentId;
            CityId = cityId;
            SubdivisionId = subdivisionId;
            FreeDelivery = freeDelivery;
            IgnoreDeliveryCostCalculation = ignoreDeliveryCostCalculation;
            OrderPayments = orderPayments;
            WarehouseId = warehouseId;
            AssemblyWarehouseId = assemblyWarehouseId;
            BufferWarehouseId = bufferWarehouseId;
            AdditionalServiceWarehouseId = additionalServiceWarehouseId;
            StateId = stateId;
            OrderProducts = orderProducts;
            BonusesQuantity = bonusesQuantity;
            BonusesToChargeQuantity = bonusesToChargeQuantity;
            Folders = folders;
            CurrentPackageDeliveryCost = currentPackageDeliveryCost;
            MoneyBackAmount = moneyBackAmount;
        }

        public int Id { get; }

        public int CarryId { get; }

        public int PaymentId { get; }

        public int CityId { get; }

        public int BonusesQuantity { get; }

        public int BonusesToChargeQuantity { get; }

        public int SubdivisionId { get; }

        public bool FreeDelivery { get; }

        public bool IgnoreDeliveryCostCalculation { get; }

        public int CurrentPackageDeliveryCost { get; }

        public int? WarehouseId { get; }

        public int? AssemblyWarehouseId { get; }

        public int? BufferWarehouseId { get; }

        public int? AdditionalServiceWarehouseId { get; }

        public int StateId { get; }

        public decimal? MoneyBackAmount { get; }

        public OrderPaymentRecordViewItem[] OrderPayments { get; }

        public OrderProductViewItem[] OrderProducts { get; }

        public IReadOnlyCollection<OrderFolderDto> Folders { get; }
    }
}