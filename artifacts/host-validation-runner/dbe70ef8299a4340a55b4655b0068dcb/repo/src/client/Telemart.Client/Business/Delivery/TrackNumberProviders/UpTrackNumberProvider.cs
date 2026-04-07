using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Ukrposhta;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Ukrposhta;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Business.Delivery.TrackNumberProviders
{
    public class UpTrackNumberProvider : ITrackNumberProvider
    {
        private ILogger _logger;

        public UpTrackNumberProvider(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IMediator mediator,
            ILogger<UpTrackNumberProvider> logger)
        {
            WebClient = webClient;
            MessageFacadeService = messageFacadeService;
            Mediator = mediator;
            _logger = logger;
        }

        private IWebClient WebClient { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IMediator Mediator { get; }

        public async Task<string> CreateAsync(int orderId, PackageProperties package)
        {
            try
            {
                UkrposhtaTtnCreateByOrderDto dto = new UkrposhtaTtnCreateByOrderDto(
                    orderId,
                    package.TotalWeight,
                    package.Places.Max(x => x.Length ?? 0),
                    package.Fragile,
                    package.Places.Select(MapToDto).ToList());

                CreateUpTtnByOrder request = new(dto);

                Result<UpDocumentDto> result = await WebClient.ExecuteApiRequestAsync(request);

                return result.Data.Barcode;
            }
            catch (UnexpectedSatusException exception)
            {
                throw new CreateTrackNumberException(exception.GetErrorItems().Select(x => x.Message).ToArray(), exception);
            }
            catch (UnexpectedErrorException exception)
            {
                throw new CreateTrackNumberException(new[] { Resources.ServerUnavailable }, exception);
            }

            UpDocumentPackagePlaceDto MapToDto(PackagePlaceProperties source)
            {
                return new UpDocumentPackagePlaceDto(source.Weight, source.Length ?? 0, source.Width ?? 0, source.Height ?? 0);
            }
        }

        public bool SupportTrackNumbers { get; } = true;

        public bool SupportGetDeliveryDate { get; } = false;

        public async Task PrintAsync(string barcode, bool showPreview)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                MessageFacadeService.ShowNotificationWarning("ТТН не заполнена");
                return;
            }

            try
            {
                UpDocumentDto upDocument = await WebClient.ExecuteApiRequestAsync(new QueryUpDocument(barcode));

                await Mediator.Send(new PrintTrackNumberRequest(upDocument.Id, null, showPreview, upDocument.Base64Pdf));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed print TTN");
                MessageFacadeService.ShowNotificationError("Ошибка при печати ТТН");
            }
        }

        public async Task TrackAsync(int orderId, string barcode)
        {
            try
            {
                Result<List<UpActionsStatusDto>> result = await WebClient.ExecuteApiRequestAsync(new TrackUpDocument(barcode));

                string message = string.Join(Environment.NewLine, result.Data.Select(x => $"{x.Date}: {x.EventName} {x.EventReason}"));

                MessageFacadeService.ShowMessageBox(
                    message,
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
                _logger.LogError(exception, $"{Resources.ErrorDuringDataLoading}. {Resources.ServerUnavailable}");
                MessageFacadeService.ShowNotificationError($"{Resources.ErrorDuringDataLoading}. {Resources.ServerUnavailable}");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"{Resources.ErrorDuringDataLoading}. {Resources.ServerUnavailable}");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        public Task<DateTime> GetDeliveryDateAsync(int carryTypeId, int citySenderId, int cityRecipientId, DateTime startDate)
        {
            throw new NotSupportedException();
        }
    }
}