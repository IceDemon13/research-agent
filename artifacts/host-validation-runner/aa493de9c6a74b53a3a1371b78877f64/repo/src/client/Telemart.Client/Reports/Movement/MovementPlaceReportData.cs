namespace Telemart.Client.ReportDesigner
{
    public class MovementPlaceReportData
    {
        public MovementPlaceReportData(int place, int places, int movementId, string warehouseSenderName, string warehouseRecipientName)
        {
            Place = place;
            Places = places;
            MovementId = movementId;
            WarehouseSenderName = warehouseSenderName;
            WarehouseRecipientName = warehouseRecipientName;
        }

        public int Place { get; }

        public int Places { get; }

        public int MovementId { get; }

        public string WarehouseSenderName { get; }

        public string WarehouseRecipientName { get; }
    }
}
