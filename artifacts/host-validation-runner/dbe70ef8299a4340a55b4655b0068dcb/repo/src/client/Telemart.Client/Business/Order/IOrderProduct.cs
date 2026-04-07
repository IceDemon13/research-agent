using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;

namespace Telemart.Client.Business.Order
{
    public interface IOrderProduct : IOrderPaymentInfoProduct
    {
        int Id { get; }

        int ProductId { get; }
        
        string ProductName { get; }

        OrderProductStatus State { get; }

        OrderProductSource Source { get; }

        int? OrderFolderId { get; }

        int? ParentRecordId { get; }

        bool IsAdditionalService { get; }

        bool AssemblyIncluded { get; }
    }
}