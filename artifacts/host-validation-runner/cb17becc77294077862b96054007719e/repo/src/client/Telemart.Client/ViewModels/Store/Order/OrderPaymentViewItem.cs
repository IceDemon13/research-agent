using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderPaymentViewItem : ValidatableItem
    {
        public bool CanEditProducts
        {
            get { return GetProperty(() => CanEditProducts); }
            set { SetProperty(() => CanEditProducts, value); }
        }
    }
}