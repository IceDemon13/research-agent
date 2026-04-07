using System;
using System.Collections.Generic;
using Telemart.Client.Core.Exceptions;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices.Exceptions
{
    [Serializable]
    public sealed class PriceCalculationValidationException : GenericException<PriceCalculationValidationExceptionArgs>
    {
        public PriceCalculationValidationException(
            int productId,
            string productName,
            IReadOnlyCollection<string> validationResultItems,
            Exception innerException = null)
            : base(
                new PriceCalculationValidationExceptionArgs(productId, productName, validationResultItems),
                "Failed to calculate product prices",
                innerException)
        {
        }

        public PriceCalculationValidationException()
        {
        }

        public PriceCalculationValidationException(string message)
            : base(message)
        {
        }

        public PriceCalculationValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private PriceCalculationValidationException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }
}