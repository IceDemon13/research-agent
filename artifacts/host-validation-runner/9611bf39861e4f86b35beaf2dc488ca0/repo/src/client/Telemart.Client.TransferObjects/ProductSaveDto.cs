using System.Collections.Generic;

namespace Telemart.Client.TransferObjects
{
    public sealed record ProductSaveDto
    {
        public int ParserCategoryId { get; init; }

        public string Code { get; init; }

        public string Name { get; init; }

        public string Pn { get; init; }

        public int AvailType { get; init; }

        public string Category { get; init; }

        public string FullCategory { get; init; }

        public IReadOnlyCollection<ParserPriceDto> Prices { get; init; }

        public IReadOnlyCollection<SupplierWarehouseAvailDto> SupplierWarehouseAvails { get; init; }
    }
}