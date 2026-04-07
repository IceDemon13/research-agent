using System;
using System.Globalization;
using System.IO.Abstractions;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Reports.SmartPost;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business.Delivery.TrackNumberProviders
{
    public sealed class TelemartTrackNumberProvider : ITrackNumberProvider
    {
        public TelemartTrackNumberProvider(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IFileSystem fileSystem,
            IMediator mediator,
            IPrintingSettingsStore printingSettingsStore,
            ILogger<TelemartTrackNumberProvider> logger)
        {
            WebClient = webClient;
            MessageFacadeService = messageFacadeService;
            FileSystem = fileSystem;
            Mediator = mediator;
            PrintingSettingsStore = printingSettingsStore;
            Logger = logger;
        }

        public bool SupportTrackNumbers { get; } = true;

        public bool SupportGetDeliveryDate { get; } = false;

        private IWebClient WebClient { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IFileSystem FileSystem { get; }

        private IMediator Mediator { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private ILogger Logger { get; }

        public Task<string> CreateAsync(int orderId, PackageProperties package)
        {
            return Task.FromResult(orderId.ToString(CultureInfo.InvariantCulture));
        }

        public async Task PrintAsync(string trackNumber, bool showPreview)
        {
            try
            {
                int orderId = int.Parse(trackNumber);

                OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId));

                SmartPostTtnReportData[] reportData = new SmartPostTtnReportData[order.PackagePlaces];

                for (int i = 0; i < order.PackagePlaces; i++)
                {
                    reportData[i] = new SmartPostTtnReportData(
                           order.Id,
                           "Телемарт",
                           order.DeliveryTime.Value,
                           order.GetTotalAmount().Uah + order.PackageDeliveryCost,
                           order.DeliveryData.Street,
                           order.DeliveryData.House,
                           order.DeliveryData.Flat,
                           order.Fio,
                           order.Phone,
                           i + 1,
                           order.PackagePlaces);
                }

                SmartPostTtnReport report = new SmartPostTtnReport { DataSource = reportData };

                if (!FileSystem.Directory.Exists(ApplicationFolders.PrintedFolderPath))
                {
                    FileSystem.Directory.CreateDirectory(ApplicationFolders.PrintedFolderPath);
                }

                string filePath = FileSystem.Path.Combine(ApplicationFolders.PrintedFolderPath, $"ttn-{trackNumber}.pdf");

                report.ExportToPdf(filePath);

                PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

                await Mediator.Send(new PrintPdfFileRequest(filePath, printSettings.Sticker?.Name, printSettings.Sticker?.PaperSource, showPreview));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print TrackNumber: {TrackNumber}", trackNumber);
                MessageFacadeService.ShowNotificationError("Ошибка печати при печати ТТН");
            }
        }

        public Task TrackAsync(int orderId, string packageTtn)
        {
            MessageFacadeService.ShowNotificationWarning("Невозможно отследить ТТН для данного типа доставки");

            return Task.CompletedTask;
        }

        public Task<DateTime> GetDeliveryDateAsync(int carryTypeId, int citySenderId, int cityRecipientId, DateTime startDate)
        {
            throw new NotSupportedException();
        }
    }
}