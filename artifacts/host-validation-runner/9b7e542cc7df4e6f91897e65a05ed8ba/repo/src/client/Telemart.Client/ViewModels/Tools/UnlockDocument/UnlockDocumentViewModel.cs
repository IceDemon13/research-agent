using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Tools.UnlockDocument
{
    public sealed class UnlockDocumentViewModel : TelemartDialogViewModelBase
    {
        private const int OrderDocumentTypeId = 1;
        private const int InvoiceDocumentTypeId = 2;
        private const int MovementDocumentTypeId = 3;
        private const int ServiceRequestDocumentTypeId = 4;

        public UnlockDocumentViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory)
            : base(webClient, dictionaries, messageFacadeService)
        {
            LockableOperationProcessorFactory = lockableOperationProcessorFactory;
        }

        public UnlockDocumentViewModel()
        {
        }

        public ReadOnlyObservableCollection<ComboBoxItem> DocumentTypes
        {
            get { return GetProperty(() => DocumentTypes); }
            private set { SetProperty(() => DocumentTypes, value); }
        }

        public int? DocumentTypeId
        {
            get { return GetProperty(() => DocumentTypeId); }
            set { SetProperty(() => DocumentTypeId, value); }
        }

        public int? DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            set { SetProperty(() => DocumentId, value); }
        }

        private ILockableOperationProcessorFactory LockableOperationProcessorFactory { get; }

        public static void BuildMetadata(MetadataBuilder<UnlockDocumentViewModel> builder)
        {
            builder.Property(x => x.DocumentTypeId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DocumentId).Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            DocumentTypes = GetDocumentTypes().ToReadOnlyObservableCollection();
            Title = "Разблокировка документа";
            return Task.CompletedTask;

            IEnumerable<ComboBoxItem> GetDocumentTypes()
            {
                yield return new ComboBoxItem(OrderDocumentTypeId, "Заказ");
                yield return new ComboBoxItem(InvoiceDocumentTypeId, "Накладная");
                yield return new ComboBoxItem(MovementDocumentTypeId, "Перемещение");
                yield return new ComboBoxItem(ServiceRequestDocumentTypeId, "Серв. заявка");
            }
        }

        protected override Task HandleOkAsync()
        {
            if (DocumentTypeId == null || DocumentId == null || !MessageFacadeService.Confirm("Вы уверены?"))
            {
                return Task.CompletedTask;
            }

            int entityId = DocumentId.Value;

            Task unlockTask;

            switch (DocumentTypeId.Value)
            {
                case OrderDocumentTypeId:
                    unlockTask = UnlockEntityAsync<OrderDto>(entityId);
                    break;
                case InvoiceDocumentTypeId:
                    unlockTask = UnlockEntityAsync<InvoiceDto>(entityId);
                    break;
                case MovementDocumentTypeId:
                    unlockTask = UnlockEntityAsync<MovementDto>(entityId);
                    break;
                case ServiceRequestDocumentTypeId:
                    unlockTask = UnlockEntityAsync<ServiceRequestDto>(entityId);
                    break;
                default:
                    unlockTask = Task.CompletedTask;
                    break;
            }

            return unlockTask;
        }

        private async Task UnlockEntityAsync<TDto>(int entityId)
            where TDto : class, new()
        {
            LockableOperationProcessor<TDto> lockableOperationProcessor = LockableOperationProcessorFactory.Create<TDto>();

            LockResponse<TDto> response = await lockableOperationProcessor.UnlockAsync(entityId, true);

            if (response != null && response.Success)
            {
                MessageFacadeService.ShowNotificationInfo("Документ успешно разблокирован");
                IsOk = true;
                Close();
            }
        }
    }
}