using System;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Complaint;

namespace Telemart.Client.Factories.Complaint
{
    public class ServiceRequestComplaintCreator : ComplaintCreatorBase
    {
        public ServiceRequestComplaintCreator(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            ILogger<ServiceRequestComplaintCreator> logger)
            : base(webClient, messageFacadeService, logger)
        {
        }

        public override async Task ShowViewAsync(
            IDocumentManagerService dialogDocumentManagerService,
            IDocumentManagerService nonModalDialogDocumentManagerService,
            ISupportServices supportServices)
        {
            string documentNumber = GetDocumentNumber("Введите номер заявки", "Номер сервисной заявки", dialogDocumentManagerService, supportServices);

            if (string.IsNullOrEmpty(documentNumber))
            {
                return;
            }

            if (int.TryParse(documentNumber, out int serviceRequestId))
            {
                try
                {
                    ServiceRequestDto serviceRequest = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(serviceRequestId));

                    ComplaintCreateParameter parameter = new ComplaintCreateParameter(
                        serviceRequest.OrderId,
                        serviceRequest.Id,
                        null,
                        serviceRequest.ContractorId,
                        new[] { new ComboBoxItem(serviceRequest.ProductId, serviceRequest.ProductName) },
                        serviceRequest.Fio,
                        serviceRequest.Phone,
                        serviceRequest.Phone2,
                        serviceRequest.Email);

                    nonModalDialogDocumentManagerService.ShowView<ComplaintCreateViewModel>(parameter, supportServices);
                }
                catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    MessageFacadeService.ShowNotificationWarning($"Заявка №{serviceRequestId} не найдена");
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationWarning(Resources.ErrorDuringDataLoading);
                    Logger.LogError(exception, "Error while creating complaint from service request");
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Неверный номер заявки");
            }
        }
    }
}