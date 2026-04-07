using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.IO;
using Telemart.Client.Mediator.Requests;

namespace Telemart.Client.Mediator.RequestsHandlers
{
    public sealed class PrintTrackNumberRequestHandler : PrintDocumentRequestHandlerBase, IRequestHandler<PrintTrackNumberRequest>
    {
        private readonly IPrintingSettingsStore _printingSettingsStore;

        public PrintTrackNumberRequestHandler(
            IFileSystem fileSystem,
            IFileDownloader fileDownloader,
            IMediator mediator,
            IPrintingSettingsStore printingSettingsStore)
            : base(fileSystem, fileDownloader, mediator)
        {
            _printingSettingsStore = printingSettingsStore;
        }

        public async Task Handle(PrintTrackNumberRequest request, CancellationToken cancellationToken)
        {
            PrintingSettingsInfo printingSettingsInfo = await _printingSettingsStore.LoadAsync();

            await PrintAsync(request.Link, $"ttn-{request.Number}.pdf", printingSettingsInfo.Sticker, request.ShowPreview, request.Base64Pdf, request.BytesPdf);
        }
    }
}