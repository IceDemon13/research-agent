using System.Collections.ObjectModel;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class SetOrderProductPriceParameter
    {
        public SetOrderProductPriceParameter(ReadOnlyObservableCollection<OrderProductViewModel> orderProducts, OrderContractorViewItem selectedContractor)
        {
            OrderProducts = orderProducts;
            SelectedContractor = selectedContractor;
        }

        public OrderContractorViewItem SelectedContractor { get; }

        public ReadOnlyObservableCollection<OrderProductViewModel> OrderProducts { get; }
    }
}