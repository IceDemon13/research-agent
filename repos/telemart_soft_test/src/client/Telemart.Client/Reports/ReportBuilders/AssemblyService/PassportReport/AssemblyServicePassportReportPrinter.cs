using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.XtraReports;
using MediatR;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Fonts;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Reports.AssemblyService;
using Telemart.Client.Reports.ReportBuilders.Base;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Reports.ReportBuilders.AssemblyService.PassportReport
{
    public class AssemblyServicePassportReportPrinter : ReportPrinterBase<AssemblyServicePassportReportPrinterData>,  IAssemblyServicePassportReportPrinter
    {
        public AssemblyServicePassportReportPrinter(IMessageFacadeService messageFacadeService, IWebClient webClient, IPrintingSettingsStore printingSettingsStore, IMediator mediator)
            : base(messageFacadeService, webClient, printingSettingsStore, mediator)
        {
        }

        public override async Task PrintAsync(AssemblyServicePassportReportPrinterData data)
        {
            AssemblyServiceDto assemblyService = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyService(data.AssemblyServiceId));

            if (assemblyService.ProductId is null || assemblyService.CompletedOn is null)
            {
                return;
            }

            FontInstaller.AddGeometryFontToCommonApplicationData();

            FontInstaller.RegisterGeometriaFont();

            PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

            List<ProductDto> products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(new QueryProductByIdsDto(new[] { assemblyService.ProductId.Value }, Constants.TelemartContractorId)));

            ProductDto product = products.First();

            AssemblyServicePassportReportData reportData = new(product.NameUkr, assemblyService.NomenclatureSeries, assemblyService.CompletedOn.Value, product.WarrantyNameUkr, product.DescriptionShortUkr);

            IReport report = new AssemblyServicePassportReport { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = printSettings.Sticker != null
                ? new PrintReportRequest(report, data.ShowPreview, printSettings.Sticker.Name, printSettings.Sticker.PaperSource)
                : new PrintReportRequest(report, data.ShowPreview);

            await Mediator.Send(printRequest);
        }
    }
}