using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Dialogs.AddDocuments;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public class ServiceRequestAddDocumentViewModel : AddDocumentsViewModelBase
    {
        public ServiceRequestAddDocumentViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        private IMessenger Messenger { get; }

        protected override IEnumerable<ComboBoxItem> GetTypes()
        {
            return Dictionaries.GetItems<ServiceRequestDocumentType>().Select(x => new ComboBoxItem(x.Id, x.Name));
        }

        protected override async Task CreateDocumentAsync(int entityId, AddDocumentViewItem item)
        {
            ServiceRequestDocumentDto dto = new ServiceRequestDocumentDto
            {
                Data = await item.GetDataAsync(),
                ServiceRequestId = entityId,
                Name = item.DocumentName,
                Ext = item.GetExtension(),
                TypeId = item.DocumentTypeId!.Value
            };

            ServiceRequestDocumentSimpleDto document = await WebClient.ExecuteApiRequestAsync(new CreateServiceRequestDocument(dto));

            Messenger.Send(new ServiceRequestDocumentMessage(document, MessageType.Added));
        }
    }
}
