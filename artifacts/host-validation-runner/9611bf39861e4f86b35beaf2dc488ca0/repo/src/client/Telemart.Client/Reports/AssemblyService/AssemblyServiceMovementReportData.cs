namespace Telemart.Client.Reports.AssemblyService
{
    public class AssemblyServiceMovementReportData
    {
        public AssemblyServiceMovementReportData(
            int orderId,
            int placeNumber,
            int totalPlaceNumbers,
            int assemblyServiceProductsQuantity,
            int assemblyServiceId)
        {
            OrderId = orderId;
            TotalPlaceNumbers = totalPlaceNumbers;
            PlaceNumber = placeNumber;
            AssemblyServiceId = assemblyServiceId;
            AssemblyServiceProductsQuantity = assemblyServiceProductsQuantity;
            Barcode = $"GR-{assemblyServiceId}";
        }

        public int OrderId { get; }

        public int AssemblyServiceId { get; }

        public int TotalPlaceNumbers { get; }

        public int PlaceNumber { get; }

        public string Barcode { get; }

        public int AssemblyServiceProductsQuantity { get; }
    }
}
