using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class DeleteTradeInEDocument : DeleteEntityResultRequestBase<object>
    {
        public DeleteTradeInEDocument(int documentId)
            : base(ApiResources.TradeIns, "e_document", documentId)
        {
        }
    }
}