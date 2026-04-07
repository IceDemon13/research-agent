using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.OrderDocuments;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs.AddDocuments;
using Telemart.Client.Views.Dialogs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order.OrderDocuments
{
    [ViewName(nameof(AddDocumentsView))]
    public sealed class OrderAddDocumentViewModel : AddDocumentsViewModelBase
    {
        private readonly IMessenger _messenger;
        private readonly IErrorHandler _errorHandler;

        public OrderAddDocumentViewModel(
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

        protected override IEnumerable<ComboBoxItem> GetTypes()
        {
            return Dictionaries.GetItems<OrderDocumentType>().Select(x => new ComboBoxItem(x.Id, x.Name));
        }

        protected override async Task CreateDocumentAsync(int entityId, AddDocumentViewItem item)
        {
            OrderCreateDocumentDto documentDto = new OrderCreateDocumentDto()
            {
                Data = await item.GetDataAsync(),
                OrderId = entityId,
                Name = item.DocumentName,
                Ext = item.GetExtension(),
                TypeId = item.DocumentTypeId!.Value
            };

            Result<OrderDocumentDto> documentResult = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateOrderDocument(entityId, documentDto)),
                "создании документа",
                "Документ создан",
                this,
                true);

            if (documentResult.IsSuccess)
            {
                OrderDto orderDto = await WebClient.ExecuteApiRequestAsync(new QueryOrder(entityId));

                _messenger.Send(new OrderCreateDocumentMessage(documentResult.Data, MessageType.Added, orderDto));
            }
        }
    }
}