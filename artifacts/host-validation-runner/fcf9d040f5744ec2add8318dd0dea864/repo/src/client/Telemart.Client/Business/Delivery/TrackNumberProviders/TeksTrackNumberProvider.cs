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
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.Requests.Features.Teks;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Teks;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Business.Delivery.TrackNumberProviders
{
    public class TeksTrackNumberProvider : ITrackNumberProvider
    {
        private readonly ILogger _logger;
        private readonly IWebClient _webClient;
        private readonly IMessageFacadeService _messageFacadeService;
        private readonly IMediator _mediator;

        public TeksTrackNumberProvider(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IMediator mediator,
            ILogger<TeksTrackNumberProvider> logger)
        {
            _logger = logger;
            _mediator = mediator;
            _webClient = webClient;
            _messageFacadeService = messageFacadeService;
        }

        public bool SupportTrackNumbers => true;

        public bool SupportGetDeliveryDate => false;

        public bool InvoiceTtn { get; set; }

        public Task<string> CreateAsync(int id, PackageProperties package)
        {
            throw new NotSupportedException();
        }

        public async Task PrintAsync(string trackNumber, bool showPreview)
        {
            if (string.IsNullOrWhiteSpace(trackNumber))
            {
                _messageFacadeService.ShowNotificationWarning("ТТН не заполнена");
                return;
            }

            try
            {
                string[] trackNumbers = trackNumber.Split(",");

                List<Task<TeksDocumentDto>> resultTermoPrintTasks = trackNumbers.Select(x => _webClient.ExecuteApiRequestAsync(GetFuncPrint(x))).ToList();

                await Task.WhenAll(resultTermoPrintTasks);

                foreach (TeksDocumentDto teksDocument in resultTermoPrintTasks.Select(x => x.Result))
                {
                    await _mediator.Send(GetRequestPrint(teksDocument, showPreview));
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed print TTN");
                _messageFacadeService.ShowNotificationError("Ошибка при печати ТТН");
            }

            InvoiceTtn = false;
        }

        public async Task TrackAsync(int id, string packageTtn)
        {
            try
            {
                TeksDocumentDto teksDocumentDto = await _webClient.ExecuteApiRequestAsync(new QueryTeksTtnInfo(packageTtn));

                Result<TtnStatusDto[]> resultTrack = await _webClient.ExecuteApiRequestAsync(new GetInfoTeksDocuments(packageTtn));

                if (resultTrack.IsSuccess && resultTrack.Data?.Length > 0)
                {
                    StringBuilder builder = new StringBuilder(100);

                    foreach (var ttnStatus in resultTrack.Data)
                    {
                        string date = ttnStatus.StatusDateTime;

                        builder.AppendLine($"{ttnStatus.StatusName} ({date})");
                    }

                    _messageFacadeService.ShowMessageBox(
                        builder.ToString(),
                        $"История ТТН: {teksDocumentDto.Id}",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (UnexpectedSatusException exception)
            {
                string message = string.Join(Environment.NewLine, exception.GetErrorItems().Select(x => x.Message));

                _messageFacadeService.ShowMessageBox(
                    message,
                    Resources.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (UnexpectedErrorException exception)
            {
                _logger.LogError(exception, $"{Resources.ErrorDuringDataLoading}. {Resources.ServerUnavailable}");
                _messageFacadeService.ShowNotificationError(
                    $"{Resources.ErrorDuringDataLoading}. {Resources.ServerUnavailable}");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"{Resources.ErrorDuringDataLoading}. {Resources.ServerUnavailable}");
                _messageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        public Task<DateTime> GetDeliveryDateAsync(int carryTypeId, int citySenderId, int cityRecipientId, DateTime startDate)
        {
            throw new NotSupportedException();
        }

        private QueryEntityRequestBase<TeksDocumentDto> GetFuncPrint(string ttn)
        {
            return InvoiceTtn ? new QueryTeksDocumentForPrint(ttn) : new QueryTeksDocumentForTermoPrint(ttn);
        }

        private IRequest GetRequestPrint(TeksDocumentDto teksDocument, bool showPreview)
        {
            return InvoiceTtn
                ? new PrintA4PdfRequest($"teks-{teksDocument.Id}", showPreview, teksDocument.BytePdf)
                : new PrintTrackNumberRequest(teksDocument.Id, null, showPreview, bytesPdf: teksDocument.BytePdf);
        }
    }
}