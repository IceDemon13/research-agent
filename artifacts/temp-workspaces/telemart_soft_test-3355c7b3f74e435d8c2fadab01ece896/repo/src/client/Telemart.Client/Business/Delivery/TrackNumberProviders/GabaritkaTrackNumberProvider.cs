using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.Gabaritka;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Gabaritka;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Business.Delivery.TrackNumberProviders
{
    public class GabaritkaTrackNumberProvider : ITrackNumberProvider
    {
        public GabaritkaTrackNumberProvider(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            ILogger<GabaritkaTrackNumberProvider> logger)
        {
            WebClient = webClient;
            MessageFacadeService = messageFacadeService;
            Logger = logger;
        }

        public bool SupportTrackNumbers { get; } = true;

        public bool SupportGetDeliveryDate { get; } = false;

        private IWebClient WebClient { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private ILogger<GabaritkaTrackNumberProvider> Logger { get; }

        public async Task<string> CreateAsync(int orderId, PackageProperties package)
        {
            try
            {
                GabaritkaDocumentCreateDto dto = new(orderId, package.Places.Select(x => new GabaritkaDocumentPlaceCreateDto(x.Weight, x.Insurance)).ToList());

                CreateGabaritkaTtnByOrder request = new CreateGabaritkaTtnByOrder(dto);

                Result<GabaritkaDocumentDto> result = await WebClient.ExecuteApiRequestAsync(request);

                return result.Data.Ttn;
            }
            catch (UnexpectedSatusException exception)
            {
                throw new CreateTrackNumberException(exception.GetErrorItems().Select(x => x.Message).ToArray(), exception);
            }
            catch (UnexpectedErrorException exception)
            {
                throw new CreateTrackNumberException(new[] { Resources.ServerUnavailable }, exception);
            }
        }

        public async Task PrintAsync(string trackNumber, bool showPreview)
        {
            if (string.IsNullOrWhiteSpace(trackNumber))
            {
                MessageFacadeService.ShowNotificationWarning("ТТН не заполнена");
                return;
            }

            try
            {
                GabaritkaDocumentDto document = await WebClient.ExecuteApiRequestAsync(new QueryGabaritkaDocument(trackNumber));

                ProcessHelper.Start(document.Link);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print TTN. TrackNumber: {TTN}", trackNumber);
                MessageFacadeService.ShowNotificationError("Ошибка при печати ТТН");
            }
        }

        public async Task TrackAsync(int orderId, string packageTtn)
        {
            try
            {
                Result<GabaritkaTrackNumberDto> result = await WebClient.ExecuteApiRequestAsync(new TrackGabaritkaDocument(packageTtn));

                string info = $@"
ТТН: {packageTtn}
Статус: {result.Data.OrderStatusName}
Вес: {result.Data.Weight}
Прибытие ОТ: {result.Data.DateFrom1}
Прибытие ДО: {result.Data.DateFrom2}
Прибытие ДО (дата): {result.Data.DateTo}
Кол-во мест: {result.Data.Pieces}";

                MessageFacadeService.ShowMessageBox(
                    info,
                    Resources.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (UnexpectedSatusException exception)
            {
                string message = string.Join(Environment.NewLine, exception.GetErrorItems().Select(x => x.Message));

                MessageFacadeService.ShowMessageBox(
                    message,
                    Resources.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, $"{Resources.ErrorDuringDataLoading}. {Resources.ServerUnavailable}");
                MessageFacadeService.ShowNotificationError($"{Resources.ErrorDuringDataLoading}. {Resources.ServerUnavailable}");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, $"{Resources.ErrorDuringDataLoading}. {Resources.ServerUnavailable}");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        public Task<DateTime> GetDeliveryDateAsync(int carryTypeId, int citySenderId, int cityRecipientId, DateTime startDate)
        {
            throw new NotSupportedException();
        }
    }
}