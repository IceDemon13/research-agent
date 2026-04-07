using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderWarehouseViewItem : ValidatableItem
    {
        public int CityId { get; set; }

        public int Position { get; set; }

        public bool Active { get; set; }

        public string Address { get; set; }

        public string AddressUa { get; set; }

        public string AddressEn { get; set; }

        public bool UseCells { get; set; }

        public int MaxPackageWeight { get; set; }

        public int? AssemblyWarehouseId { get; set; }

        public int? LocationId { get; set; }
    }
}