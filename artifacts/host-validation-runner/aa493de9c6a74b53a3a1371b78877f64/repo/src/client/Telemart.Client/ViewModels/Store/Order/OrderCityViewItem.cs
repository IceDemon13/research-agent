using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderCityViewItem : ValidatableItem
    {
        public int[] CityCarries { get; set; }

        public int Position { get; set; }

        public int? AreaId { get; set; }

        public bool Active { get; init; }
    }
}