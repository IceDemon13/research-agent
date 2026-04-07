using System;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.XtraReports;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.Reports.Product;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class GenerateSerialNumbersViewModel : TelemartDialogViewModelBase
    {
        private int productId;

        public GenerateSerialNumbersViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IReportPrintHelper reportPrintHelper,
            IPrintingSettingsStore printingSettingsStore)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ReportPrintHelper = reportPrintHelper;
            PrintingSettingsStore = printingSettingsStore;
        }

        public GenerateSerialNumbersViewModel()
        {
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            private set { SetProperty(() => ProductName, value); }
        }

        public int? Count
        {
            get { return GetProperty(() => Count); }
            set { SetProperty(() => Count, value); }
        }

        public short Copies
        {
            get { return GetProperty(() => Copies); }
            set { SetProperty(() => Copies, value); }
        }

        private IReportPrintHelper ReportPrintHelper { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        public static void BuildMetadata(MetadataBuilder<GenerateSerialNumbersViewModel> builder)
        {
            builder.Property(x => x.Count).Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            GenerateSerialNumbersParameter parameter = (GenerateSerialNumbersParameter)Parameter;

            productId = parameter.ProductId;
            ProductName = parameter.ProductName;

            Copies = 1;

            Title = "Генерировать SN";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                PrintingSettingsInfo settings = await PrintingSettingsStore.LoadAsync();

                if (string.IsNullOrEmpty(settings.SerialNumber?.Name))
                {
                    MessageFacadeService.ShowNotificationError("Задайте принтер серийных номеров в настройках");
                    return;
                }

                if (!MessageFacadeService.Confirm("Вы подтверждаете создание SN?"))
                {
                    return;
                }

                Result<ProductSerialNumbersDto> result = await WebClient.ExecuteApiRequestAsync(new CreateProductSerialNumbers(productId, Count.Value));

                IReport report = new SerialNumberReport
                {
                    DataSource = result.Data.SerialNumbers.Select(x => new SerialNumberReportData(x)).ToArray()
                };

                ReportPrintHelper.Print(report, settings.SerialNumber.Name, settings.SerialNumber.PaperSource, false, Copies);

                MessageFacadeService.ShowNotificationInfo("Серийные номера успешно напечатаны");

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при генерации серийных номеров");
                ShowValidationResultView("Ошибки при генерации серийных номеров", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to generate serial numbers");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка генерации серийных номеров");
                Logger.LogError(exception, "Error while generating serial numbers");
            }
        }
    }
}