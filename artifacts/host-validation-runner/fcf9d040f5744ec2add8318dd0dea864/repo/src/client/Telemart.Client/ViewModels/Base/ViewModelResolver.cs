using Telemart.Client.Common.Messages;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Cashbox;
using Telemart.Client.ViewModels.TradeIn;
using Telemart.Client.ViewModels.Warehouse;

namespace Telemart.Client.ViewModels.Base
{
    public class ViewModelResolver : IViewModelResolver
    {
        public (bool ViewModelSupport, object Message) Resolve(int? entityId, int documentId)
        {
            switch (entityId)
            {
                case Entity.ServiceRequestId:
                    return (true, new ServiceRequestViewMessage(documentId));
                case Entity.CallId:
                    return (true, new CallViewMessage(documentId));
                case Entity.TradeInId:
                    return (true, new TradeInViewMessage(documentId));
                case Entity.InvoiceId:
                    return (true, new InvoiceEditViewMessage(documentId));
                case Entity.OrderId:
                    return (true, new OrderEditViewMessage(documentId));
                case Entity.ContractorId:
                    return (true, new ContractorViewMessage(documentId));
                case Entity.ParserSettingsId:
                    return (true, new ParserSettingsViewMessage(documentId));
                case Entity.MovementId:
                    return (true, new MovementViewMessage(documentId));
                case Entity.ReturnInvoiceId:
                    return (true, new ReturnInvoiceEditViewMessage(documentId));
                case Entity.AssemblyServiceId:
                    return (true, new AssemblyServiceViewMessage(documentId));
                case Entity.AdditionalServiceProductId:
                    return (true, new AdditionalServiceProductViewMessage(documentId));
                case Entity.CashboxId:
                    return (true, new CashboxEditParameter(documentId));
                case Entity.WarehouseId:
                    return (true, new WarehouseEditParameter(documentId));
                case Entity.RefundId:
                    return (true, new RefundViewMessage(documentId));
                case Entity.DiscussionId:
                    return (true, new DiscussionViewMessage(documentId));

                default:
                    return default;
            }
        }
    }
}