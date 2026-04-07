using System.Collections.Generic;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.ReturnInvoice;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.ReturnInvoice;
using Telemart.Client.ViewModels.Dialogs.AddDocuments;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public class ReturnInvoiceAddDocumentViewModel : AddDocumentsViewModelBase
    {
        public ReturnInvoiceAddDocumentViewModel(
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
            yield break;
        }

        protected override async Task CreateDocumentAsync(int entityId, AddDocumentViewItem item)
        {
            ReturnInvoiceDocumentDto dto = new ReturnInvoiceDocumentDto
            {
                Data = await item.GetDataAsync(),
                ReturnInvoiceId = entityId,
                Name = item.DocumentName,
                Ext = item.GetExtension(),
            };

            Result<ReturnInvoiceDocumentSimpleDto> result = await WebClient.ExecuteApiRequestAsync(new CreateReturnInvoiceDocument(dto));

            Messenger.Send(new ReturnInvoiceDocumentMessage(result.Data, MessageType.Added));
        }
    }
}