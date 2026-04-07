using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderCarryTypeViewItem : ValidatableItem
    {
        public int Position { get; set; }

        public bool IsLocal { get; set; }

        public bool RequireLastName { get; set; }

        public bool RequireMiddleName { get; set; }

        public bool Active { get; set; }

        public int DeliveryCost { get; set; }

        public int MinFreeDeliveryCost { get; set; }

        public int? WeightLimit { get; set; }

        public int? OrderCostLimit { get; set; }

        public bool AllowFreeUnderLimit { get; set; }
    }
}