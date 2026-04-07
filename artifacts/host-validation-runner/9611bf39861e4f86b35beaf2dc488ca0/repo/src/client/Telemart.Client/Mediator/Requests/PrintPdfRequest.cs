using System.IO;
using MediatR;

namespace Telemart.Client.Mediator.Requests
{
    public class PrintPdfRequest : IRequest
    {
        public PrintPdfRequest(Stream body, string printerName, string paperSource, bool showPreview)
        {
            Body = body;
            PrinterName = printerName;
            PaperSource = paperSource;
            ShowPeview = showPreview;
        }

        public Stream Body { get; }

        public string PaperSource { get; }

        public string PrinterName { get; }

        public bool ShowPeview { get; }
    }
}