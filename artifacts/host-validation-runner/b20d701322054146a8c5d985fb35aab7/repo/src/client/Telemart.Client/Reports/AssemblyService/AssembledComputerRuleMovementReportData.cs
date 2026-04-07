namespace Telemart.Client.Reports.AssemblyService
{
    public class AssembledComputerRuleMovementReportData
    {
        public AssembledComputerRuleMovementReportData(
            int orderId,
            int assemblyServiceId,
            int totalPlaceNumbers,
            int placeNumber,
            int assemblyServiceProductsQuantity,
            string nomenclatureSeries,
            string productName,
            int productId)
        {
            OrderId = orderId;
            AssemblyServiceId = assemblyServiceId;
            TotalPlaceNumbers = totalPlaceNumbers;
            PlaceNumber = placeNumber;
            Barcode = $"TEL-{productId}";
            AssemblyServiceProductsQuantity = assemblyServiceProductsQuantity;
            NomenclatureSeries = nomenclatureSeries;
            ProductName = productName;
            ProductId = productId;
        }

        public int OrderId { get; }

        public int AssemblyServiceId { get; }

        public int TotalPlaceNumbers { get; }

        public int PlaceNumber { get; }

        public string Barcode { get; }

        public int AssemblyServiceProductsQuantity { get; }

        public string NomenclatureSeries { get; }

        public string ProductName { get; }

        public int ProductId { get; }
    }
}
