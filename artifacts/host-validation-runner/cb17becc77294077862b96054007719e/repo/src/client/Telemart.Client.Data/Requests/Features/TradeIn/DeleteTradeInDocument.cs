using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class DeleteTradeInDocument : DeleteEntityRequestBase
    {
        public DeleteTradeInDocument(int documentId)
            : base(ApiResources.TradeIns, "documents", documentId)
        {
        }
    }
}