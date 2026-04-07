using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.History;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.History.SerialNumber
{
    public sealed class SerialNumberHistoryViewModel : TelemartDialogViewModelBase
    {
        private const string InvoiceDocumentType = "Invoice";
        private const string OrderDocumentType = "Order";
        private const string ServiceRequestDocumentType = "ServiceRequest";
        private const string ServiceRepairDocumentType = "ServiceRepair";
        private const string AssemblyServiceDocumentType = "AssemblyService";
        private const string ReturnInvoiceDocumantType = "ReturnInvoice";

        private readonly IReadOnlyDictionary<string, string> documentNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [InvoiceDocumentType] = "Накладная",
            [OrderDocumentType] = "Заказ",
            [ServiceRequestDocumentType] = "Серв. заявка",
            [ServiceRepairDocumentType] = "Ремонт",
            [AssemblyServiceDocumentType] = "Сборка",
            [ReturnInvoiceDocumantType] = "Возврат"
        };

        public SerialNumberHistoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            RefreshCommand = new AsyncCommand<string>(RefreshAsync, x => !string.IsNullOrWhiteSpace(x));
            ViewDocumentCommand = new DelegateCommand<SerialNumberHistoryViewItem>(ViewDocument, x => x != null);
        }

        public SerialNumberHistoryViewModel()
        {
        }

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand ViewDocumentCommand { get; }

        #endregion

        #region Properties

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public bool IsReadOnly
        {
            get { return GetProperty(() => IsReadOnly); }
            private set { SetProperty(() => IsReadOnly, value); }
        }

        public ReadOnlyObservableCollection<SerialNumberHistoryViewItem> Records
        {
            get { return GetProperty(() => Records); }
            private set { SetProperty(() => Records, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 450;

        public override int MinHeight => 338;

        public override int MinWidth => 600;

        public override int Width => 800;

        #endregion

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<SerialNumberHistoryViewModel> builder)
        {
            builder.Property(x => x.SerialNumber).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            SerialNumber = (string)Parameter;
            IsReadOnly = !string.IsNullOrWhiteSpace(SerialNumber);

            await RefreshAsync(SerialNumber);

            Title = "История SN";
        }

        protected override Task HandleOkAsync()
        {
            return Task.CompletedTask;
        }

        private SerialNumberHistoryViewItem MapToViewItem(SerialNumberHistoryDto source, SerialNumberHistoryViewItem destination)
        {
            destination.DocumentId = source.DocumentId;
            destination.DocumentType = source.DocumentType;
            destination.DocumentTypeRu = documentNames.GetValueOrDefault(source.DocumentType, source.DocumentType);
            destination.DateTime = source.DateTime;
            destination.Info = source.Info;

            return destination;
        }

        private async Task RefreshAsync(string serialNumber)
        {
            Records = null;

            try
            {
                IReadOnlyCollection<SerialNumberHistoryDto> records = Array.Empty<SerialNumberHistoryDto>();

                if (!string.IsNullOrWhiteSpace(serialNumber))
                {
                    records = await WebClient.ExecuteApiRequestAsync(new QuerySerialNumberHistory(serialNumber));
                }

                Records = records.Select(x => MapToViewItem(x, new SerialNumberHistoryViewItem())).ToReadOnlyObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void ViewDocument(SerialNumberHistoryViewItem viewItem)
        {
            switch (viewItem.DocumentType)
            {
                case InvoiceDocumentType:
                    Messenger.Send(new InvoiceEditViewMessage(viewItem.DocumentId));
                    break;
                case OrderDocumentType:
                    Messenger.Send(new OrderEditViewMessage(viewItem.DocumentId));
                    break;
                case ServiceRequestDocumentType:
                    Messenger.Send(new ServiceRequestViewMessage(viewItem.DocumentId));
                    break;
                case ServiceRepairDocumentType:
                    Messenger.Send(new ServiceRepairViewMessage(viewItem.DocumentId));
                    break;
                case ReturnInvoiceDocumantType:
                    Messenger.Send(new ReturnInvoiceEditViewMessage(viewItem.DocumentId));
                    break;
                case AssemblyServiceDocumentType:
                    Messenger.Send(new AssemblyServiceViewMessage(viewItem.DocumentId));
                    break;
            }
        }
    }
}