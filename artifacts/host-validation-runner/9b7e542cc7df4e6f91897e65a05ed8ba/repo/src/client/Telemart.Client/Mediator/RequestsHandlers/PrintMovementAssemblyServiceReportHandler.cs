using System.Threading;
using System.Threading.Tasks;
using DevExpress.XtraReports;
using MediatR;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Reports.AssemblyService;

namespace Telemart.Client.Mediator.RequestsHandlers
{
    public sealed class PrintMovementAssemblyServiceReportHandler : IRequestHandler<PrintMovementAssemblyServiceReportRequest>
    {
        private readonly IMediator _mediator;
        private readonly IPrintingSettingsStore _printingSettingsStore;

        public PrintMovementAssemblyServiceReportHandler(IMediator mediator, IPrintingSettingsStore printingSettingsStore)
        {
            _mediator = mediator;
            _printingSettingsStore = printingSettingsStore;
        }

        public async Task Handle(PrintMovementAssemblyServiceReportRequest request, CancellationToken cancellationToken)
        {
            PrintingSettingsInfo printSettings = await _printingSettingsStore.LoadAsync();

            IReport report;

            AssemblyServiceMovementReportData[] reportsData = new AssemblyServiceMovementReportData[request.Places];

            for (int i = 0; i < request.Places; i++)
            {
                reportsData[i] = new AssemblyServiceMovementReportData(
                    request.OrderId,
                    i + 1,
                    request.Places,
                    request.CountProducts,
                    request.AssemblyServiceId);
            }

            report = new AssemblyServiceMovementReport { DataSource = reportsData };

            PrintReportRequest printRequest = printSettings?.Sticker != null
                ? new PrintReportRequest(report, true, printSettings.Sticker.Name, printSettings.Sticker.PaperSource)
                : new PrintReportRequest(report, true);

            await _mediator.Send(printRequest);
        }
    }
}