using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.SupplierBill;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.SupplierBill;
using Telemart.Client.ViewModels.Dialogs.AddDocuments;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.SupplierBill
{
    public class SupplierBillAddDocumentViewModel : AddDocumentsViewModelBase
    {
        public SupplierBillAddDocumentViewModel(
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
            return Dictionaries.GetItems<SupplierBillDocumentType>().Select(x => new ComboBoxItem(x.Id, x.Name));
        }

        protected override async Task CreateDocumentAsync(int entityId, AddDocumentViewItem item)
        {
            SupplierBillDocumentDto dto = new SupplierBillDocumentDto
            {
                Data = await item.GetDataAsync(),
                SupplierBillId = entityId,
                Name = item.DocumentName,
                Ext = item.GetExtension(),
                TypeId = item.DocumentTypeId!.Value
            };

            Result<SupplierBillDocumentDto> document = await WebClient.ExecuteApiRequestAsync(new CreateSupplierBillDocument(dto));

            Messenger.Send(new SupplierBillDocumentMessage(document.Data, MessageType.Added));
        }
    }
}