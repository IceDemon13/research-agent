using System.Threading.Tasks;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Reports.Product;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common.PrintBarcodeParameters
{
    public sealed class PrintBarcodeParametersViewModel : TelemartDialogViewModelBase
    {
        public PrintBarcodeParametersViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IPrintingSettingsStore printingSettingsStore)
            : base(webClient, dictionaries, messageFacadeService)
        {
            PrintingSettingsStore = printingSettingsStore;
        }

        public decimal Count
        {
            get { return GetProperty(() => Count); }
            set { SetProperty(() => Count, value); }
        }

        public decimal Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public BarcodeReportFormat Format
        {
            get { return GetProperty(() => Format); }
            set { SetProperty(() => Format, value); }
        }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        protected override async Task HandleLoadedAsync()
        {
            PrintBarcodeParametersParameter parameter = (PrintBarcodeParametersParameter)Parameter;

            int barcodeFormat = (await PrintingSettingsStore.LoadAsync()).BarcodeFormat ?? 0;

            Count = parameter.Copies;
            Quantity = parameter.Quantity;
            Format = barcodeFormat == PrintingSettingsBarcodeFormat.Barcode30X20.Id
                ? BarcodeReportFormat.Barcode30X20
                : BarcodeReportFormat.Barcode50X40;
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();

            return Task.CompletedTask;
        }
    }
}