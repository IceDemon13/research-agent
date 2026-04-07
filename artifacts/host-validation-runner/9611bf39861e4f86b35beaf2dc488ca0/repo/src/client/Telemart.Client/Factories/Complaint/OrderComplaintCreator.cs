using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Complaint;

namespace Telemart.Client.Factories.Complaint
{
    public class OrderComplaintCreator : ComplaintCreatorBase
    {
        public OrderComplaintCreator(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            ILogger<OrderComplaintCreator> logger)
            : base(webClient, messageFacadeService, logger)
        {
        }

        public override async Task ShowViewAsync(
            IDocumentManagerService dialogDocumentManagerService,
            IDocumentManagerService nonModalDialogDocumentManagerService,
            ISupportServices supportServices)
        {
            string documentNumber = GetDocumentNumber("Введите номер заказа", "Номер заказа", dialogDocumentManagerService, supportServices);

            if (string.IsNullOrEmpty(documentNumber))
            {
                return;
            }

            if (int.TryParse(documentNumber, out int orderId))
            {
                try
                {
                    OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId));

                    ComplaintCreateParameter parameter = new(
                        order.Id,
                        null,
                        null,
                        order.ClientId,
                        order.Products.Select(x => new ComboBoxItem(x.Product.Id, x.Product.Name)).ToList(),
                        order.Fio,
                        order.Phone,
                        order.Phone2,
                        order.Email);

                    nonModalDialogDocumentManagerService.ShowView<ComplaintCreateViewModel>(parameter, supportServices);
                }
                catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    MessageFacadeService.ShowNotificationWarning($"Заказ №{orderId} не найден");
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationWarning(Resources.ErrorDuringDataLoading);
                    Logger.LogError(exception, "Error while creating complaint from order");
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Неверный номер заказа");
            }
        }
    }
}