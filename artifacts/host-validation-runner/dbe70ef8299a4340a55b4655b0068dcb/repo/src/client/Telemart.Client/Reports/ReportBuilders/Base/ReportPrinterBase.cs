using System.Threading.Tasks;
using MediatR;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.Reports.ReportBuilders.Base
{
    public abstract class ReportPrinterBase<TData> : IReportPrinterBase<TData>
    where TData : ReportPrinterDataBase
    {
        protected ReportPrinterBase(
            IMessageFacadeService messageFacadeService,
            IWebClient webClient,
            IPrintingSettingsStore printingSettingsStore,
            IMediator mediator)
        {
            MessageFacadeService = messageFacadeService;
            WebClient = webClient;
            PrintingSettingsStore = printingSettingsStore;
            Mediator = mediator;
        }

        protected IMessageFacadeService MessageFacadeService { get; }

        protected IWebClient WebClient { get; }

        protected IPrintingSettingsStore PrintingSettingsStore { get; }

        protected IMediator Mediator { get; }

        public abstract Task PrintAsync(TData data);
    }
}