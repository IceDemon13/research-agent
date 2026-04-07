using MediatR;

namespace Telemart.Client.Mediator.Requests
{
    public sealed class PrintTrackNumberRequest : IRequest
    {
        public PrintTrackNumberRequest(string number, string link, bool showPreview, string base64Pdf = null, byte[] bytesPdf = null)
        {
            Number = number;
            Link = link;
            ShowPreview = showPreview;
            Base64Pdf = base64Pdf;
            BytesPdf = bytesPdf;
        }

        public string Number { get; }

        public string Link { get; }

        public bool ShowPreview { get; }

        public string Base64Pdf { get; }

        public byte[] BytesPdf { get; }
    }
}