using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs.AddDocuments;
using Telemart.Client.Views.Dialogs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.TradeIn
{
    [ViewName(nameof(AddDocumentsView))]
    public sealed class TradeInAddDocumentViewModel : AddDocumentsViewModelBase
    {
        private readonly IMessenger _messenger;
        private readonly IErrorHandler _errorHandler;

        public TradeInAddDocumentViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _messenger = messenger;
            _errorHandler = errorHandler;
        }

        protected override bool Validate(IReadOnlyCollection<AddDocumentViewItem> notProcessedItems)
        {
            if (notProcessedItems.Any(x => x.DocumentTypeId!.Value == TradeInDocumentType.InnId))
            {
                if (!MessageFacadeService.Confirm("Убедитесь, что ФИО в сканкопии ИНН соответствует ФИО, который указан в заявке"))
                {
                    return false;
                }
            }

            return true;
        }

        protected override IEnumerable<ComboBoxItem> GetTypes()
        {
            return Dictionaries.GetItems<TradeInDocumentType>().Select(x => new ComboBoxItem(x.Id, x.Name));
        }

        protected override async Task CreateDocumentAsync(int entityId, AddDocumentViewItem item)
        {
            CreateTradeInDocumentDto documentDto = new CreateTradeInDocumentDto()
            {
                Data = await item.GetDataAsync(),
                TradeInId = entityId,
                Name = item.DocumentName,
                Ext = item.GetExtension(),
                TypeId = item.DocumentTypeId!.Value
            };

            Result<TradeInDocumentDto> documentResult = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateTradeInDocument(entityId, documentDto)),
                "создании документа Trade-In",
                "Документ создан",
                this,
                true);

            if (documentResult.IsSuccess)
            {
                _messenger.Send(new TradeInCreateDocumentMessage(documentResult.Data, MessageType.Added));
            }
        }
    }
}