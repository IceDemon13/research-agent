namespace Telemart.Client.TransferObjects
{
    public sealed record ParserPriceDto
    {
        public int PriceType { get; init; }

        public double Value { get; init; }

        public int Currency { get; init; }
    }
}