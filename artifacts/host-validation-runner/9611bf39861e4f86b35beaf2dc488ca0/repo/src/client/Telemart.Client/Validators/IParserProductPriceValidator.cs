using System.Collections.Generic;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Validators
{
    public interface IParserProductPriceValidator
    {
        Result<IReadOnlyCollection<ProductSaveDto>> ValidateAsync(IReadOnlyCollection<ProductSaveDto> posSettings);
    }
}