using Telemart.Client.TransferObjects.Warehouse.Cell;

namespace Telemart.Client.ViewModels.Common.RecognizeWarehouseCell
{
    public class RecognizeWarehouseCellBarcodeResultEventArgs : RecognizeBarcodeResultEventArgsBase
    {
        private RecognizeWarehouseCellBarcodeResultEventArgs(
            WarehouseCellDto cell,
            string barcodeText,
            string errorText)
        : base(barcodeText, errorText)
        {
            Cell = cell;
        }

        public WarehouseCellDto Cell { get; }

        public static RecognizeWarehouseCellBarcodeResultEventArgs Error(string barcode, string errorText)
        {
            return new RecognizeWarehouseCellBarcodeResultEventArgs(null, barcode, errorText);
        }

        public static RecognizeWarehouseCellBarcodeResultEventArgs Found(WarehouseCellDto cell, string barcode)
        {
            return new RecognizeWarehouseCellBarcodeResultEventArgs(cell, barcode, string.Empty);
        }
    }
}
