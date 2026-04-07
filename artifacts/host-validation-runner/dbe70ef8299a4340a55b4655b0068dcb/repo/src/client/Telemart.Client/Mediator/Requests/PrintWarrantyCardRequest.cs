using MediatR;

namespace Telemart.Client.Mediator.Requests
{
    public sealed class PrintWarrantyCardRequest : IRequest
    {
        public PrintWarrantyCardRequest(int orderId, int[] productIds, bool? separateWarrantyCards, bool showPreview)
        {
            OrderId = orderId;
            SeparateWarrantyCards = separateWarrantyCards;
            ProductIds = productIds;
            ShowPreview = showPreview;
        }

        public int OrderId { get; }

        public bool? SeparateWarrantyCards { get; }

        public int[] ProductIds { get; }

        public bool ShowPreview { get; }
    }
}