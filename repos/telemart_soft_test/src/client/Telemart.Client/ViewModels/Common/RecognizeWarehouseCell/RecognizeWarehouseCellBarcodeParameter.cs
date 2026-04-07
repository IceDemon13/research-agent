namespace Telemart.Client.ViewModels.Common.RecognizeWarehouseCell
{
    public class RecognizeWarehouseCellBarcodeParameter
    {
        public RecognizeWarehouseCellBarcodeParameter(int warehouseId, bool? used)
        {
            WarehouseId = warehouseId;
            Used = used;
        }

        public int WarehouseId { get; }

        public bool? Used { get; }
    }
}
