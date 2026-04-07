using System.Threading;
using System.Threading.Tasks;
using DevExpress.XtraReports;
using MediatR;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Reports.AdditionalServiceProduct;

namespace Telemart.Client.Mediator.RequestsHandlers
{
    public sealed class PrintAdditionalServiceBarcodeReportHandler : IRequestHandler<PrintAdditionalServiceBarcodeReportRequest>
    {
        private readonly IMediator _mediator;
        private readonly IPrintingSettingsStore _printingSettingsStore;

        public PrintAdditionalServiceBarcodeReportHandler(IMediator mediator, IPrintingSettingsStore printingSettingsStore)
        {
            _mediator = mediator;
            _printingSettingsStore = printingSettingsStore;
        }

        public async Task Handle(PrintAdditionalServiceBarcodeReportRequest request, CancellationToken cancellationToken)
        {
            PrintingSettingsInfo printSettings = await _printingSettingsStore.LoadAsync();

            AdditionalServiceProductMovementReportData reportData = new(request.AdditionalServiceProductId, request.OrderId, request.OrderDeliveryTimeTo, request.CompletedOn);

            IReport report = new AdditionalServiceProductMovementReport { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = printSettings?.Sticker != null
                ? new PrintReportRequest(report, true, printSettings.Sticker.Name, printSettings.Sticker.PaperSource)
                : new PrintReportRequest(report, true);

            await _mediator.Send(printRequest, cancellationToken);
        }
    }
}