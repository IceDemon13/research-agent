using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Purchase
{
    public class SetNoProductParameter
    {
        public SetNoProductParameter(OrderDto orderDto, int orderProductId, int productId, int productQuantityOrder, decimal productPriceOrder, int productSourceId)
        {
            OrderDto = orderDto;
            OrderProductId = orderProductId;
            ProductId = productId;
            ProductQuantityOrder = productQuantityOrder;
            ProductPriceOrder = productPriceOrder;
            ProductSourceId = productSourceId;
        }

        public OrderDto OrderDto { get; }

        public int OrderProductId { get; }

        public int ProductId { get; }

        public int ProductQuantityOrder { get; }

        public decimal ProductPriceOrder { get; }

        public int ProductSourceId { get; }
    }
}