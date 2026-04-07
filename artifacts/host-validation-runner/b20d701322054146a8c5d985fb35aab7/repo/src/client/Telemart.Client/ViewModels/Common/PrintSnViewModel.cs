using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.XtraReports;
using Telemart.Client.Business.Order;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.Reports.Product;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class PrintSnViewModel : TelemartDialogViewModelBase
    {
        private readonly IReportPrintHelper _reportPrintHelper;
        private readonly IPrintingSettingsStore _printingSettingsStore;
        private readonly IOrderRules _orderRules;

        public PrintSnViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IReportPrintHelper reportPrintHelper,
            IPrintingSettingsStore printingSettingsStore,
            IOrderRules orderRules)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _reportPrintHelper = reportPrintHelper;
            _printingSettingsStore = printingSettingsStore;
            _orderRules = orderRules;

            Quantity = 1;
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public static void BuildMetadata(MetadataBuilder<PrintSnViewModel> builder)
        {
            builder.Property(x => x.Quantity)
                .MatchesRule(x => x is > 0 and <= 100, () => "Кол-во должно быть в диапазоне 1..100");

            builder.Property(x => x.SerialNumber)
                .MatchesInstanceRule((x, y) => !string.IsNullOrWhiteSpace(x) && y._orderRules.ValidateSerialNumber(x, null) == null, (x, y) => y._orderRules.ValidateSerialNumber(x, null));
        }

        protected override Task HandleLoadedAsync()
        {
            Title = "Печать SN";

            return base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            PrintingSettingsInfo settings = await _printingSettingsStore.LoadAsync();

            if (string.IsNullOrEmpty(settings.SerialNumber?.Name))
            {
                MessageFacadeService.ShowNotificationError("Задайте принтер серийных номеров в настройках");
                return;
            }

            IReport report = new SerialNumberReport
            {
                DataSource = new[] { new SerialNumberReportData(SerialNumber.ToUpperInvariant()) }
            };

            _reportPrintHelper.Print(
                report,
                settings.SerialNumber.Name,
                settings.SerialNumber.PaperSource,
                false,
                (short)Quantity);

            MessageFacadeService.ShowNotificationInfo("Серийный номер успешно напечатан");

            SerialNumber = null;
        }
    }
}