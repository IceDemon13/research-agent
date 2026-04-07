using System.Collections.Generic;

namespace Telemart.Client.TransferObjects.ParserRequests
{
    public sealed record SaveProductsRequest
    {
        public int ParserSettingsId { get; init; }

        public IReadOnlyCollection<ProductSaveDto> Products { get; init; }
    }
}