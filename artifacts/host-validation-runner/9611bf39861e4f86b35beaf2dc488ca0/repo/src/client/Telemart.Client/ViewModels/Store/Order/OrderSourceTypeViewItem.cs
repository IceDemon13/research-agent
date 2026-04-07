using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderSourceTypeViewItem : ValidatableItem
    {
        public int Position { get; set; }

        public bool AvailOn { get; set; }
    }
}