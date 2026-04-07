using MediatR;

namespace Telemart.Client.Mediator.Requests
{
    public sealed class PrintScanSheetRequest : IRequest
    {
        public PrintScanSheetRequest(string scanSheetRef, string link, bool showPreview, string base64Pdf = null)
        {
            ScanSheetRef = scanSheetRef;
            Link = link;
            ShowPreview = showPreview;
            Base64Pdf = base64Pdf;
        }

        public string ScanSheetRef { get; }

        public string Link { get; }

        public bool ShowPreview { get; }

        public string Base64Pdf { get; }
    }
}