namespace Telemart.Client.ViewModels.Store
{
    public class NpScanSheetParameter
    {
        public NpScanSheetParameter(int scansheetId)
        {
            ScansheetId = scansheetId;
        }

        public int ScansheetId { get; }
    }
}
