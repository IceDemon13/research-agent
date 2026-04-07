using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.XtraReports.UI;
using MediatR;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core;
using Telemart.Client.Data.Requests;
using Telemart.Client.Data.Requests.Features.ScanSheets;
using Telemart.Client.Dictionaries;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Reports.OrderPack;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store.CreateScanSheet;
using Telemart.Client.ViewModels.Store.CreateScanSheetForEntities;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Business.Delivery.ScanSheets
{
    internal sealed class TelemartScanSheetProcessor : IScanSheetProcessor
    {
        public TelemartScanSheetProcessor(
            IMediator mediator,
            IMessageFacadeService messageFacadeService,
            IPrintingSettingsStore printingSettingsStore,
            IFileSystem fileSystem)
        {
            Mediator = mediator;
            MessageFacadeService = messageFacadeService;
            PrintingSettings = printingSettingsStore;
            FileSystem = fileSystem;
        }

        private IMediator Mediator { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IPrintingSettingsStore PrintingSettings { get; }

        private IFileSystem FileSystem { get; }

        public bool Validate(CreateScanSheetModel model)
        {
            if (model.Orders.Any(x => x.CourierEmployeeId is null)
                && !MessageFacadeService.Confirm("В реестре присутствуют заказы в которых не указан курьер, продолжить?"))
            {
                return false;
            }

            return true;
        }

        public async Task PrintAsync(CreateScanSheetModel model)
        {
            ScanSheetOrderReportData[] orders = model.Orders
                .Select(x => new ScanSheetOrderReportData(x.Carry.Id == CarryType.GabaritkaId ? $"T{x.Id}" : x.Id.ToString(), x.DeliveryTime, x.Fio, x.Phone, x.Phone2, x.Address, x.Comment, x.PriceToCost.Uah, x.PriceToCost.Usd, x.PackagePlaces))
                .ToArray();

            ScanSheetReportData reportData = new ScanSheetReportData(orders);

            XtraReport report = new ScanSheetReport { DataSource = new[] { reportData } };

            if (!FileSystem.Directory.Exists(ApplicationFolders.PrintedFolderPath))
            {
                FileSystem.Directory.CreateDirectory(ApplicationFolders.PrintedFolderPath);
            }

            string filePath = FileSystem.Path.Combine(ApplicationFolders.PrintedFolderPath, $"register-{Guid.NewGuid():N}.pdf");

            await report.ExportToPdfAsync(filePath);

            PrintingSettingsInfo printSettings = await PrintingSettings.LoadAsync();

            PrintReportRequest printRequest = printSettings.Main != null
                ? new PrintReportRequest(report, true, printSettings.Main.Name, printSettings.Main.PaperSource)
                : new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }

        public IRestClientGatewayRequest<Result<ScanSheetCreateResponse[]>> CreateRequest(int[] orderIds)
        {
            return new ScanSheetNotifyCustomers(new ScanSheetNotifyDto(orderIds));
        }

        public Task PrintAsync(CreateScanSheetForEntitiesModel model)
        {
            throw new System.NotImplementedException();
        }

        public IRestClientGatewayRequest<Result<ScanSheetCreateResponse[]>> CreateEntityRequest(int entityType, int[] entityIds)
        {
            throw new System.NotImplementedException();
        }
    }
}