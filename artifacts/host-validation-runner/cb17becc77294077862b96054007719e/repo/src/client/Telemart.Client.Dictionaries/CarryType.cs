using System;

namespace Telemart.Client.Dictionaries
{
    public sealed class CarryType : DictionaryItem, IComparable<CarryType>
    {
        public const int PickupId = 1;
        public const int DeliveryId = 2;
        public const int NpWarehouseId = 3;
        public const int NpDeliveryId = 4;
        public const int GunselId = 5;
        public const int NochnikId = 6;
        public const int OtherId = 7;
        public const int MiniBusId = 8;
        public const int HomenkoId = 9;
        public const int LocalExpressId = 10;
        public const int SmartPostId = 11;
        public const int MeWarehouseId = 12;
        public const int MeDeliveryId = 13;
        public const int SupplierDeliveryId = 14;
        public const int MePostBoxId = 15;
        public const int MeMiniWarehouseId = 16;
        public const int NpPostBoxId = 17;
        public const int KievDeliveryId = 18;
        public const int GabaritkaId = 19;
        public const int UpWarehouseId = 20;
        public const int UpDeliveryId = 21;
        public const int TeksId = 22;
        public const int UklonId = 23;
        public const int TelemartServiceCourierId = 24;

        public CarryType(
            CarryTypeKind kind,
            int id,
            string name,
            string nameShort,
            string nameUa,
            string nameEn,
            int position,
            bool active,
            bool useInMovements,
            bool useInOrder,
            bool isLocal,
            bool requireLastName,
            bool requireMiddleName,
            string ttnRegex,
            int deliveryCost,
            int minFreeDeliveryCost,
            int? weightLimit,
            int? orderCostLimit,
            bool useInServiceMovement,
            bool canSwitchInOrders,
            bool stickerRequired,
            bool allowFreeUnderLimit,
            int? carryProviderId,
            bool scheduleDelivery,
            int[] drivers,
            int insurancePercent)
            : base(id, name, active)
        {
            ScheduleDelivery = scheduleDelivery;
            Drivers = drivers ?? [];
            Kind = kind;
            NameShort = nameShort;
            NameUa = nameUa;
            NameEn = nameEn;
            Position = position;
            UseInMovements = useInMovements;
            UseInOrder = useInOrder;
            IsLocal = isLocal;
            RequireLastName = requireLastName;
            RequireMiddleName = requireMiddleName;
            TtnRegex = ttnRegex;
            DeliveryCost = deliveryCost;
            MinFreeDeliveryCost = minFreeDeliveryCost;
            WeightLimit = weightLimit;
            OrderCostLimit = orderCostLimit;
            UseInServiceMovement = useInServiceMovement;
            CanSwitchInOrders = canSwitchInOrders;
            AllowFreeUnderLimit = allowFreeUnderLimit;
            StickerRequired = stickerRequired;
            CarryProviderId = carryProviderId;
            InsurancePercent = insurancePercent;
        }

        public bool ScheduleDelivery { get; }

        public CarryTypeKind Kind { get; }

        public string NameShort { get; }

        public string NameUa { get; }

        public string NameEn { get; }

        public int Position { get; }

        public bool UseInMovements { get; }

        public bool UseInOrder { get; }

        public bool IsLocal { get; }

        public bool RequireLastName { get; }

        public bool RequireMiddleName { get; }

        public bool CanSwitchInOrders { get; }

        public string TtnRegex { get; }

        public int DeliveryCost { get; }

        public int MinFreeDeliveryCost { get; }

        public int? WeightLimit { get; }

        public int? OrderCostLimit { get; }

        public bool UseInServiceMovement { get; }

        public bool StickerRequired { get; }

        public bool AllowFreeUnderLimit { get; }

        public int? CarryProviderId { get; }

        public int[] Drivers { get; }

        public int InsurancePercent { get; }

        public static bool IsNovaposhta(int id)
        {
            return id is NpDeliveryId
                or NpWarehouseId
                or NpPostBoxId
                or LocalExpressId;
        }

        public bool IsNovaposhta()
        {
            return Id is NpDeliveryId
                or NpWarehouseId
                or NpPostBoxId
                or LocalExpressId;
        }

        public bool IsUkrposhta()
        {
            return Id is UpDeliveryId
                or UpWarehouseId;
        }

        public bool IsMeestExpress()
        {
            return Id is MeDeliveryId
                or MeWarehouseId
                or MeMiniWarehouseId
                or MePostBoxId;
        }

        public bool IsActive()
        {
            return Active && UseInOrder;
        }

        public int CompareTo(CarryType other)
        {
            return Position.CompareTo(other?.Position ?? -1);
        }

        public override int CompareTo(DictionaryItemBase other)
        {
            return CompareTo(other as CarryType);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}