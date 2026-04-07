using DevExpress.Mvvm.DataAnnotations;
using System.Collections.Generic;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Carry
{
    public class CarryViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int CarryTypeId
        {
            get { return GetProperty(() => CarryTypeId); }
            set { SetProperty(() => CarryTypeId, value); }
        }

        public int InsurancePercent
        {
            get { return GetProperty(() => InsurancePercent); }
            set { SetProperty(() => InsurancePercent, value); }
        }

        public int ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string NameShort
        {
            get { return GetProperty(() => NameShort); }
            set { SetProperty(() => NameShort, value); }
        }

        public decimal Comission
        {
            get { return GetProperty(() => Comission); }
            set { SetProperty(() => Comission, value); }
        }

        public int? OrderCostLimit
        {
            get { return GetProperty(() => OrderCostLimit); }
            set { SetProperty(() => OrderCostLimit, value); }
        }

        public string NameUa
        {
            get { return GetProperty(() => NameUa); }
            set { SetProperty(() => NameUa, value); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public bool UseInMovements
        {
            get { return GetProperty(() => UseInMovements); }
            set { SetProperty(() => UseInMovements, value); }
        }

        public bool IsLocal
        {
            get { return GetProperty(() => IsLocal); }
            set { SetProperty(() => IsLocal, value); }
        }

        public bool RequireLastName
        {
            get { return GetProperty(() => RequireLastName); }
            set { SetProperty(() => RequireLastName, value); }
        }

        public bool RequireMiddleName
        {
            get { return GetProperty(() => RequireMiddleName); }
            set { SetProperty(() => RequireMiddleName, value); }
        }

        public bool OurWarehouseShipment
        {
            get { return GetProperty(() => OurWarehouseShipment); }
            set { SetProperty(() => OurWarehouseShipment, value); }
        }

        public decimal MaxLeftToPayUsd
        {
            get { return GetProperty(() => MaxLeftToPayUsd); }
            set { SetProperty(() => MaxLeftToPayUsd, value); }
        }

        public decimal MaxLeftToPayUah
        {
            get { return GetProperty(() => MaxLeftToPayUah); }
            set { SetProperty(() => MaxLeftToPayUah, value); }
        }

        public int DeliveryCost
        {
            get { return GetProperty(() => DeliveryCost); }
            set { SetProperty(() => DeliveryCost, value); }
        }

        public int MinFreeDeliveryCost
        {
            get { return GetProperty(() => MinFreeDeliveryCost); }
            set { SetProperty(() => MinFreeDeliveryCost, value); }
        }

        public List<int> Drivers
        {
            get { return GetProperty(() => Drivers); }
            set { SetProperty(() => Drivers, value); }
        }

        public static void BuildMetadata(MetadataBuilder<CarryViewItem> builder)
        {
            builder.Property(x => x.Name).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameShort).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameUa).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameEn).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DeliveryCost)
             .MatchesInstanceRule((x, y) => y.DeliveryCost >= 0, () => "Значение не может быть отрицательным");
            builder.Property(x => x.OrderCostLimit)
                .MatchesInstanceRule((x, y) => x is null || x > 0, () => "Значение должно быть больше 0")
                .MatchesInstanceRule((x, y) => x is null || x < 10_000_000, () => "Значение должно быть меньше 10000000");

            builder.Property(x => x.Comission)
                .MatchesInstanceRule((x, y) => x >= 0, () => "Значение не должно быть меньше 0")
                .MatchesInstanceRule((x, y) => x < 1000, () => "Значение должно быть меньше 1000");

            builder.Property(x => x.InsurancePercent)
                .MatchesInstanceRule((x, y) => x >= 0, () => "Значение не должно быть меньше 0")
                .MatchesInstanceRule((x, y) => x < 1000, () => "Значение должно быть меньше 1000");

            builder.Property(x => x.MinFreeDeliveryCost)
                .MatchesInstanceRule((x, y) => x >= 0, () => "Значение должно быть больше 0");
        }
    }
}