namespace Telemart.Client.TransferObjects.ParserRequests
{
    public sealed record ClearAvailableBySupplierWarehouseIdRequest
    {
        public int ContractorId { get; init; }
    }
}