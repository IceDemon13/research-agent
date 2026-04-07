using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.IO;
using Telemart.Client.Mediator.Requests;

namespace Telemart.Client.Mediator.RequestsHandlers
{
    public sealed class PrintA4PdfHandler : PrintDocumentRequestHandlerBase, IRequestHandler<PrintA4PdfRequest>
    {
        private readonly IPrintingSettingsStore _printingSettingsStore;

        public PrintA4PdfHandler(
            IFileSystem fileSystem,
            IFileDownloader fileDownloader,
            IMediator mediator,
            IPrintingSettingsStore printingSettingsStore)
            : base(fileSystem, fileDownloader, mediator)
        {
            _printingSettingsStore = printingSettingsStore;
        }

        public async Task Handle(PrintA4PdfRequest request, CancellationToken cancellationToken)
        {
            PrintingSettingsInfo printingSettingsInfo = await _printingSettingsStore.LoadAsync();

            await PrintAsync(null, $"{request.Name}.pdf", printingSettingsInfo.Main, request.ShowPreview, null, request.BytesPdf);
        }
    }
}