using System.Threading.Tasks;
using System.Windows.Media;
using DevExpress.Mvvm;
using MediatR;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Reports;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public class ShowTextViewModel : TelemartDialogViewModelBase
    {
        private readonly IMediator mediator;

        private int reportWidth;
        private PrinterSettingsInfo printer;

        public ShowTextViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMediator mediator)
            : base(webClient, dictionaries, messageFacadeService)
        {
            PrintCommand = new AsyncCommand(PrintAsync, () => PrintButtonVisible);

            this.mediator = mediator;
        }

        public IDelegateCommand PrintCommand { get; }

        public FontFamily Font
        {
            get { return GetProperty(() => Font); }
            private set { SetProperty(() => Font, value); }
        }

        public string Body
        {
            get { return GetProperty(() => Body); }
            private set { SetProperty(() => Body, value); }
        }

        public bool PrintButtonVisible
        {
            get { return GetProperty(() => PrintButtonVisible); }
            private set { SetProperty(() => PrintButtonVisible, value); }
        }

        public async Task PrintAsync()
        {
            TextReportData data = new(Body, reportWidth);

            TextReport report = new TextReport
            {
                DataSource = new[] { data }
            };

            Mediator.Requests.PrintReportRequest request = new(report, false, printer.Name, printer.PaperSource);

            await mediator.Send(request);
        }

        protected override Task HandleLoadedAsync()
        {
            ShowTextParameter parameter = (ShowTextParameter)Parameter;

            Title = parameter.Caption;
            Body = parameter.Body;

            reportWidth = parameter.ReportWidth;
            printer = parameter.Printer;

            PrintButtonVisible = printer != null;

            if (parameter.Monospace)
            {
                Font = new FontFamily("Courier New");
            }

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }
    }
}