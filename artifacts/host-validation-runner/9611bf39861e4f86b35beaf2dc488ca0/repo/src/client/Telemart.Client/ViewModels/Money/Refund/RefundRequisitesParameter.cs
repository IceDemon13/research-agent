using Telemart.Client.ViewModels.Common;

namespace Telemart.Client.ViewModels.Money.Refund
{
    public sealed class RefundRequisitesParameter
    {
        public RefundRequisitesParameter(RequisitesViewItem requisites, int documentId)
        {
            Requisites = requisites;
            DocumentId = documentId;
        }

        public int DocumentId { get; }

        public RequisitesViewItem Requisites { get; }
    }
}