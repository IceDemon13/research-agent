using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.PromoCode.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.PromoCode;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.PromoCode
{
    public class PromoCodeSetShowInSiteViewModel : TelemartDialogViewModelBase
    {
        private int promoCodeId;

        public PromoCodeSetShowInSiteViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public bool ShowInSite
        {
            get { return GetProperty(() => ShowInSite); }
            set { SetProperty(() => ShowInSite, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            PromoCodeSetShowInSiteParameter parameter = (PromoCodeSetShowInSiteParameter)Parameter;

            promoCodeId = parameter.Id;
            ShowInSite = parameter.ShowInSite;

            Title = "Отображать в карточке товара";

            return base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                Result<PromoCodeFullDto> result = await WebClient.ExecuteApiRequestAsync(new SetPromoCodeShowInSite(promoCodeId, ShowInSite));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Отображение на карточке товара изменено с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Отображение на карточке товара успешно изменено");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                ShowValidationResultView(Resources.ErrorExecutingOperation, exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to set promocode show in site");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                Logger.LogError(exception, "Error while set promocode show in site");
            }
        }
    }
}