using System;
using System.Collections.Generic;
using Telemart.Client.Core.Exceptions;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices.Exceptions
{
    public sealed class PriceCalculationValidationExceptionArgs : ExceptionArgs
    {
        public PriceCalculationValidationExceptionArgs(int productId, string productName, IReadOnlyCollection<string> errorMessages)
        {
            ProductId = productId;
            ProductName = productName;
            ErrorMessages = errorMessages ?? Array.Empty<string>();
        }

        public int ProductId { get; }

        public string ProductName { get; }

        public IReadOnlyCollection<string> ErrorMessages { get; }

        public override string Message => string.Join(",", ErrorMessages);
    }
}