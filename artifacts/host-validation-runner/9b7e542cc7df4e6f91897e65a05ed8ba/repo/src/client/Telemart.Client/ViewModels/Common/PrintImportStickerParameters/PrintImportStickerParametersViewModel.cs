using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Reports.Product;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common.PrintImportStickerParameters
{
    public sealed class PrintImportStickerParametersViewModel : TelemartDialogViewModelBase
    {
        public PrintImportStickerParametersViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public decimal Count
        {
            get { return GetProperty(() => Count); }
            set { SetProperty(() => Count, value); }
        }

        public BarcodeReportFormat Format
        {
            get { return GetProperty(() => Format); }
            set { SetProperty(() => Format, value); }
        }

        public BarcodeReportFormat[] Formats
        {
            get { return GetProperty(() => Formats); }
            set { SetProperty(() => Formats, value); }
        }

        public static void BuildMetadata(MetadataBuilder<PrintImportStickerParametersViewModel> builder)
        {
            builder.Property(x => x.Count)
                .MatchesRule(x => x >= 1 && x <= 1000, () => "Допустимые значения 1...1000");
        }

        protected override Task HandleLoadedAsync()
        {
            PrintImportStickerParametersParameter parameter = (PrintImportStickerParametersParameter)Parameter;

            Formats = new[] { BarcodeReportFormat.Barcode50X40 };

            Count = parameter.Count;
            Format = BarcodeReportFormat.Barcode50X40;

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();

            return Task.CompletedTask;
        }
    }
}