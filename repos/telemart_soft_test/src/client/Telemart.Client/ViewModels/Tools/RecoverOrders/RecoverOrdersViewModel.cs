using System;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Tools.RecoverOrders
{
    public class RecoverOrdersViewModel : TelemartDialogViewModelBase
    {
        public RecoverOrdersViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Title = "Провести заказы";
        }

        public int[] OrderIds
        {
            get { return GetProperty(() => OrderIds); }
            set { SetProperty(() => OrderIds, value); }
        }

        public static void BuildMetadata(MetadataBuilder<RecoverOrdersViewModel> builder)
        {
            builder.Property(x => x.OrderIds)
                .MatchesRule(x => x?.Any() == true, () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleOkAsync()
        {
            Data.Requests.Features.Order.Actions.RecoverOrders recoverOrders = new Data.Requests.Features.Order.Actions.RecoverOrders(OrderIds);

            try
            {
                string[] result = await WebClient.ExecuteApiRequestAsync(recoverOrders);

                ShowValidationResultView("Результат проведения заказов", result.Select(x => new ValidationResultItem(x, false)));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при проведении заказов");
                ShowValidationResultView("Ошибки при проведении заказов", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to recover orders");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при проведении заказов");
                Logger.LogError(exception, "Error while recovering orders");
            }
        }
    }
}