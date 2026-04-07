using Telemart.Client.Business;

namespace Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo
{
    public interface IOrderPaymentInfoProduct
    {
        Price OriginalPrice { get; }

        Price Price { get; }

        int Quantity { get; }
    }
}
