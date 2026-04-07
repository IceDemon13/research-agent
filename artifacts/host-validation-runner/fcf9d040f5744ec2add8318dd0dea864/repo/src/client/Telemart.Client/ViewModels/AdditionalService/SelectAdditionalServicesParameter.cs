using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public sealed class SelectAdditionalServicesParameter
    {
        public SelectAdditionalServicesParameter(
            int productId,
            string productName,
            decimal productPriceOut,
            int contractorId,
            int paymentId,
            IReadOnlyCollection<ExternalPaymentDto> externalPayments,
            bool allowEdit,
            IReadOnlyCollection<int> additionalServiceProductTypeIds = null)
        {
            ProductPriceOut = productPriceOut;
            ProductId = productId;
            ProductName = productName;
            ContractorId = contractorId;
            PaymentId = paymentId;
            AllowEdit = allowEdit;
            ExternalPayments = externalPayments;
            AdditionalServiceProductTypeIds = additionalServiceProductTypeIds;
        }

        public int PaymentId { get; }

        public IReadOnlyCollection<ExternalPaymentDto> ExternalPayments { get; }

        public int ProductId { get; }

        public int ContractorId { get; }

        public string ProductName { get; }

        public decimal ProductPriceOut { get; }

        public bool AllowEdit { get; }

        public IReadOnlyCollection<int> AdditionalServiceProductTypeIds { get; }
    }
}