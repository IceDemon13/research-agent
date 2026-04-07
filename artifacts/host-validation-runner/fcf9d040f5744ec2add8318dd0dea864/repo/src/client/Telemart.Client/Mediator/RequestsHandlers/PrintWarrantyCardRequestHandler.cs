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
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Mediator.RequestsHandlers
{
    internal sealed class PrintWarrantyCardRequestHandler : IRequestHandler<PrintWarrantyCardRequest>
    {
        private const bool TodayAsIssueDate = false;

        public PrintWarrantyCardRequestHandler(
            IWebClient webClient,
            IDictionaries dictionaries,
            IOrderReportBuilder reportBuilder,
            IPrintingSettingsStore printingSettingsStore,
            IMessenger messenger,
            IReportPrintHelper reportPrintHelper)
        {
            WebClient = webClient;
            Dictionaries = dictionaries;
            ReportBuilder = reportBuilder;
            PrintingSettingsStore = printingSettingsStore;
            Messenger = messenger;
            ReportPrintHelper = reportPrintHelper;
        }

        private IMessenger Messenger { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IOrderReportBuilder ReportBuilder { get; }

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        private IReportPrintHelper ReportPrintHelper { get; }

        public async Task Handle(PrintWarrantyCardRequest request, CancellationToken cancellationToken)
        {
            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(request.OrderId));

            PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();

            IDictionary<int, string> warranties = Dictionaries.GetItems<Warranty>().ToDictionary(x => x.Id, x => x.NameUa);

            Messenger.Send(new OrderMessage(order, MessageType.Changed));

            int[] orderProductsIds = null;

            if (request.ProductIds != null)
            {
                orderProductsIds = order.Products.Where(x => request.ProductIds.Contains(x.Product.Id)).Select(x => x.Id).ToArray();
            }

            IReport report = await ReportBuilder.BuildWarrantyCardReportAsync(
                order,
                orderProductsIds,
                warranties,
                TodayAsIssueDate,
                request.SeparateWarrantyCards ?? order.Options?.SeparateWarrantyCards ?? false);

            ReportPrintHelper.Print(report, printingSettings.WarrantyCard?.Name, printingSettings.WarrantyCard?.PaperSource, request.ShowPreview);
        }
    }
}