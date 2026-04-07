using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderSplitProductParameter
    {
        public OrderSplitProductParameter(int orderId, int orderProductId, OrderFolderDto orderFolder, bool isAssemblyVirrtualProduct, int quantity)
        {
            OrderId = orderId;
            OrderProductId = orderProductId;
            OrderFolder = orderFolder;
            IsAssemblyVirtualProduct = isAssemblyVirrtualProduct;
            Quantity = quantity;
        }

        public int OrderId { get; }

        public int OrderProductId { get; }

        public OrderFolderDto OrderFolder { get; }

        public bool IsAssemblyVirtualProduct { get; }

        public int Quantity { get; }
    }
}
