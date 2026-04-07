using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.Common.Messages
{
    public class NpScanSheetViewMessage : NpScanSheetParameter
    {
        public NpScanSheetViewMessage(int scansheetId)
            : base(scansheetId)
        {
        }
    }
}
