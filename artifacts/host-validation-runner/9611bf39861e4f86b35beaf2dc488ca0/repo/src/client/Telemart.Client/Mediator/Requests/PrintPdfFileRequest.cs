using MediatR;

namespace Telemart.Client.Mediator.Requests
{
    public sealed class PrintPdfFileRequest : IRequest
    {
        public PrintPdfFileRequest(string filePath, string printerName, string paperSource, bool showPreview)
        {
            FilePath = filePath;
            PrinterName = printerName;
            PaperSource = paperSource;
            ShowPeview = showPreview;
        }

        public string FilePath { get; }

        public string PaperSource { get; }

        public string PrinterName { get; }

        public bool ShowPeview { get; }
    }
}