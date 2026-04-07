using MediatR;

namespace Telemart.Client.Mediator.Requests
{
    public class PrintA4PdfRequest : IRequest
    {
        public PrintA4PdfRequest(string name, bool showPreview, byte[] bytesPdf)
        {
            Name = name;
            ShowPreview = showPreview;
            BytesPdf = bytesPdf;
        }

        public string Name { get; }

        public bool ShowPreview { get; }

        public byte[] BytesPdf { get; }
    }
}