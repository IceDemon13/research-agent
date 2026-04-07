using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.XtraReports;
using MediatR;
using Telemart.Client.Business.Order;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Mediator.RequestsHandlers
{
    internal sealed class PrintOrderDocumentHandler : IRequestHandler<PrintOrderDocumentRequest>
    {
        public PrintOrderDocumentHandler(
            IWebClient webClient,
            IOrderReportBuilder reportBuilder,
            IPrintingSettingsStore printingSettingsStore,
            IMessenger messenger,
            IReportPrintHelper reportPrintHelper)
        {
            WebClient = webClient;
            ReportBuilder = reportBuilder;
            PrintingSettingsStore = printingSettingsStore;
            Messenger = messenger;
            ReportPrintHelper = reportPrintHelper;
        }

        private IMessenger Messenger { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IOrderReportBuilder ReportBuilder { get; }

        private IWebClient WebClient { get; }

        private IReportPrintHelper ReportPrintHelper { get; }

        public async Task Handle(PrintOrderDocumentRequest request, CancellationToken cancellationToken)
        {
            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(request.OrderId));

            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
            Dictionary<int, string> cities = (await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync()).ToDictionary(x => x.Id, x => x.NameUkr);

            PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();

            string contractorName = contractors.FirstOrDefault(x => x.Id == order.ClientId)?.Name ?? string.Empty;
            cities.TryGetValue(order.CityId ?? 0, out string cityName);

            Messenger.Send(new OrderMessage(order, MessageType.Changed));

            switch (request.OrderDocumentTypeId)
            {
                case OrderDocumentType.ChequeId:
                    {
                        int[] productIds = (int[])request.Parameter;

                        if (printingSettings.ChequeFormat == PrintingSettingsChequeFormat.CheckTape.Id &&
                            printingSettings.Cheque != null)
                        {
                            IReport report = await ReportBuilder.BuildTapeChequeReportAsync(order, productIds, contractorName, cityName, request.TodayAsIssueDate);
                            ReportPrintHelper.Print(report, printingSettings.Cheque.Name, printingSettings.Cheque.PaperSource, request.Preview, request.Copies);
                        }
                        else
                        {
                            IReport report = await ReportBuilder.BuildChequeReportAsync(order, productIds, contractorName, cityName, request.TodayAsIssueDate);
                            ReportPrintHelper.Print(report, printingSettings.Main?.Name, printingSettings.Main?.PaperSource, request.Preview, request.Copies);
                        }

                        break;
                    }

                case OrderDocumentType.AcceptanceProtocolId:
                    {
                        IReport report = await ReportBuilder.BuildAcceptanceProtocolReportAsync(order, contractorName, cityName, request.TodayAsIssueDate);

                        if (printingSettings.InvoiceFormat == PrintingSettingsInvoiceFormat.TapeId)
                        {
                            ReportPrintHelper.Print(report, printingSettings.Cheque?.Name, printingSettings.Cheque?.PaperSource, request.Preview, request.Copies);
                        }
                        else
                        {
                             ReportPrintHelper.Print(report, printingSettings.Main?.Name, printingSettings.Main?.PaperSource, request.Preview, request.Copies);
                        }

                        break;
                    }
            }
        }
    }
}