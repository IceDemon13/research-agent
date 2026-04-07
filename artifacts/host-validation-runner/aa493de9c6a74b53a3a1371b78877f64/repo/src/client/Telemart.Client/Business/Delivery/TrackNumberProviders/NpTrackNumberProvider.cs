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
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Novaposhta.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Business.Delivery.TrackNumberProviders
{
    public sealed class NpTrackNumberProvider : ITrackNumberProvider
    {
        private readonly ILogger _logger;

        public NpTrackNumberProvider(
            IWebClient webClient,
            IMediator mediator,
            IMessageFacadeService messageFacadeService,
            ILogger<NpTrackNumberProvider> logger)
        {
            WebClient = webClient;
            Mediator = mediator;
            MessageFacadeService = messageFacadeService;
            _logger = logger;
        }

        public bool SupportTrackNumbers { get; } = true;

        public bool SupportGetDeliveryDate { get; } = true;

        public string Phone { get; set; }

        private IWebClient WebClient { get; }

        private IMediator Mediator { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        public async Task<string> CreateAsync(int orderId, PackageProperties package)
        {
            try
            {
                PackagePlaceProperties placeProperties = package.Places.First();

                CreateNpTtnByOrder request = new(orderId, package.PlaceCount, package.TotalWeight, placeProperties.Width, placeProperties.Length, placeProperties.Height, package.AddToApplication);

                Result<NpDocumentDto> result = await WebClient.ExecuteApiRequestAsync(request);

                return result.Data.Id;
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
                NpDocumentDto npDocument = await WebClient.ExecuteApiRequestAsync(new QueryNpDocument(trackNumber));

                await Mediator.Send(new PrintTrackNumberRequest(npDocument.Id, npDocument.Link, showPreview));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to print TTN");
                MessageFacadeService.ShowNotificationError("Ошибка при печати ТТН");
            }
        }

        public async Task TrackAsync(int orderId, string packageTtn)
        {
            try
            {
                Result<List<NewPostDocumentDto>> result = await WebClient.ExecuteApiRequestAsync(new TrackNpDocument(packageTtn, Phone));

                string message = string.Join(Environment.NewLine, result.Data.Select(x => $"{x.Number}: {x.Status} {x.RecipientDateTime} {x.RecipientFullName}"));

                MessageFacadeService.ShowMessageBox(
                    message,
                    Resources.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
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
            finally
            {
                Phone = null;
            }
        }

        public async Task<DateTime> GetDeliveryDateAsync(int carryType, int citySenderId, int cityRecipientId, DateTime startDate)
        {
            NewPostGetDeliveryDateRequest request = new(
                citySenderId,
                cityRecipientId,
                carryType,
                startDate);

            NewPostGetDeliveryDateResponse response = await WebClient.ExecuteApiRequestAsync(new QueryNewPostDocumentDeliveryDate(request));

            return response.DeliveryDate;
        }
    }
}