using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.MeestExpress;
using Telemart.Client.Data.Requests.Features.MeestExpress.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.MeestExpress;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Business.Delivery.TrackNumberProviders
{
    public sealed class MeTrackNumberProvider : ITrackNumberProvider
    {
        public MeTrackNumberProvider(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IMediator mediator,
            ILogger<MeTrackNumberProvider> logger)
        {
            WebClient = webClient;
            MessageFacadeService = messageFacadeService;
            Mediator = mediator;
            Logger = logger;
        }

        public bool SupportTrackNumbers { get; } = true;

        public bool SupportGetDeliveryDate { get; } = false;

        private IWebClient WebClient { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IMediator Mediator { get; }

        private ILogger<MeTrackNumberProvider> Logger { get; }

        public async Task<string> CreateAsync(int orderId, PackageProperties package)
        {
            try
            {
                MeDocumentCreateDto dto = new MeDocumentCreateDto(
                    orderId,
                    package.PlaceCount,
                    package.TotalWeight,
                    package.TotalInsurance,
                    package.Places.Select(x => new MeDocumentPlaceCreateDto(x.Weight, x.Insurance)).ToArray());

                CreateMeTtnByOrder request = new CreateMeTtnByOrder(dto);

                Result<MeDocumentDto> result = await WebClient.ExecuteApiRequestAsync(request);

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
                MeDocumentDto meDocument = await WebClient.ExecuteApiRequestAsync(new QueryMeDocument(trackNumber));

                await Mediator.Send(new PrintTrackNumberRequest(meDocument.Ttn, meDocument.Link, showPreview));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print TTN: {TTN}", trackNumber);
                MessageFacadeService.ShowNotificationError("Ошибка при печати ТТН");
            }
        }

        public async Task TrackAsync(int orderId, string packageTtn)
        {
            try
            {
                Result<List<MeTrackDto>> result = await WebClient.ExecuteApiRequestAsync(new TrackMeDocument(packageTtn));

                StringBuilder sb = new();

                sb.Append($"ТТН: {packageTtn}\n");

                foreach (MeTrackDto item in result.Data)
                {
                    sb.AppendLine();
                    sb.AppendLine($"{item.TimeStamp:dd.MM.yyyy HH:mm} {item.Description.DescriptionRu}\nДетали: {item.DescriptionDetail.DescriptionRu}\n");
                }

                MessageFacadeService.ShowMessageBox(
                    sb.ToString(),
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