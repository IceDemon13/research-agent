using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Dialogs;

namespace Telemart.Client.Factories.Complaint
{
    public abstract class ComplaintCreatorBase
    {
        protected ComplaintCreatorBase(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            ILogger logger)
        {
            WebClient = webClient;
            MessageFacadeService = messageFacadeService;
            Logger = logger;
        }

        protected IWebClient WebClient { get; }

        protected ILogger Logger { get; }

        protected IMessageFacadeService MessageFacadeService { get; }

        public abstract Task ShowViewAsync(
            IDocumentManagerService dialogDocumentManagerService,
            IDocumentManagerService nonModalDialogDocumentManagerService,
            ISupportServices supportServices);

        protected string GetDocumentNumber(string caption, string title, IDocumentManagerService dialogDocumentManagerService, ISupportServices supportServices)
        {
            GetTextFromUserParameter fromUserParameter = new(
                caption,
                title,
                @"^\d+$",
                "Номер документа должен быть числом");

            GetTextFromUserViewModel fromUserViewModel = dialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, supportServices);

            return fromUserViewModel.IsOk ? fromUserViewModel.Content : null;
        }
    }
}