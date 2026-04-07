using System;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.ViewModels.Complaint;

namespace Telemart.Client.Factories.Complaint
{
    public class TradeInComplaintCreator : ComplaintCreatorBase
    {
        public TradeInComplaintCreator(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            ILogger<TradeInComplaintCreator> logger)
            : base(webClient, messageFacadeService, logger)
        {
        }

        public override async Task ShowViewAsync(
            IDocumentManagerService dialogDocumentManagerService,
            IDocumentManagerService nonModalDialogDocumentManagerService,
            ISupportServices supportServices)
        {
            string documentNumber = GetDocumentNumber("Введите номер Trade-In заявки", "Номер Trade-In заявки", dialogDocumentManagerService, supportServices);

            if (string.IsNullOrEmpty(documentNumber))
            {
                return;
            }

            if (int.TryParse(documentNumber, out int tradeInId))
            {
                try
                {
                    TradeInDto tradeIn = await WebClient.ExecuteApiRequestAsync(new QueryTradeIn(tradeInId));

                    ComplaintCreateParameter parameter = new ComplaintCreateParameter(
                        null,
                        null,
                        tradeIn.Id,
                        null,
                        new[] { new ComboBoxItem(tradeIn.ProductId ?? 0, tradeIn.ProductName) },
                        $"{tradeIn.LastName} {tradeIn.FirstName} {tradeIn.MiddleName}",
                        tradeIn.Phone,
                        null,
                        tradeIn.Email);

                    nonModalDialogDocumentManagerService.ShowView<ComplaintCreateViewModel>(parameter, supportServices);
                }
                catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    MessageFacadeService.ShowNotificationWarning($"Trade-In заявка №{tradeInId} не найдена");
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationWarning(Resources.ErrorDuringDataLoading);
                    Logger.LogError(exception, "Error while creating complaint from Trade-In request");
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Неверный номер Trade-In заявки");
            }
        }
    }
}