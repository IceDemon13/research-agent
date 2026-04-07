using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Complaint;

namespace Telemart.Client.Factories.Complaint
{
    public class EmptyComplaintCreator : ComplaintCreatorBase
    {
        public EmptyComplaintCreator(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            ILogger<EmptyComplaintCreator> logger)
            : base(webClient, messageFacadeService, logger)
        {
        }

        public override Task ShowViewAsync(
            IDocumentManagerService dialogDocumentManagerService,
            IDocumentManagerService nonModalDialogDocumentManagerService,
            ISupportServices supportServices)
        {
            nonModalDialogDocumentManagerService.ShowView<ComplaintCreateViewModel>(ComplaintCreateParameter.Empty, supportServices);

            return Task.CompletedTask;
        }
    }
}